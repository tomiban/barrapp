using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Barrapp.Application.Features.AthleteProfiles;
using Barrapp.Application.Features.Plans;
using Barrapp.Domain.Planning;
using Barrapp.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Barrapp.Api.FunctionalTests;

/// <summary>
/// <c>POST /plan</c> genera el mesociclo (#27, D7) corriendo el mismo motor que <c>GET /plan</c>,
/// pero además lo persiste: reemplaza el mesociclo activo del atleta y devuelve el plan con el
/// mismo contrato que la lectura. A partir de ahí, <c>GET /plan</c> sirve el mesociclo guardado en
/// lugar de volver a generarlo.
/// </summary>
/// <remarks>Cada test usa su propia <see cref="BarrappApiFactory"/> (base efímera) para no
/// depender del estado que deja otro test de la clase.</remarks>
public sealed class GeneratePlanEndpointTests
{
    [Fact]
    public async Task Post_plan_with_a_start_date_starts_on_the_first_training_day_on_or_after_it()
    {
        using var factory = new BarrappApiFactory();
        using var client = factory.CreateClient();
        await client.PutAsJsonAsync(
            "/profile",
            PlanTestData.Profile(3, ["tuesday", "thursday", "saturday"]));
        await client.PutAsJsonAsync("/profile/objective", new { skillId = "planche" });

        // Lunes 2 de marzo de 2026: el primer día de entrenamiento en o después es el martes 3.
        using var response = await client.PostAsJsonAsync("/plan", new { startDate = "2026-03-02" });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var plan = await response.Content.ReadFromJsonAsync<PlanResponse>();
        Assert.NotNull(plan);
        Assert.Equal(new DateOnly(2026, 3, 3), plan!.StartDate);
        Assert.Equal(
            [new DateOnly(2026, 3, 3), new DateOnly(2026, 3, 5), new DateOnly(2026, 3, 7)],
            plan.Microcycles[0].Sessions.Select(session => session.Date));
    }

    [Fact]
    public async Task Post_plan_persists_the_start_date_of_the_mesocycle()
    {
        using var factory = new BarrappApiFactory();
        using var client = factory.CreateClient();
        await client.PutAsJsonAsync(
            "/profile",
            PlanTestData.Profile(3, ["tuesday", "thursday", "saturday"]));
        await client.PutAsJsonAsync("/profile/objective", new { skillId = "planche" });
        await client.PostAsJsonAsync("/plan", new { startDate = "2026-03-02" });

        using var response = await client.GetAsync("/plan");
        var plan = await response.Content.ReadFromJsonAsync<PlanResponse>();

        Assert.NotNull(plan);
        Assert.Equal(new DateOnly(2026, 3, 3), plan!.StartDate);
    }

    [Fact]
    public async Task Post_plan_without_a_start_date_starts_the_mesocycle_today()
    {
        using var factory = new BarrappApiFactory();
        using var client = factory.CreateClient();
        await client.PutAsJsonAsync(
            "/profile",
            PlanTestData.Profile(3, ["tuesday", "thursday", "saturday"]));
        await client.PutAsJsonAsync("/profile/objective", new { skillId = "planche" });

        using var response = await client.PostAsync("/plan", content: null);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var plan = await response.Content.ReadFromJsonAsync<PlanResponse>();
        Assert.NotNull(plan);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        Assert.InRange(plan!.StartDate, today, today.AddDays(6));
        Assert.All(
            plan.Microcycles.SelectMany(microcycle => microcycle.Sessions),
            session => Assert.NotNull(session.Date));
    }

