using System.Net;
using System.Net.Http.Json;
using Barrapp.Application.Features.SessionLogs;
using Microsoft.AspNetCore.Mvc;

namespace Barrapp.Api.FunctionalTests;

/// <summary>
/// El registro de sesión se guarda serie a serie con un <c>POST</c> y se lee con un <c>GET</c>
/// (spec 0001, US-34; decisión D5). La unidad —reps o segundos— la deriva el servidor del tipo
/// de ejercicio del catálogo, nunca el cliente.
/// </summary>
public sealed class SessionLogEndpointTests(BarrappApiFactory factory)
    : IClassFixture<BarrappApiFactory>
{
    [Fact]
    public async Task Post_registers_the_sets_and_get_returns_them_with_the_derived_unit()
    {
        using var client = factory.CreateClient();

        using var postResponse = await client.PostAsJsonAsync(
            "/session-logs",
            new
            {
                exerciseId = "push_up",
                mesocycleId = (Guid?)null,
                sessionDay = 2,
                sets = new[]
                {
                    new { setNumber = 1, value = 10 },
                    new { setNumber = 2, value = 11 },
                },
            });
        Assert.Equal(HttpStatusCode.OK, postResponse.StatusCode);

        var saved = await postResponse.Content.ReadFromJsonAsync<SessionLogResponse>();
        Assert.NotNull(saved);
        Assert.Equal("push_up", saved!.ExerciseId);
        Assert.Equal("Flexiones", saved.ExerciseName);
        Assert.Equal("reps", saved.Metric);
        Assert.Equal(2, saved.SessionDay);
        Assert.Null(saved.MesocycleId);
        Assert.Equal([10, 11], saved.Sets.Select(set => set.Value));

        var logs = await client.GetFromJsonAsync<List<SessionLogResponse>>("/session-logs");
        Assert.NotNull(logs);
        var loaded = Assert.Single(logs!);
        Assert.Equal(saved.Id, loaded.Id);
        Assert.Equal([1, 2], loaded.Sets.Select(set => set.SetNumber));
    }

    [Fact]
    public async Task Post_derives_seconds_for_a_hold_exercise()
    {
        using var client = factory.CreateClient();

        using var response = await client.PostAsJsonAsync(
            "/session-logs",
            new
            {
                exerciseId = "hollow-body-hold",
                sessionDay = 1,
                sets = new[] { new { setNumber = 1, value = 25 } },
            });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var saved = await response.Content.ReadFromJsonAsync<SessionLogResponse>();
        Assert.NotNull(saved);
        Assert.Equal("seconds", saved!.Metric);
        Assert.Equal("Cuerpo hueco", saved.ExerciseName);
    }

    [Fact]
    public async Task Post_with_an_unknown_exercise_returns_400()
    {
        using var client = factory.CreateClient();

        using var response = await client.PostAsJsonAsync(
            "/session-logs",
            new
            {
                exerciseId = "ghost",
                sessionDay = 1,
                sets = new[] { new { setNumber = 1, value = 10 } },
            });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(problem);
        Assert.Contains("El ejercicio indicado no existe en el catálogo.", problem!.Detail);
    }

    [Fact]
    public async Task Post_without_sets_returns_400()
    {
        using var client = factory.CreateClient();

        using var response = await client.PostAsJsonAsync(
            "/session-logs",
            new { exerciseId = "push_up", sessionDay = 1, sets = Array.Empty<object>() });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Post_with_a_negative_actual_value_returns_400()
    {
        using var client = factory.CreateClient();

        using var response = await client.PostAsJsonAsync(
            "/session-logs",
            new
            {
                exerciseId = "push_up",
                sessionDay = 1,
                sets = new[] { new { setNumber = 1, value = -1 } },
            });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(problem);
        Assert.Contains("El valor real de una serie no puede ser negativo.", problem!.Detail);
    }
}

/// <summary>
/// Clase aparte para arrancar una base vacía: sin registros, el listado devuelve una lista vacía.
/// </summary>
public sealed class SessionLogEmptyEndpointTests(BarrappApiFactory factory)
    : IClassFixture<BarrappApiFactory>
{
    [Fact]
    public async Task Get_logs_without_saved_logs_returns_an_empty_list()
    {
        using var client = factory.CreateClient();

        var logs = await client.GetFromJsonAsync<List<SessionLogResponse>>("/session-logs");

        Assert.NotNull(logs);
        Assert.Empty(logs!);
    }
}
