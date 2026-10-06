using System.Net;
using System.Net.Http.Json;
using Barrapp.Application.Features.AthleteProfiles;
using Barrapp.Application.Features.Plans;
using Microsoft.AspNetCore.Mvc;

namespace Barrapp.Api.FunctionalTests;

/// <summary>
/// El plan del mesociclo se lee con <c>GET /plan</c>: necesita perfil y objetivo guardados y, hoy,
/// solo sabe generar el reparto full-body de 3 días.
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
    public async Task Get_plan_for_an_unsupported_frequency_returns_400_with_a_spanish_detail()
    {
        using var client = factory.CreateClient();
        await client.PutAsJsonAsync("/profile", PlanTestData.Profile(5));
        await client.PutAsJsonAsync("/profile/objective", new { skillId = "planche" });

        using var response = await client.GetAsync("/plan");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(problem);
        Assert.Contains("Por ahora solo se puede generar un plan de 3 días.", problem!.Detail);
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