    [Fact]
    public async Task Post_plan_without_a_profile_returns_404_with_a_spanish_detail()
    {
        using var factory = new BarrappApiFactory();
        using var client = factory.CreateClient();

        using var response = await client.PostAsync("/plan", content: null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(problem);
        Assert.Contains("No hay ningún perfil guardado para este atleta.", problem!.Detail);
    }

    [Fact]
    public async Task Post_plan_without_an_objective_returns_404_with_a_spanish_detail()
    {
        using var factory = new BarrappApiFactory();
        using var client = factory.CreateClient();
        await client.PutAsJsonAsync("/profile", PlanTestData.Profile(3));

        using var response = await client.PostAsync("/plan", content: null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(problem);
        Assert.Contains("No hay ningún objetivo guardado para este atleta.", problem!.Detail);
    }

    [Fact]
    public async Task Post_plan_generates_and_returns_the_same_contract_as_get()
    {
        using var factory = new BarrappApiFactory();
        using var client = factory.CreateClient();
        await client.PutAsJsonAsync("/profile", PlanTestData.Profile(3));
        await client.PutAsJsonAsync("/profile/objective", new { skillId = "planche" });

        using var generatedResponse = await client.PostAsync("/plan", content: null);
        Assert.Equal(HttpStatusCode.OK, generatedResponse.StatusCode);
        var generated = await generatedResponse.Content.ReadFromJsonAsync<PlanResponse>();

        using var readResponse = await client.GetAsync("/plan");
        var read = await readResponse.Content.ReadFromJsonAsync<PlanResponse>();

        Assert.NotNull(generated);
        Assert.Equal(Serialize(generated!), Serialize(read!));
        Assert.Equal("planche", generated!.SkillId);
        Assert.Equal(3, generated.TrainingDays);
        Assert.Equal(4, generated.Microcycles.Count);
    }

    [Fact]
    public async Task Post_plan_persists_the_active_mesocycle_that_get_plan_serves()
    {
        using var factory = new BarrappApiFactory();
        using var client = factory.CreateClient();
        await client.PutAsJsonAsync("/profile", PlanTestData.Profile(3)); // push_up 10
        await client.PutAsJsonAsync("/profile/objective", new { skillId = "planche" });

        var generated = await (await client.PostAsync("/plan", content: null))
            .Content.ReadFromJsonAsync<PlanResponse>();

        await client.PutAsJsonAsync(
            "/profile",
            new AthleteProfileResponse(
                78,
                180,
                180,
                85,
                3,
                [
                    new MaximumResponse("push_up", 20),
                    new MaximumResponse("pull_up", 5),
                    new MaximumResponse("squat", 20),
                ],
                ["monday", "wednesday", "friday"]));

        var served = await (await client.GetAsync("/plan")).Content.ReadFromJsonAsync<PlanResponse>();

        // GET devuelve el mesociclo persistido (máximo 10 → reps 5–7), no uno regenerado con el
        // máximo nuevo (que prescribiría 15–17).
        Assert.Equal(Serialize(generated!), Serialize(served!));
        var pushUp = served!.Microcycles[0].Sessions[0].Items.Single(item => item.ExerciseId == "push_up");
        Assert.Equal(5, pushUp.RepsMin);
        Assert.Equal(7, pushUp.RepsMax);
    }

    [Fact]
    public async Task Post_plan_twice_replaces_the_active_mesocycle()
    {
        using var factory = new BarrappApiFactory();
        using var client = factory.CreateClient();
        await client.PutAsJsonAsync("/profile", PlanTestData.Profile(3));
        await client.PutAsJsonAsync("/profile/objective", new { skillId = "planche" });
        await client.PostAsync("/plan", content: null);

        // El atleta cambia de objetivo y regenera: el activo anterior se reemplaza.
        await client.PutAsJsonAsync("/profile/objective", new { skillId = "handstand" });
        await client.PostAsync("/plan", content: null);

        var plan = await (await client.GetAsync("/plan")).Content.ReadFromJsonAsync<PlanResponse>();
        Assert.Equal("handstand", plan!.SkillId);

        // En la base solo queda un mesociclo activo (el índice filtrado no admitiría dos).
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var actives = await db.Mesocycles
            .Where(mesocycle => mesocycle.Status == MesocycleStatus.Active)
            .ToListAsync();
        Assert.Single(actives);
        Assert.NotNull(plan.MesocycleId);
        Assert.Equal(actives[0].Id, plan.MesocycleId);
    }

    /// <summary>Los records no comparan colecciones estructuralmente; JSON sí.</summary>
    private static string Serialize(PlanResponse plan) => JsonSerializer.Serialize(plan);
}
