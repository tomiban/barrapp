using System.Net;
using System.Net.Http.Json;
using Barrapp.Application.Features.SkillProgress;
using Microsoft.AspNetCore.Mvc;

namespace Barrapp.Api.FunctionalTests;

/// <summary>
/// La etapa actual por skill se fija con <c>PUT</c> y se lee con <c>GET</c>, y solo admite etapas
/// de la escalera del skill.
/// </summary>
public sealed class SkillProgressEndpointTests(BarrappApiFactory factory)
    : IClassFixture<BarrappApiFactory>
{
    [Fact]
    public async Task Put_progress_then_get_progress_returns_the_saved_stage()
    {
        using var client = factory.CreateClient();

        using var putResponse = await client.PutAsJsonAsync(
            "/catalog/progress/planche",
            new { stageOrder = 2 });
        Assert.Equal(HttpStatusCode.OK, putResponse.StatusCode);

        var saved = await putResponse.Content.ReadFromJsonAsync<SkillProgressResponse>();
        Assert.NotNull(saved);
        Assert.Equal("planche", saved!.SkillId);
        Assert.Equal(2, saved.StageOrder);

        var progress = await client.GetFromJsonAsync<List<SkillProgressResponse>>("/catalog/progress");
        Assert.NotNull(progress);
        Assert.Equal(4, progress!.Count);
        Assert.Equal(2, progress.Single(entry => entry.SkillId == "planche").StageOrder);
    }

    [Fact]
    public async Task Put_progress_overwrites_the_previous_stage()
    {
        using var client = factory.CreateClient();

        await client.PutAsJsonAsync("/catalog/progress/planche", new { stageOrder = 2 });

        using var response = await client.PutAsJsonAsync(
            "/catalog/progress/planche",
            new { stageOrder = 4 });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var saved = await response.Content.ReadFromJsonAsync<SkillProgressResponse>();
        Assert.NotNull(saved);
        Assert.Equal(4, saved!.StageOrder);
    }

    [Fact]
    public async Task Put_progress_with_a_stage_outside_the_ladder_returns_400()
    {
        using var client = factory.CreateClient();

        using var response = await client.PutAsJsonAsync(
            "/catalog/progress/planche",
            new { stageOrder = 99 });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(problem);
        Assert.Contains("La etapa indicada no existe en la escalera del skill.", problem!.Detail);
    }

    [Fact]
    public async Task Put_progress_with_an_unknown_skill_returns_400()
    {
        using var client = factory.CreateClient();

        using var response = await client.PutAsJsonAsync(
            "/catalog/progress/ghost",
            new { stageOrder = 1 });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(problem);
        Assert.Contains("El skill indicado no existe en el catálogo.", problem!.Detail);
    }

    [Fact]
    public async Task Put_progress_with_a_stage_below_one_returns_400()
    {
        using var client = factory.CreateClient();

        using var response = await client.PutAsJsonAsync(
            "/catalog/progress/planche",
            new { stageOrder = 0 });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}

/// <summary>
/// Clase aparte para arrancar una base vacía: sin filas, todos los skills parten de la etapa 1.
/// </summary>
public sealed class SkillProgressDefaultEndpointTests(BarrappApiFactory factory)
    : IClassFixture<BarrappApiFactory>
{
    [Fact]
    public async Task Get_progress_without_saved_rows_returns_every_skill_at_the_first_stage()
    {
        using var client = factory.CreateClient();

        var progress = await client.GetFromJsonAsync<List<SkillProgressResponse>>("/catalog/progress");

        Assert.NotNull(progress);
        Assert.Equal(
            ["handstand", "front-lever", "planche", "pistol-squat"],
            progress!.Select(entry => entry.SkillId));
        Assert.All(progress, entry => Assert.Equal(1, entry.StageOrder));
    }
}
