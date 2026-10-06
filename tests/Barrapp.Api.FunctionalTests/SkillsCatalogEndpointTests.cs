using System.Net;
using System.Net.Http.Json;
using Barrapp.Application.Features.Catalog;

namespace Barrapp.Api.FunctionalTests;

/// <summary>
/// El catálogo de skills se sirve con su escalera de progresión y sus rutinas de patrón, con el
/// patrón y la métrica en minúsculas, y con el flag de palanca de cada skill.
/// </summary>
public sealed class SkillsCatalogEndpointTests(BarrappApiFactory factory)
    : IClassFixture<BarrappApiFactory>
{
    [Fact]
    public async Task Get_skills_returns_every_skill_with_its_pattern_and_lever()
    {
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/catalog/skills");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var skills = await response.Content.ReadFromJsonAsync<List<SkillResponse>>();
        Assert.NotNull(skills);
        Assert.Equal(["handstand", "front-lever", "planche", "pistol-squat"], skills!.Select(s => s.Id));

        var planche = skills.Single(skill => skill.Id == "planche");
        Assert.Equal("Planche", planche.Name);
        Assert.Equal("push", planche.Group);
        Assert.True(planche.Lever);

        var frontLever = skills.Single(skill => skill.Id == "front-lever");
        Assert.Equal("pull", frontLever.Group);
        Assert.True(frontLever.Lever);

        Assert.False(skills.Single(skill => skill.Id == "handstand").Lever);
        Assert.False(skills.Single(skill => skill.Id == "pistol-squat").Lever);
    }

    [Fact]
    public async Task Get_skills_includes_the_ladder_with_criteria_and_the_pattern_routines()
    {
        using var client = factory.CreateClient();

        var skills = await client.GetFromJsonAsync<List<SkillResponse>>("/catalog/skills");

        Assert.NotNull(skills);

        var planche = skills!.Single(skill => skill.Id == "planche");

        Assert.Equal(5, planche.Stages.Count);
        var firstStage = planche.Stages[0];
        Assert.Equal(1, firstStage.Order);
        Assert.Equal("planche-lean", firstStage.ExerciseId);
        Assert.Equal("seconds", firstStage.Criterion.Metric);
        Assert.True(firstStage.Criterion.Target > 0);
        Assert.True(firstStage.Criterion.Sets > 0);
        Assert.False(string.IsNullOrWhiteSpace(firstStage.Notes));

        // Planche trae varios modelos (R1–R5) con sus filas.
        Assert.Equal(5, planche.PatternRoutines.Count);
        var firstRoutine = planche.PatternRoutines[0];
        Assert.Equal("r1", firstRoutine.Id);
        Assert.Equal("Modelo R1", firstRoutine.Name);
        Assert.Equal(2, firstRoutine.Intensity);
        Assert.False(string.IsNullOrWhiteSpace(firstRoutine.Equipment));
        Assert.NotEmpty(firstRoutine.Items);

        var holdItem = firstRoutine.Items[0];
        Assert.Equal("planche-tuck", holdItem.ExerciseId);
        Assert.True(holdItem.Sets > 0);
        Assert.NotNull(holdItem.HoldSecondsMin);
        Assert.NotNull(holdItem.HoldSecondsMax);
        Assert.True(holdItem.RestSeconds > 0);

        // El pistol squat usa repeticiones en su criterio.
        var pistol = skills.Single(skill => skill.Id == "pistol-squat");
        Assert.All(pistol.Stages, stage => Assert.Equal("reps", stage.Criterion.Metric));
    }
}
