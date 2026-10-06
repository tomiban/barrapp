using System.Net;
using System.Net.Http.Json;
using Barrapp.Application.Features.Plans;
using Barrapp.Domain.Planning;
using Barrapp.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Barrapp.Api.FunctionalTests;

/// <summary>
/// Historial de mesociclos (spec 0001, US-29; ticket #27): <c>GET /plan/history</c> lista los
/// mesociclos pasados (cerrados), más reciente primero, y <c>GET /plan/history/{id}</c> abre el
/// detalle de uno —el plan tal y como se guardó— con el mismo contrato que <c>GET /plan</c>. El
/// cierre del mesociclo activo es del ticket #24; aquí se siembra el Estado cerrado vía el agregado.
/// </summary>
/// <remarks>Cada test usa su propia <see cref="BarrappApiFactory"/> (base efímera): el historial
/// de cada uno depende del número de mesociclos que siembra, así que no se puede compartir base
/// entre tests.</remarks>
public sealed class MesocycleHistoryEndpointTests
{
    [Fact]
    public async Task History_without_closed_mesocycles_returns_an_empty_list()
    {
        using var factory = new BarrappApiFactory();
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/plan/history");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var history = await response.Content.ReadFromJsonAsync<IReadOnlyList<MesocycleSummaryResponse>>();
        Assert.NotNull(history);
        Assert.Empty(history!);
    }

    [Fact]
    public async Task History_lists_closed_mesocycles_newest_first_with_a_summary()
    {
        using var factory = new BarrappApiFactory();
        var first = DateTimeOffset.Parse("2026-10-31T20:00:00+00:00");
        var second = DateTimeOffset.Parse("2026-11-30T20:00:00+00:00");
        await SeedClosedMesocycleAsync(factory, first);
        await SeedClosedMesocycleAsync(factory, second);

        using var client = factory.CreateClient();
        using var response = await client.GetAsync("/plan/history");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var history = await response.Content.ReadFromJsonAsync<IReadOnlyList<MesocycleSummaryResponse>>();

        Assert.NotNull(history);
        Assert.Equal(2, history!.Count);
        Assert.Equal(second, history[0].ClosedAtUtc);
        Assert.Equal(first, history[1].ClosedAtUtc);
        Assert.All(
            history,
            entry =>
            {
                Assert.NotEqual(Guid.Empty, entry.Id);
                Assert.Equal("planche", entry.SkillId);
                Assert.Equal("Planche", entry.SkillName);
                Assert.Equal(3, entry.TrainingDays);
                Assert.NotEqual(default, entry.StartedAtUtc);
            });
    }

    [Fact]
    public async Task History_detail_opens_a_closed_mesocycle_with_its_plan()
    {
        using var factory = new BarrappApiFactory();
        var closedAtUtc = DateTimeOffset.Parse("2026-10-31T20:00:00+00:00");
        var mesocycleId = await SeedClosedMesocycleAsync(factory, closedAtUtc);

        using var client = factory.CreateClient();
        using var response = await client.GetAsync($"/plan/history/{mesocycleId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var plan = await response.Content.ReadFromJsonAsync<PlanResponse>();

        Assert.NotNull(plan);
        Assert.Equal("planche", plan!.SkillId);
        Assert.Equal(3, plan.TrainingDays);
        Assert.Equal(4, plan.Microcycles.Count);
        Assert.All(plan.Microcycles, microcycle => Assert.Equal(3, microcycle.Sessions.Count));
        Assert.Equal(1, plan.SkillStage.Order);
    }

    [Fact]
    public async Task History_detail_works_for_the_active_mesocycle_too()
    {
        using var factory = new BarrappApiFactory();
        using var client = factory.CreateClient();
        await client.PutAsJsonAsync("/profile", PlanTestData.Profile(3));
        await client.PutAsJsonAsync("/profile/objective", new { skillId = "planche" });
        await client.PostAsync("/plan", content: null);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var active = await db.Mesocycles.FirstAsync(mesocycle => mesocycle.Status == MesocycleStatus.Active);

        using var response = await client.GetAsync($"/plan/history/{active.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var plan = await response.Content.ReadFromJsonAsync<PlanResponse>();
        Assert.Equal("planche", plan!.SkillId);
    }

    [Fact]
    public async Task History_detail_of_an_unknown_id_returns_404_with_a_spanish_detail()
    {
        using var factory = new BarrappApiFactory();
        using var client = factory.CreateClient();
        var unknownId = Guid.NewGuid();

        using var response = await client.GetAsync($"/plan/history/{unknownId}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(problem);
        Assert.Contains("No hay ningún mesociclo guardado con ese identificador.", problem!.Detail);
    }

    /// <summary>
    /// Genera un mesociclo vía API (POST /plan) y lo cierra con el agregado por debajo, como hará
    /// el ticket #24: deja el Estado que alimenta el historial.
    /// </summary>
    private static async Task<Guid> SeedClosedMesocycleAsync(BarrappApiFactory factory, DateTimeOffset closedAtUtc)
    {
        using var client = factory.CreateClient();
        await client.PutAsJsonAsync("/profile", PlanTestData.Profile(3));
        await client.PutAsJsonAsync("/profile/objective", new { skillId = "planche" });
        await client.PostAsync("/plan", content: null);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var active = await db.Mesocycles.SingleAsync(mesocycle => mesocycle.Status == MesocycleStatus.Active);
        Assert.True(active.Close(closedAtUtc).IsSuccess);
        await db.SaveChangesAsync();

        return active.Id;
    }
}