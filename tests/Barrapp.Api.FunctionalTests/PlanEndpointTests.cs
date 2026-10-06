using System.Net;
using System.Net.Http.Json;
using Barrapp.Application.Features.AthleteProfiles;
using Barrapp.Application.Features.Plans;
using Microsoft.AspNetCore.Mvc;

namespace Barrapp.Api.FunctionalTests;

/// <summary>
/// El plan del mesociclo se lee con <c>GET /plan</c>: necesita perfil y objetivo guardados y, hoy,
/// genera el reparto full-body de 3 días, el alterno tren superior / tren inferior de 4 días (#15)
/// o el reparto por patrón de 5 días (#16).
/// </summary>
public sealed class PlanEndpointTests(BarrappApiFactory factory)
    : IClassFixture<BarrappApiFactory>
{
    [Fact]
    public async Task Get_plan_for_a_three_day_profile_returns_four_weeks_with_three_sessions()
    {
        using var client = factory.CreateClient();
        await client.PutAsJsonAsync("/profile", PlanTestData.Profile(3));
        await client.PutAsJsonAsync("/profile/objective", new { skillId = "planche" });

        using var response = await client.GetAsync("/plan");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var plan = await response.Content.ReadFromJsonAsync<PlanResponse>();
        Assert.NotNull(plan);
        Assert.Equal("planche", plan!.SkillId);
        Assert.Equal(3, plan.TrainingDays);
        Assert.Equal(4, plan.Microcycles.Count);
        Assert.All(plan.Microcycles, microcycle => Assert.Equal(3, microcycle.Sessions.Count));
        Assert.All(
            plan.Microcycles,
            microcycle => Assert.Equal(new[] { 1, 2, 3 }, microcycle.Sessions.Select(session => session.Day)));

        // Cada sesión arranca por el bloque de skill, con su nombre en español resuelto.
        Assert.All(
            plan.Microcycles.SelectMany(microcycle => microcycle.Sessions),
            session =>
            {
                Assert.Equal("skill", session.Items[0].Role);
                Assert.False(string.IsNullOrWhiteSpace(session.Items[0].ExerciseName));
            });

        // El reparto full-body cubre los tres patrones en cada semana.
        Assert.All(
            plan.Microcycles,
            microcycle =>
            {
                var patterns = microcycle.Sessions
                    .SelectMany(session => session.Items)
                    .Where(item => item.Role == "strength")
                    .Select(item => item.Pattern)
                    .ToHashSet();

                Assert.Contains("push", patterns);
                Assert.Contains("pull", patterns);
                Assert.Contains("leg", patterns);
            });
    }

    [Fact]
    public async Task Get_plan_practises_the_current_stage_of_the_objective_skill()
    {
        using var client = factory.CreateClient();
        await client.PutAsJsonAsync("/profile", PlanTestData.Profile(3));
        await client.PutAsJsonAsync("/profile/objective", new { skillId = "planche" });
        await client.PutAsJsonAsync("/catalog/progress/planche", new { stageOrder = 2 });

        using var response = await client.GetAsync("/plan");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var plan = await response.Content.ReadFromJsonAsync<PlanResponse>();
        Assert.NotNull(plan);

        // La etapa 2 de planche es «Planche agrupada» (planche-tuck): cada sesión la practica primero.
        Assert.All(
            plan!.Microcycles.SelectMany(microcycle => microcycle.Sessions),
            session => Assert.Equal("planche-tuck", session.Items[0].ExerciseId));
    }

    [Fact]
    public async Task Get_plan_adjusts_the_skill_block_and_its_note_by_the_athlete_lever()
    {
        using var client = factory.CreateClient();
        await client.PutAsJsonAsync(
            "/profile",
            new AthleteProfileResponse(
                100,
                185,
                190,
                80,
                3,
                [
                    new MaximumResponse("push_up", 10),
                    new MaximumResponse("pull_up", 5),
                    new MaximumResponse("squat", 20),
                ]));
        await client.PutAsJsonAsync("/profile/objective", new { skillId = "planche" });

        using var response = await client.GetAsync("/plan");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var plan = await response.Content.ReadFromJsonAsync<PlanResponse>();
        Assert.NotNull(plan);

        var skillBlocks = plan!.Microcycles
            .SelectMany(microcycle => microcycle.Sessions)
            .Select(session => session.Items[0])
            .ToList();

        // La escalera de planche arranca en 3 series; una palanca desfavorable pide una más.
        Assert.All(skillBlocks, block => Assert.Equal(4, block.Sets));
        Assert.All(skillBlocks, block => Assert.Contains("lento", block.Note));
    }

    [Fact]
    public async Task Get_plan_for_a_four_day_profile_alternates_upper_and_lower_sessions()
    {
        using var client = factory.CreateClient();
        await client.PutAsJsonAsync("/profile", PlanTestData.Profile(4));
        await client.PutAsJsonAsync("/profile/objective", new { skillId = "planche" });

        using var response = await client.GetAsync("/plan");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var plan = await response.Content.ReadFromJsonAsync<PlanResponse>();
        Assert.NotNull(plan);
        Assert.Equal("planche", plan!.SkillId);
        Assert.Equal(4, plan.TrainingDays);
        Assert.Equal(4, plan.Microcycles.Count);
        Assert.All(plan.Microcycles, microcycle => Assert.Equal(4, microcycle.Sessions.Count));
        Assert.All(
            plan.Microcycles,
            microcycle => Assert.Equal(new[] { 1, 2, 3, 4 }, microcycle.Sessions.Select(session => session.Day)));

        // Días 1 y 3: tren superior, arrancan con el skill y cubren empuje y tirón. Días 2 y 4:
        // tren inferior, sin skill y con pierna.
        Assert.All(
            plan.Microcycles,
            microcycle =>
            {
                var upperItems = microcycle.Sessions
                    .Where(session => session.Day is 1 or 3)
                    .SelectMany(session => session.Items)
                    .ToList();
                Assert.Equal("skill", upperItems[0].Role);
                Assert.Contains(upperItems, item => item.Role == "strength" && item.Pattern == "push");
                Assert.Contains(upperItems, item => item.Role == "strength" && item.Pattern == "pull");

                var lowerItems = microcycle.Sessions
                    .Where(session => session.Day is 2 or 4)
                    .SelectMany(session => session.Items)
                    .ToList();
                Assert.DoesNotContain(lowerItems, item => item.Role == "skill");
                Assert.Contains(lowerItems, item => item.Role == "strength" && item.Pattern == "leg");
            });
    }

    [Fact]
    public async Task Get_plan_for_a_five_day_profile_splits_every_session_by_pattern()
    {
        using var client = factory.CreateClient();
        await client.PutAsJsonAsync("/profile", PlanTestData.Profile(5));
        await client.PutAsJsonAsync("/profile/objective", new { skillId = "planche" });

        using var response = await client.GetAsync("/plan");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var plan = await response.Content.ReadFromJsonAsync<PlanResponse>();
        Assert.NotNull(plan);
        Assert.Equal("planche", plan!.SkillId);
        Assert.Equal(5, plan.TrainingDays);
        Assert.Equal(4, plan.Microcycles.Count);
        Assert.All(plan.Microcycles, microcycle => Assert.Equal(5, microcycle.Sessions.Count));
        Assert.All(
            plan.Microcycles,
            microcycle => Assert.Equal(new[] { 1, 2, 3, 4, 5 }, microcycle.Sessions.Select(session => session.Day)));

        // Reparto por patrón (#16): D1 empuje+skill, D2 tirón, D3 pierna, D4 empuje+skill y
        // D5 tirón+pierna; el bloque de skill abre todas las sesiones.
        Assert.All(
            plan.Microcycles,
            microcycle =>
            {
                var strengthByDay = microcycle.Sessions.ToDictionary(
                    session => session.Day,
                    session => session.Items
                        .Where(item => item.Role == "strength")
                        .Select(item => item.Pattern)
                        .ToArray());

                Assert.Equal(new[] { "push" }, strengthByDay[1]);
                Assert.Equal(new[] { "pull" }, strengthByDay[2]);
                Assert.Equal(new[] { "leg" }, strengthByDay[3]);
                Assert.Equal(new[] { "push" }, strengthByDay[4]);
                Assert.Equal(new[] { "pull", "leg" }, strengthByDay[5]);

                Assert.All(microcycle.Sessions, session => Assert.Equal("skill", session.Items[0].Role));
            });
    }
}

