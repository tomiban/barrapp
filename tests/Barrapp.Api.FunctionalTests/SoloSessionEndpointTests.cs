using System.Net;
using System.Net.Http.Json;
using Barrapp.Application.Features.SoloSessions;
using Microsoft.AspNetCore.Mvc;

namespace Barrapp.Api.FunctionalTests;

/// <summary>
/// La sesión suelta se genera con <c>POST /sessions/suelta</c>: necesita perfil y objetivo
/// guardados y compone la sesión según tiempo, energía y foco. El motor vive en el servidor y la
/// respuesta devuelve la sesión ya generada (el historial llega en #29).
/// </summary>
public sealed class SoloSessionEndpointTests(BarrappApiFactory factory)
    : IClassFixture<BarrappApiFactory>
{
    [Fact]
    public async Task Post_suelta_with_a_pattern_focus_returns_the_session_for_that_pattern()
    {
        using var client = factory.CreateClient();
        await client.PutAsJsonAsync("/profile", PlanTestData.Profile(3));
        await client.PutAsJsonAsync("/profile/objective", new { skillId = "planche" });

        using var response = await client.PostAsJsonAsync(
            "/sessions/suelta",
            new { timeMinutes = 30, energy = "media", focus = "patron", pattern = "push" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var session = await response.Content.ReadFromJsonAsync<SoloSessionResponse>();
        Assert.NotNull(session);
        Assert.Equal("patron", session!.Focus);
        Assert.Equal("push", session.Pattern);
        Assert.Equal(30, session.TimeMinutes);
        Assert.Equal("media", session.Energy);

        // 30 min → dos huecos de fuerza + core; todo el trabajo de fuerza es de empuje.
        Assert.Equal(3, session.Items.Count);
        Assert.All(
            session.Items.Where(item => item.Role == "strength"),
            item => Assert.Equal("push", item.Pattern));
        Assert.Contains(session.Items, item => item.ExerciseId == "push_up");
        Assert.Contains(session.Items, item => item.Role == "core");
        Assert.All(session.Items, item => Assert.False(string.IsNullOrWhiteSpace(item.ExerciseName)));
    }

    [Fact]
    public async Task Post_suelta_with_skill_focus_leads_with_the_current_stage()
    {
        using var client = factory.CreateClient();
        await client.PutAsJsonAsync("/profile", PlanTestData.Profile(3));
        await client.PutAsJsonAsync("/profile/objective", new { skillId = "planche" });
        await client.PutAsJsonAsync("/catalog/progress/planche", new { stageOrder = 2 });

        using var response = await client.PostAsJsonAsync(
            "/sessions/suelta",
            new { timeMinutes = 45, energy = "alta", focus = "skill" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var session = await response.Content.ReadFromJsonAsync<SoloSessionResponse>();
        Assert.NotNull(session);
        Assert.Equal("skill", session!.Focus);
        Assert.Equal("planche", session.SkillId);
        Assert.Equal("Planche", session.SkillName);

        // La escalera aparece al inicio: etapa 2 de planche, sin tocar su marca.
        Assert.Equal("skill", session.Items[0].Role);
        Assert.Equal("planche-tuck", session.Items[0].ExerciseId);
        Assert.Equal(10, session.Items[0].HoldSecondsMax);
        Assert.Contains(session.Items, item => item.Role == "core");
    }

    [Fact]
    public async Task Post_suelta_with_surprise_focus_resolves_deterministically()
    {
        using var client = factory.CreateClient();
        await client.PutAsJsonAsync("/profile", PlanTestData.Profile(3));
        await client.PutAsJsonAsync("/profile/objective", new { skillId = "planche" });

        async Task<SoloSessionResponse> Generate()
        {
            var httpResponse = await client.PostAsJsonAsync(
                "/sessions/suelta",
                new { timeMinutes = 45, energy = "alta", focus = "sorprendeme" });
            return (await httpResponse.Content.ReadFromJsonAsync<SoloSessionResponse>())!;
        }

        var first = await Generate();
        var second = await Generate();

        Assert.Equal("sorprendeme", first.Focus);
        Assert.True(first.Pattern is not null || first.SkillId is not null,
            "sorpréndeme debe resolver a un patrón o al skill");
        Assert.Equal(first.Items.Count, second.Items.Count);
        Assert.Equal(
            first.Items.Select(item => item.ExerciseId),
            second.Items.Select(item => item.ExerciseId));
    }

    [Fact]
    public async Task Post_suelta_with_an_invalid_energy_returns_400_with_a_spanish_detail()
    {
        using var client = factory.CreateClient();
        await client.PutAsJsonAsync("/profile", PlanTestData.Profile(3));
        await client.PutAsJsonAsync("/profile/objective", new { skillId = "planche" });

        using var response = await client.PostAsJsonAsync(
            "/sessions/suelta",
            new { timeMinutes = 30, energy = "maxima", focus = "patron", pattern = "push" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(problem);
        Assert.Contains("La energía debe ser baja, media o alta.", problem!.Detail);
    }

    [Fact]
    public async Task Post_suelta_with_an_invalid_time_returns_400_with_a_spanish_detail()
    {
        using var client = factory.CreateClient();
        await client.PutAsJsonAsync("/profile", PlanTestData.Profile(3));
        await client.PutAsJsonAsync("/profile/objective", new { skillId = "planche" });

        using var response = await client.PostAsJsonAsync(
            "/sessions/suelta",
            new { timeMinutes = 20, energy = "media", focus = "skill" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(problem);
        Assert.Contains("El tiempo debe ser 15, 30, 45 o 60 minutos.", problem!.Detail);
    }

    [Fact]
    public async Task Post_suelta_with_pattern_focus_without_a_pattern_returns_400()
    {
        using var client = factory.CreateClient();
        await client.PutAsJsonAsync("/profile", PlanTestData.Profile(3));
        await client.PutAsJsonAsync("/profile/objective", new { skillId = "planche" });

        using var response = await client.PostAsJsonAsync(
            "/sessions/suelta",
            new { timeMinutes = 30, energy = "media", focus = "patron" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(problem);
        Assert.Contains("Debes elegir un patrón (empuje, tirón o pierna).", problem!.Detail);
    }

    [Theory]
    [InlineData("core")]
    [InlineData("cardio")]
    public async Task Post_suelta_with_an_unsupported_focus_pattern_returns_400(string pattern)
    {
        using var client = factory.CreateClient();
        await client.PutAsJsonAsync("/profile", PlanTestData.Profile(3));
        await client.PutAsJsonAsync("/profile/objective", new { skillId = "planche" });

        using var response = await client.PostAsJsonAsync(
            "/sessions/suelta",
            new { timeMinutes = 30, energy = "media", focus = "patron", pattern });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}

/// <summary>Clase aparte para arrancar una base vacía: sin perfil no hay sesión suelta.</summary>
public sealed class SoloSessionWithoutProfileEndpointTests(BarrappApiFactory factory)
    : IClassFixture<BarrappApiFactory>
{
    [Fact]
    public async Task Post_suelta_without_a_profile_returns_404_with_a_spanish_detail()
    {
        using var client = factory.CreateClient();

        using var response = await client.PostAsJsonAsync(
            "/sessions/suelta",
            new { timeMinutes = 30, energy = "media", focus = "skill" });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(problem);
        Assert.Contains("No hay ningún perfil guardado para este atleta.", problem!.Detail);
    }
}

/// <summary>Clase aparte: guarda el perfil pero no el objetivo.</summary>
public sealed class SoloSessionWithoutObjectiveEndpointTests(BarrappApiFactory factory)
    : IClassFixture<BarrappApiFactory>
{
    [Fact]
    public async Task Post_suelta_without_an_objective_returns_404_with_a_spanish_detail()
    {
        using var client = factory.CreateClient();
        await client.PutAsJsonAsync("/profile", PlanTestData.Profile(3));

        using var response = await client.PostAsJsonAsync(
            "/sessions/suelta",
            new { timeMinutes = 30, energy = "media", focus = "patron", pattern = "push" });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(problem);
        Assert.Contains("No hay ningún objetivo guardado para este atleta.", problem!.Detail);
    }
}
