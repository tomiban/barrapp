using System.Net;
using System.Net.Http.Json;
using Barrapp.Application.Features.Plans;
using Barrapp.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Barrapp.Api.FunctionalTests;

/// <summary>
/// Cierre del mesociclo (spec 0001, US-24; ticket #24, D7): <c>POST /plan/close</c> cierra el
/// mesociclo activo, ajusta los máximos del perfil con las sesiones registradas del mesociclo
/// (para cada ejercicio básico, <c>max(máximo actual, mejor repetición lograda)</c>, nunca baja)
/// y lo publica en el historial. Sin mesociclo activo, el cierre <b>sintetiza</b> el mesociclo que
/// se cierra desde el perfil + objetivo (FIX-1): así la secuencia real de la app —que solo llama a
/// <c>GET /plan</c>, sin persistir— cierra y ajusta de extremo a extremo. Devuelve 404 si no hay
/// perfil y 404 también si, al sintetizar, no hay objetivo guardado.
/// El siguiente <c>GET /plan</c> (sin activo) genera con los máximos ajustados.
/// </summary>
/// <remarks>Cada test usa su propia <see cref="BarrappApiFactory"/> (base efímera).</remarks>
public sealed class CloseMesocycleEndpointTests
{
    [Fact]
    public async Task Post_plan_close_without_a_profile_returns_404_with_a_spanish_detail()
    {
        using var factory = new BarrappApiFactory();
        using var client = factory.CreateClient();

        using var response = await client.PostAsync("/plan/close", content: null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(problem);
        Assert.Contains("No hay ningún perfil guardado para este atleta.", problem!.Detail);
    }

    [Fact]
    public async Task Post_plan_close_without_an_active_mesocycle_synthesizes_and_closes_from_get_plan_alone()
    {
        using var factory = new BarrappApiFactory();
        using var client = factory.CreateClient();
        await client.PutAsJsonAsync("/profile", PlanTestData.Profile(3)); // push_up 10
        await client.PutAsJsonAsync("/profile/objective", new { skillId = "planche" });

        // La app nunca llama a POST /plan: genera on-read con GET /plan, que no persiste. El
        // cierre sintetiza el mesociclo que se cierra desde el perfil + objetivo (FIX-1), así que
        // la secuencia real de la app funciona de extremo a extremo.
        var plan = await (await client.GetAsync("/plan")).Content.ReadFromJsonAsync<PlanResponse>();
        Assert.NotNull(plan);
        Assert.Equal("planche", plan!.SkillId);

        // El atleta logra 14 flexiones en el mesociclo sintetizado (marca mejor que su máximo de 10).
        var registered = await client.PostAsJsonAsync(
            "/session-logs",
            SessionLogRequest(plan, 1, 14, 14));
        Assert.Equal(HttpStatusCode.OK, registered.StatusCode);

        using var closeResponse = await client.PostAsync("/plan/close", content: null);
        Assert.Equal(HttpStatusCode.OK, closeResponse.StatusCode);
        var closed = await closeResponse.Content.ReadFromJsonAsync<CloseMesocycleResponse>();

        Assert.NotNull(closed);
        Assert.NotEqual(Guid.Empty, closed!.MesocycleId);
        Assert.Equal("planche", closed.SkillId);
        Assert.Contains(
            closed.Maximums,
            maximum => maximum.ExerciseCode == "push_up" && maximum.Repetitions == 14);

        // El mesociclo sintetizado queda publicado en el historial con su detalle.
        var history = await (await client.GetAsync("/plan/history"))
            .Content.ReadFromJsonAsync<IReadOnlyList<MesocycleSummaryResponse>>();
        var entry = Assert.Single(history!);
        Assert.Equal(closed.MesocycleId, entry.Id);
        Assert.Equal("planche", entry.SkillId);

        var detail = await (await client.GetAsync($"/plan/history/{closed.MesocycleId}"))
            .Content.ReadFromJsonAsync<PlanResponse>();
        Assert.Equal("planche", detail!.SkillId);
    }

    [Fact]
    public async Task Post_plan_close_raises_maximums_and_the_next_plan_reflects_them()
    {
        using var factory = new BarrappApiFactory();
        using var client = factory.CreateClient();
        await client.PutAsJsonAsync("/profile", PlanTestData.Profile(3)); // push_up 10
        await client.PutAsJsonAsync("/profile/objective", new { skillId = "planche" });
        await client.PostAsync("/plan", content: null);
        var plan = await (await client.GetAsync("/plan")).Content.ReadFromJsonAsync<PlanResponse>();
        Assert.NotNull(plan);

        // El atleta logra 14 flexiones en el mesociclo (marca mejor que su máximo de 10).
        var registered = await client.PostAsJsonAsync(
            "/session-logs",
            SessionLogRequest(plan, 1, 14, 14));
        Assert.Equal(HttpStatusCode.OK, registered.StatusCode);

        using var closeResponse = await client.PostAsync("/plan/close", content: null);
        Assert.Equal(HttpStatusCode.OK, closeResponse.StatusCode);
        var closed = await closeResponse.Content.ReadFromJsonAsync<CloseMesocycleResponse>();

        Assert.NotNull(closed);
        Assert.NotEqual(Guid.Empty, closed!.MesocycleId);
        Assert.Equal("planche", closed.SkillId);
        Assert.Contains(closed.Maximums, maximum => maximum.ExerciseCode == "push_up" && maximum.Repetitions == 14);

        // Sin mesociclo activo, GET /plan regenera con el máximo ajustado: semana 1 RIR 3 → 9–11.
        var updatedPlan = await (await client.GetAsync("/plan")).Content.ReadFromJsonAsync<PlanResponse>();
        var pushUp = updatedPlan!.Microcycles[0].Sessions[0].Items.Single(item => item.ExerciseId == "push_up");
        Assert.Equal(9, pushUp.RepsMin);
        Assert.Equal(11, pushUp.RepsMax);
    }

    [Fact]
    public async Task Post_plan_close_never_lowers_a_maximum_below_the_registered_mark()
    {
        using var factory = new BarrappApiFactory();
        using var client = factory.CreateClient();
        await client.PutAsJsonAsync("/profile", PlanTestData.Profile(3)); // push_up 10
        await client.PutAsJsonAsync("/profile/objective", new { skillId = "planche" });
        await client.PostAsync("/plan", content: null);
        var plan = await (await client.GetAsync("/plan")).Content.ReadFromJsonAsync<PlanResponse>();
        Assert.NotNull(plan);

        await client.PostAsJsonAsync(
            "/session-logs",
            SessionLogRequest(plan, 1, 8, 9));

        using var closeResponse = await client.PostAsync("/plan/close", content: null);
        var closed = await closeResponse.Content.ReadFromJsonAsync<CloseMesocycleResponse>();

        Assert.Equal(10, closed!.Maximums.Single(maximum => maximum.ExerciseCode == "push_up").Repetitions);
        // La prescripción se mantiene con el máximo sin tocar: semana 1 RIR 3 → 5–7.
        var updatedPlan = await (await client.GetAsync("/plan")).Content.ReadFromJsonAsync<PlanResponse>();
        var pushUp = updatedPlan!.Microcycles[0].Sessions[0].Items.Single(item => item.ExerciseId == "push_up");
        Assert.Equal(5, pushUp.RepsMin);
        Assert.Equal(7, pushUp.RepsMax);
    }

    [Fact]
    public async Task Post_plan_close_publishes_the_mesocycle_to_history()
    {
        using var factory = new BarrappApiFactory();
        using var client = factory.CreateClient();
        await client.PutAsJsonAsync("/profile", PlanTestData.Profile(3));
        await client.PutAsJsonAsync("/profile/objective", new { skillId = "planche" });
        await client.PostAsync("/plan", content: null);

        using var closeResponse = await client.PostAsync("/plan/close", content: null);
        var closed = await closeResponse.Content.ReadFromJsonAsync<CloseMesocycleResponse>();

        Assert.Equal(HttpStatusCode.OK, closeResponse.StatusCode);

        var history = await (await client.GetAsync("/plan/history"))
            .Content.ReadFromJsonAsync<IReadOnlyList<MesocycleSummaryResponse>>();
        Assert.NotNull(history);
        var entry = Assert.Single(history!);
        Assert.Equal(closed!.MesocycleId, entry.Id);
        Assert.Equal("planche", entry.SkillId);

        var detail = await (await client.GetAsync($"/plan/history/{closed.MesocycleId}"))
            .Content.ReadFromJsonAsync<PlanResponse>();
        Assert.Equal("planche", detail!.SkillId);
    }

    [Fact]
    public async Task Post_plan_close_twice_synthesizes_a_fresh_closed_mesocycle_each_time()
    {
        using var factory = new BarrappApiFactory();
        using var client = factory.CreateClient();
        await client.PutAsJsonAsync("/profile", PlanTestData.Profile(3));
        await client.PutAsJsonAsync("/profile/objective", new { skillId = "planche" });

        // Sin mesociclo activo, cada cierre sintetiza el mesociclo que se cierra (FIX-1): los dos
        // cierres entran en el historial, cada uno con su propio mesociclo.
        var first = await (await client.PostAsync("/plan/close", content: null))
            .Content.ReadFromJsonAsync<CloseMesocycleResponse>();
        var second = await (await client.PostAsync("/plan/close", content: null))
            .Content.ReadFromJsonAsync<CloseMesocycleResponse>();

        Assert.NotNull(first);
        Assert.NotNull(second);
        Assert.NotEqual(first!.MesocycleId, second!.MesocycleId);

        var history = await (await client.GetAsync("/plan/history"))
            .Content.ReadFromJsonAsync<IReadOnlyList<MesocycleSummaryResponse>>();
        Assert.NotNull(history);
        Assert.Equal(2, history!.Count);
    }

    [Fact]
    public async Task Post_plan_close_keeps_the_closed_mesocycle_persisted_as_closed()
    {
        using var factory = new BarrappApiFactory();
        using var client = factory.CreateClient();
        await client.PutAsJsonAsync("/profile", PlanTestData.Profile(3));
        await client.PutAsJsonAsync("/profile/objective", new { skillId = "planche" });
        await client.PostAsync("/plan", content: null);

        var closed = await (await client.PostAsync("/plan/close", content: null))
            .Content.ReadFromJsonAsync<CloseMesocycleResponse>();

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var saved = await db.Mesocycles.SingleAsync(mesocycle => mesocycle.Id == closed!.MesocycleId);

        Assert.Equal(Domain.Planning.MesocycleStatus.Closed, saved.Status);
        Assert.NotNull(saved.ClosedAtUtc);
    }

    private static object SessionLogRequest(PlanResponse? plan, int sessionDay, params int[] values) => new
    {
        session = new
        {
            kind = plan?.MesocycleId is null ? "suelta" : "mesocycle",
            date = DateOnly.FromDateTime(DateTime.UtcNow.Date.AddDays(sessionDay - 1)),
            mesocycleId = plan?.MesocycleId,
            microcycleNumber = plan?.MesocycleId is null ? (int?)null : 1,
            sessionDay = plan?.MesocycleId is null ? (int?)null : sessionDay,
        },
        item = new
        {
            exerciseId = "push_up",
            role = "strength",
            pattern = "push",
            prescribedSets = values.Length,
            repsMin = (int?)8,
            repsMax = (int?)12,
            holdSecondsMin = (int?)null,
            holdSecondsMax = (int?)null,
            note = (string?)null,
            sets = values.Select((value, index) => new
            {
                setNumber = index + 1,
                value,
                actualRir = (int?)null,
                loadKg = (double?)null,
            }),
        },
    };
}
