using System.Net;
using System.Net.Http.Json;
using Barrapp.Application.Features.Catalog;

namespace Barrapp.Api.FunctionalTests;

/// <summary>
/// El catálogo de ejercicios se sirve agrupado por patrón, en el orden del dominio
/// (push, pull, leg, core, cardio) y con las métricas en minúsculas.
/// </summary>
public sealed class CatalogEndpointTests(BarrappApiFactory factory)
    : IClassFixture<BarrappApiFactory>
{
    [Fact]
    public async Task Get_exercises_returns_the_catalog_grouped_by_pattern_in_order()
    {
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/catalog/exercises");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var catalog = await response.Content.ReadFromJsonAsync<ExerciseCatalogResponse>();
        Assert.NotNull(catalog);
        Assert.Equal(
            ["push", "pull", "leg", "core", "cardio"],
            catalog!.Groups.Select(group => group.Group));
        Assert.All(catalog.Groups, group => Assert.NotEmpty(group.Exercises));
    }

    [Fact]
    public async Task Get_exercises_includes_cardio_and_keeps_metric_and_regression()
    {
        using var client = factory.CreateClient();

        var catalog = await client.GetFromJsonAsync<ExerciseCatalogResponse>("/catalog/exercises");

        Assert.NotNull(catalog);

        var cardio = catalog!.Groups.Single(group => group.Group == "cardio");
        Assert.Contains(cardio.Exercises, exercise => exercise.Id == "burpees");

        var pushUp = catalog.Groups
            .Single(group => group.Group == "push")
            .Exercises.Single(exercise => exercise.Id == "push_up");
        Assert.Equal("Flexiones", pushUp.Name);
        Assert.Equal("reps", pushUp.Metric);
        Assert.True(pushUp.TracksMaximum);
        Assert.Equal("incline-push-up", pushUp.RegressionId);
        Assert.Null(pushUp.SkillId);

        var wallSit = catalog.Groups
            .Single(group => group.Group == "leg")
            .Exercises.Single(exercise => exercise.Id == "wall-sit");
        Assert.Equal("seconds", wallSit.Metric);
        Assert.False(wallSit.TracksMaximum);
    }
}