/// <summary>Clase aparte para arrancar una base vacía: sin perfil no hay plan.</summary>
public sealed class PlanWithoutProfileEndpointTests(BarrappApiFactory factory)
    : IClassFixture<BarrappApiFactory>
{
    [Fact]
    public async Task Get_plan_without_a_profile_returns_404_with_a_spanish_detail()
    {
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/plan");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(problem);
        Assert.Contains("No hay ningún perfil guardado para este atleta.", problem!.Detail);
    }
}

/// <summary>Clase aparte: guarda el perfil pero no el objetivo.</summary>
public sealed class PlanWithoutObjectiveEndpointTests(BarrappApiFactory factory)
    : IClassFixture<BarrappApiFactory>
{
    [Fact]
    public async Task Get_plan_without_an_objective_returns_404_with_a_spanish_detail()
    {
        using var client = factory.CreateClient();
        await client.PutAsJsonAsync("/profile", PlanTestData.Profile(3));

        using var response = await client.GetAsync("/plan");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(problem);
        Assert.Contains("No hay ningún objetivo guardado para este atleta.", problem!.Detail);
    }
}

/// <summary>Datos compartidos por los tests de plan.</summary>
internal static class PlanTestData
{
    public static AthleteProfileResponse Profile(int trainingDays) => new(
        78,
        180,
        180,
        85,
        trainingDays,
        [
            new MaximumResponse("push_up", 10),
            new MaximumResponse("pull_up", 5),
            new MaximumResponse("squat", 20),
        ]);
}
