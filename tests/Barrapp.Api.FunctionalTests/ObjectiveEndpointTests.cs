using System.Net;
using System.Net.Http.Json;
using Barrapp.Application.Features.Objectives;
using Microsoft.AspNetCore.Mvc;

namespace Barrapp.Api.FunctionalTests;

/// <summary>
/// El objetivo del mesociclo se fija con <c>PUT</c> y se lee con <c>GET</c>, y solo admite
/// skills del catálogo.
/// </summary>
public sealed class ObjectiveEndpointTests(BarrappApiFactory factory)
    : IClassFixture<BarrappApiFactory>
{
    [Fact]
    public async Task Put_objective_then_get_objective_returns_the_chosen_skill()
    {
        using var client = factory.CreateClient();

        using var putResponse = await client.PutAsJsonAsync("/profile/objective", new { skillId = "handstand" });
        Assert.Equal(HttpStatusCode.OK, putResponse.StatusCode);

        var saved = await putResponse.Content.ReadFromJsonAsync<ObjectiveResponse>();
        Assert.NotNull(saved);
        Assert.Equal("handstand", saved!.SkillId);

        using var getResponse = await client.GetAsync("/profile/objective");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        var loaded = await getResponse.Content.ReadFromJsonAsync<ObjectiveResponse>();
        Assert.NotNull(loaded);
        Assert.Equal("handstand", loaded!.SkillId);
    }

    [Fact]
    public async Task Put_objective_with_an_unknown_skill_returns_400_with_a_spanish_detail()
    {
        using var client = factory.CreateClient();

        using var response = await client.PutAsJsonAsync(
            "/profile/objective",
            new { skillId = "double-backflip" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(problem);
        Assert.Contains("El skill indicado no existe en el catálogo.", problem!.Detail);
    }

    [Fact]
    public async Task Put_objective_with_an_empty_skill_returns_400()
    {
        using var client = factory.CreateClient();

        using var response = await client.PutAsJsonAsync("/profile/objective", new { skillId = "" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(problem);
        Assert.Contains("Debes elegir un skill objetivo.", problem!.Detail);
    }

    [Fact]
    public async Task Put_objective_overwrites_a_previous_objective()
    {
        using var client = factory.CreateClient();

        await client.PutAsJsonAsync("/profile/objective", new { skillId = "handstand" });

        using var response = await client.PutAsJsonAsync("/profile/objective", new { skillId = "planche" });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var saved = await response.Content.ReadFromJsonAsync<ObjectiveResponse>();
        Assert.NotNull(saved);
        Assert.Equal("planche", saved!.SkillId);

        var loaded = await client.GetFromJsonAsync<ObjectiveResponse>("/profile/objective");
        Assert.NotNull(loaded);
        Assert.Equal("planche", loaded!.SkillId);
    }
}

/// <summary>
/// Clase aparte para arrancar una base vacía: el objetivo no existe hasta que se elige.
/// </summary>
public sealed class ObjectiveNotFoundEndpointTests(BarrappApiFactory factory)
    : IClassFixture<BarrappApiFactory>
{
    [Fact]
    public async Task Get_objective_without_a_saved_objective_returns_404()
    {
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/profile/objective");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(problem);
        Assert.Contains("No hay ningún objetivo guardado para este atleta.", problem!.Detail);
    }
}
