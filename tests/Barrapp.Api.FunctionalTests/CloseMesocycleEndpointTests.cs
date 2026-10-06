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
/// y lo publica en el historial. Devuelve 409 si no hay mesociclo en curso y 404 si no hay perfil.
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
    public async Task Post_plan_close_without_an_active_mesocycle_returns_409_with_a_spanish_detail()
    {
        using var factory = new BarrappApiFactory();
        using var client = factory.CreateClient();
        await client.PutAsJsonAsync("/profile", PlanTestData.Profile(3));
        await client.PutAsJsonAsync("/profile/objective", new { skillId = "planche" });

        using var response = await client.PostAsync("/plan/close", content: null);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(problem);
        Assert.Contains("No hay ningún mesociclo en curso para cerrar.", problem!.Detail);
    }

    [Fact]
    public async Task Post_plan_close_raises_maximums_and_the_next_plan_reflects_them()
    {
        using var factory = new BarrappApiFactory();
        using var client = factory.CreateClient();
        await client.PutAsJsonAsync("/profile", PlanTestData.Profile(3)); // push_up 10
        await client.PutAsJsonAsync("/profile/objective", new { skillId = "planche" });
        await client.PostAsync("/plan", content: null);

        // El atleta logra 14 flexiones en el mesociclo (marca mejor que su máximo de 10).
        var registered = await client.PostAsJsonAsync(
            "/session-logs",
            new
            {
                exerciseId = "push_up",
                mesocycleId = (Guid?)null,
                sessionDay = 1,
                sets = new[]
                {
                    new { setNumber = 1, value = 14, effort = (int?)null },
                    new { setNumber = 2, value = 14, effort = (int?)null },
                },
            });
        Assert.Equal(HttpStatusCode.OK, registered.StatusCode);

        using var closeResponse = await client.PostAsync("/plan/close", content: null);
        Assert.Equal(HttpStatusCode.OK, closeResponse.StatusCode);
        var closed = await closeResponse.Content.ReadFromJsonAsync<CloseMesocycleResponse>();

        Assert.NotNull(closed);
        Assert.NotEqual(Guid.Empty, closed!.MesocycleId);
        Assert.Equal("planche", closed.SkillId);
        Assert.Contains(closed.Maximums, maximum => maximum.ExerciseCode == "push_up" && maximum.Repetitions == 14);

        // Sin mesociclo activo, GET /plan regenera con el máximo ajustado: semana 1 RIR 3 → 9–11.
        var plan = await (await client.GetAsync("/plan")).Content.ReadFromJsonAsync<PlanResponse>();
        var pushUp = plan!.Microcycles[0].Sessions[0].Items.Single(item => item.ExerciseId == "push_up");
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

        await client.PostAsJsonAsync(
            "/session-logs",
            new
            {
                exerciseId = "push_up",
                mesocycleId = (Guid?)null,
                sessionDay = 1,
                sets = new[]
                {
                    new { setNumber = 1, value = 8, effort = (int?)null },
                    new { setNumber = 2, value = 9, effort = (int?)null },
                },
            });

        using var closeResponse = await client.PostAsync("/plan/close", content: null);
        var closed = await closeResponse.Content.ReadFromJsonAsync<CloseMesocycleResponse>();

        Assert.Equal(10, closed!.Maximums.Single(maximum => maximum.ExerciseCode == "push_up").Repetitions);

        // La prescripción se mantiene con el máximo sin tocar: semana 1 RIR 3 → 5–7.
        var plan = await (await client.GetAsync("/plan")).Content.ReadFromJsonAsync<PlanResponse>();
        var pushUp = plan!.Microcycles[0].Sessions[0].Items.Single(item => item.ExerciseId == "push_up");
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
    public async Task Post_plan_close_twice_returns_409_with_a_spanish_detail()
    {
        using var factory = new BarrappApiFactory();
        using var client = factory.CreateClient();
        await client.PutAsJsonAsync("/profile", PlanTestData.Profile(3));
        await client.PutAsJsonAsync("/profile/objective", new { skillId = "planche" });
        await client.PostAsync("/plan", content: null);
        await client.PostAsync("/plan/close", content: null);

        using var second = await client.PostAsync("/plan/close", content: null);

        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
        var problem = await second.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.Contains("No hay ningún mesociclo en curso para cerrar.", problem!.Detail);
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
}
