using System.Net;
using System.Net.Http.Json;
using Barrapp.Application.Features.AthleteProfiles;
using Barrapp.Application.Features.SessionLogs;
using Barrapp.Application.Features.SkillProgress;
using Barrapp.Application.Features.SoloSessions;
using Microsoft.AspNetCore.Mvc;

namespace Barrapp.Api.FunctionalTests;

/// <summary>
/// Historial de sesiones sueltas (#29): cada generación queda guardada y etiquetada como suelta,
/// se puede marcar como registrada y el aislamiento es estructural —la suelta no crea registros de
/// sesión, no toca los máximos y no avanza la etapa del skill. El historial se ordena de la más
/// reciente a la más antigua, así que cada test identifica su propia suelta como la primera entrada
/// (todas las pruebas de la clase comparten la base del fixture).
/// </summary>
public sealed class SessionSueltaHistoryEndpointTests(BarrappApiFactory factory)
    : IClassFixture<BarrappApiFactory>
{
    [Fact]
    public async Task Generating_a_suelta_lands_it_in_the_history_labeled_as_suelta()
    {
        using var client = factory.CreateClient();
        await client.PutAsJsonAsync("/profile", PlanTestData.Profile(3));
        await client.PutAsJsonAsync("/profile/objective", new { skillId = "planche" });

        using var generated = await client.PostAsJsonAsync(
            "/sessions/suelta",
            new { timeMinutes = 30, energy = "media", focus = "patron", pattern = "push" });
        Assert.Equal(HttpStatusCode.OK, generated.StatusCode);

        var history = await ReadHistoryAsync(client);
        var entry = history[0]; // Su historia, la más reciente.

        Assert.Equal("generada", entry.Status);
        Assert.Equal(30, entry.TimeMinutes);
        Assert.Equal("media", entry.Energy);
        Assert.Equal("patron", entry.Focus);
        Assert.Equal("push", entry.Pattern);
        Assert.Null(entry.SkillId);
        Assert.Null(entry.RecordedAtUtc);
        Assert.Equal(3, entry.Items.Count);
    }

    [Fact]
    public async Task Recording_a_generated_suelta_updates_its_status_in_history()
    {
        using var client = factory.CreateClient();
        await client.PutAsJsonAsync("/profile", PlanTestData.Profile(3));
        await client.PutAsJsonAsync("/profile/objective", new { skillId = "planche" });

        await client.PostAsJsonAsync(
            "/sessions/suelta",
            new { timeMinutes = 45, energy = "alta", focus = "skill" });
        var history = await ReadHistoryAsync(client);
        var id = history[0].Id;

        using var recorded = await client.PostAsync(
            $"/sessions/suelta/{id}/registrar",
            content: null);
        Assert.Equal(HttpStatusCode.OK, recorded.StatusCode);

        var after = await ReadHistoryAsync(client);
        var reloaded = Assert.Single(after, entry => entry.Id == id);
        Assert.Equal("registrada", reloaded.Status);
        Assert.NotNull(reloaded.RecordedAtUtc);
    }

    [Fact]
    public async Task History_returns_the_most_recent_suelta_first()
    {
        using var client = factory.CreateClient();
        await client.PutAsJsonAsync("/profile", PlanTestData.Profile(3));
        await client.PutAsJsonAsync("/profile/objective", new { skillId = "planche" });

        await client.PostAsJsonAsync(
            "/sessions/suelta",
            new { timeMinutes = 15, energy = "baja", focus = "patron", pattern = "pull" });
        await client.PostAsJsonAsync(
            "/sessions/suelta",
            new { timeMinutes = 60, energy = "alta", focus = "patron", pattern = "leg" });

        var history = await ReadHistoryAsync(client);

        // Los dos más recientes son los de esta prueba: primero el último generado (pierna).
        Assert.Equal("leg", history[0].Pattern);
        Assert.Equal("pull", history[1].Pattern);
        Assert.Equal(60, history[0].TimeMinutes);
    }

    [Fact]
    public async Task A_recorded_suelta_does_not_touch_session_logs_maximums_or_stage()
    {
        using var client = factory.CreateClient();
        await client.PutAsJsonAsync("/profile", PlanTestData.Profile(3));
        await client.PutAsJsonAsync("/profile/objective", new { skillId = "planche" });
        await client.PutAsJsonAsync("/catalog/progress/planche", new { stageOrder = 2 });

        using var generated = await client.PostAsJsonAsync(
            "/sessions/suelta",
            new { timeMinutes = 30, energy = "media", focus = "patron", pattern = "push" });
        Assert.Equal(HttpStatusCode.OK, generated.StatusCode);

        var history = await ReadHistoryAsync(client);
        await client.PostAsync($"/sessions/suelta/{history[0].Id}/registrar", content: null);

        // La suelta no alimenta el flujo de registros de sesión.
        var logs = await client.GetFromJsonAsync<IReadOnlyList<SessionLogResponse>>("/session-logs");
        Assert.NotNull(logs);
        Assert.Empty(logs!);

        // Los máximos del atleta no cambian.
        var profile = await client.GetFromJsonAsync<AthleteProfileResponse>("/profile");
        Assert.NotNull(profile);
        var pushUp = Assert.Single(profile!.Maximums, maximum => maximum.ExerciseCode == "push_up");
        Assert.Equal(10, pushUp.Repetitions);

        // La etapa del skill no avanza.
        var progress = await client.GetFromJsonAsync<IReadOnlyList<SkillProgressResponse>>("/catalog/progress");
        var planche = Assert.Single(progress!, candidate => candidate.SkillId == "planche");
        Assert.Equal(2, planche.StageOrder);
    }

    [Fact]
    public async Task Recording_an_unknown_suelta_returns_404_with_a_spanish_detail()
    {
        using var client = factory.CreateClient();
        await client.PutAsJsonAsync("/profile", PlanTestData.Profile(3));
        await client.PutAsJsonAsync("/profile/objective", new { skillId = "planche" });

        using var response = await client.PostAsync(
            $"/sessions/suelta/{Guid.NewGuid()}/registrar",
            content: null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(problem);
        Assert.Contains("No hay ninguna sesión suelta guardada con ese identificador.", problem!.Detail);
    }

    [Fact]
    public async Task Recording_the_same_suelta_twice_returns_409()
    {
        using var client = factory.CreateClient();
        await client.PutAsJsonAsync("/profile", PlanTestData.Profile(3));
        await client.PutAsJsonAsync("/profile/objective", new { skillId = "planche" });

        await client.PostAsJsonAsync(
            "/sessions/suelta",
            new { timeMinutes = 30, energy = "media", focus = "patron", pattern = "push" });
        var history = await ReadHistoryAsync(client);
        var id = history[0].Id;

        using var first = await client.PostAsync(
            $"/sessions/suelta/{id}/registrar",
            content: null);
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);

        using var second = await client.PostAsync(
            $"/sessions/suelta/{id}/registrar",
            content: null);

        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);

        var problem = await second.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(problem);
        Assert.Contains("Esta sesión suelta ya estaba registrada.", problem!.Detail);
    }

    private static async Task<IReadOnlyList<SessionSueltaResponse>> ReadHistoryAsync(HttpClient client)
    {
        var response = await client.GetAsync("/sessions/suelta");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<IReadOnlyList<SessionSueltaResponse>>())!;
    }
}

/// <summary>Clase aparte para arrancar una base vacía: sin sueltas, el historial está vacío.</summary>
public sealed class SessionSueltaHistoryEmptyEndpointTests(BarrappApiFactory factory)
    : IClassFixture<BarrappApiFactory>
{
    [Fact]
    public async Task History_is_empty_when_no_suelta_was_generated()
    {
        using var client = factory.CreateClient();
        await client.PutAsJsonAsync("/profile", PlanTestData.Profile(3));
        await client.PutAsJsonAsync("/profile/objective", new { skillId = "planche" });

        using var response = await client.GetAsync("/sessions/suelta");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var history = await response.Content.ReadFromJsonAsync<IReadOnlyList<SessionSueltaResponse>>();
        Assert.NotNull(history);
        Assert.Empty(history!);
    }
}