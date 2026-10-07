#if false
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
        var loaded = Assert.Single(logs!, log => log.Id == saved.Id);
        Assert.Equal([1, 2], loaded.Sets.Select(set => set.SetNumber));
    }

    [Fact]
    public async Task Post_saves_and_returns_the_optional_effort_per_set()
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
                    new { setNumber = 1, value = 10, effort = (int?)2 },
                    new { setNumber = 2, value = 11, effort = (int?)null },
                },
            });
        Assert.Equal(HttpStatusCode.OK, postResponse.StatusCode);

        var saved = await postResponse.Content.ReadFromJsonAsync<SessionLogResponse>();
        Assert.NotNull(saved);
        Assert.Equal(2, saved!.Sets.ElementAt(0).Effort);
        Assert.Null(saved.Sets.ElementAt(1).Effort);

        var logs = await client.GetFromJsonAsync<List<SessionLogResponse>>("/session-logs");
        Assert.NotNull(logs);
        var loaded = Assert.Single(logs!, log => log.Id == saved.Id);
        Assert.Equal(2, loaded.Sets.ElementAt(0).Effort);
        Assert.Null(loaded.Sets.ElementAt(1).Effort);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(11)]
    public async Task Post_with_an_effort_out_of_range_returns_400(int effort)
    {
        using var client = factory.CreateClient();

        using var response = await client.PostAsJsonAsync(
            "/session-logs",
            new
            {
                exerciseId = "push_up",
                sessionDay = 1,
                sets = new[] { new { setNumber = 1, value = 10, effort } },
            });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(problem);
        Assert.Contains("El esfuerzo (RIR/RPE) debe estar entre 0 y 10.", problem!.Detail);
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
    public async Task Post_derives_seconds_for_a_skill_exercise()
    {
        using var client = factory.CreateClient();

        using var response = await client.PostAsJsonAsync(
            "/session-logs",
            new
            {
                exerciseId = "handstand-wall-support",
                sessionDay = 1,
                sets = new[] { new { setNumber = 1, value = 30 }, new { setNumber = 2, value = 28 } },
            });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var saved = await response.Content.ReadFromJsonAsync<SessionLogResponse>();
        Assert.NotNull(saved);
        Assert.Equal("seconds", saved!.Metric);
        Assert.Equal("Pino en pared (pies apoyados)", saved.ExerciseName);
        Assert.Equal([30, 28], saved.Sets.Select(set => set.Value));
    }

    [Fact]
    public async Task Post_derives_reps_for_a_reps_metric_skill_exercise()
    {
        using var client = factory.CreateClient();

        using var response = await client.PostAsJsonAsync(
            "/session-logs",
            new
            {
                exerciseId = "pistol-full",
                sessionDay = 1,
                sets = new[] { new { setNumber = 1, value = 5 }, new { setNumber = 2, value = 4 } },
            });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var saved = await response.Content.ReadFromJsonAsync<SessionLogResponse>();
        Assert.NotNull(saved);
        Assert.Equal("reps", saved!.Metric);
        Assert.Equal("Pistol completa", saved.ExerciseName);
        Assert.Equal([5, 4], saved.Sets.Select(set => set.Value));
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

    [Fact]
    public async Task Put_updates_the_sets_of_an_existing_log_and_returns_the_updated_log()
    {
        using var client = factory.CreateClient();

        var created = await PostPushUpAsync(client, values: [10, 11]);

        using var putResponse = await client.PutAsJsonAsync(
            $"/session-logs/{created.Id}",
            new
            {
                sets = new[]
                {
                    new { setNumber = 1, value = 12, effort = (int?)2 },
                    new { setNumber = 2, value = 13, effort = (int?)null },
                },
            });
        Assert.Equal(HttpStatusCode.OK, putResponse.StatusCode);

        var updated = await putResponse.Content.ReadFromJsonAsync<SessionLogResponse>();
        Assert.NotNull(updated);
        Assert.Equal(created.Id, updated!.Id);
        Assert.Equal([12, 13], updated.Sets.Select(set => set.Value));
        Assert.Equal(2, updated.Sets.ElementAt(0).Effort);
        Assert.Null(updated.Sets.ElementAt(1).Effort);
        Assert.Equal("reps", updated.Metric);

        var logs = await client.GetFromJsonAsync<List<SessionLogResponse>>("/session-logs");
        Assert.NotNull(logs);
        var loaded = Assert.Single(logs!, log => log.Id == created.Id);
        Assert.Equal([12, 13], loaded.Sets.Select(set => set.Value));
    }

    [Fact]
    public async Task Put_derives_the_unit_of_the_exercise_in_the_response()
    {
        using var client = factory.CreateClient();

        using var postResponse = await client.PostAsJsonAsync(
            "/session-logs",
            new
            {
                exerciseId = "hollow-body-hold",
                sessionDay = 1,
                sets = new[] { new { setNumber = 1, value = 25 } },
            });
        var created = await postResponse.Content.ReadFromJsonAsync<SessionLogResponse>();

        using var putResponse = await client.PutAsJsonAsync(
            $"/session-logs/{created!.Id}",
            new { sets = new[] { new { setNumber = 1, value = 30 } } });

        Assert.Equal(HttpStatusCode.OK, putResponse.StatusCode);
        var updated = await putResponse.Content.ReadFromJsonAsync<SessionLogResponse>();
        Assert.NotNull(updated);
        Assert.Equal("seconds", updated!.Metric);
        Assert.Equal("Cuerpo hueco", updated.ExerciseName);
        Assert.Equal([30], updated.Sets.Select(set => set.Value));
    }

    [Fact]
    public async Task Put_with_an_unknown_id_returns_404()
    {
        using var client = factory.CreateClient();

        using var response = await client.PutAsJsonAsync(
            $"/session-logs/{Guid.NewGuid()}",
            new { sets = new[] { new { setNumber = 1, value = 12 } } });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(problem);
        Assert.Contains("El registro de sesión indicado no existe.", problem!.Detail);
    }

    [Fact]
    public async Task Put_without_sets_returns_400()
    {
        using var client = factory.CreateClient();
        var created = await PostPushUpAsync(client, values: [10]);

        using var response = await client.PutAsJsonAsync(
            $"/session-logs/{created.Id}",
            new { sets = Array.Empty<object>() });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Put_with_a_negative_value_returns_400_and_keeps_the_original_sets()
    {
        using var client = factory.CreateClient();
        var created = await PostPushUpAsync(client, values: [10]);

        using var response = await client.PutAsJsonAsync(
            $"/session-logs/{created.Id}",
            new { sets = new[] { new { setNumber = 1, value = -3 } } });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(problem);
        Assert.Contains("El valor real de una serie no puede ser negativo.", problem!.Detail);

        var logs = await client.GetFromJsonAsync<List<SessionLogResponse>>("/session-logs");
        Assert.NotNull(logs);
        var loaded = Assert.Single(logs!, log => log.Id == created.Id);
        Assert.Equal([10], loaded.Sets.Select(set => set.Value));
    }

    [Fact]
    public async Task Post_twice_with_the_same_client_id_updates_the_existing_log_without_duplicating()
    {
        using var client = factory.CreateClient();
        var clientId = Guid.NewGuid();

        // Primer envío: se crea el registro.
        using var first = await client.PostAsJsonAsync(
            "/session-logs",
            new
            {
                exerciseId = "push_up",
                mesocycleId = (Guid?)null,
                sessionDay = 5,
                clientId,
                sets = new[]
                {
                    new { setNumber = 1, value = 10, effort = (int?)null },
                    new { setNumber = 2, value = 11, effort = (int?)null },
                },
            });
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        var firstSaved = await first.Content.ReadFromJsonAsync<SessionLogResponse>();

        // El cliente reenvía el mismo log tras perder la respuesta (mismo clientId): la reescritura
        // gana last-write-wins, actualiza las series en su sitio y no duplica la fila.
        using var second = await client.PostAsJsonAsync(
            "/session-logs",
            new
            {
                exerciseId = "push_up",
                mesocycleId = (Guid?)null,
                sessionDay = 5,
                clientId,
                sets = new[]
                {
                    new { setNumber = 1, value = 14, effort = (int?)null },
                    new { setNumber = 2, value = 14, effort = (int?)null },
                },
            });
        Assert.Equal(HttpStatusCode.OK, second.StatusCode);
        var secondSaved = await second.Content.ReadFromJsonAsync<SessionLogResponse>();

        Assert.Equal(firstSaved!.Id, secondSaved!.Id);
        Assert.Equal([14, 14], secondSaved.Sets.Select(set => set.Value));

        // En la base solo queda un registro para ese ejercicio y día.
        var logs = await client.GetFromJsonAsync<List<SessionLogResponse>>("/session-logs");
        Assert.NotNull(logs);
        var matches = logs!.Where(log => log.ExerciseId == "push_up" && log.SessionDay == 5).ToList();
        var single = Assert.Single(matches);
        Assert.Equal(firstSaved.Id, single.Id);
        Assert.Equal([14, 14], single.Sets.Select(set => set.Value));
    }

    [Fact]
    public async Task Post_without_a_client_id_keeps_creating_separate_logs()
    {
        using var client = factory.CreateClient();
        var body = new
        {
            exerciseId = "push_up",
            mesocycleId = (Guid?)null,
            sessionDay = 6,
            sets = new[] { new { setNumber = 1, value = 10 } },
        };

        await client.PostAsJsonAsync("/session-logs", body);
        await client.PostAsJsonAsync("/session-logs", body);

        var logs = await client.GetFromJsonAsync<List<SessionLogResponse>>("/session-logs");
        Assert.NotNull(logs);
        Assert.Equal(
            2,
            logs!.Count(log => log.ExerciseId == "push_up" && log.SessionDay == 6));
    }

    [Fact]
    public async Task Delete_removes_the_log_and_returns_no_content()
    {
        using var client = factory.CreateClient();
        var created = await PostPushUpAsync(client, values: [10, 11]);

        using var response = await client.DeleteAsync($"/session-logs/{created.Id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var logs = await client.GetFromJsonAsync<List<SessionLogResponse>>("/session-logs");
        Assert.NotNull(logs);
        Assert.DoesNotContain(logs!, log => log.Id == created.Id);
    }

    [Fact]
    public async Task Delete_with_an_unknown_id_returns_404()
    {
        using var client = factory.CreateClient();

        using var response = await client.DeleteAsync($"/session-logs/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(problem);
        Assert.Contains("El registro de sesión indicado no existe.", problem!.Detail);
    }

    private static async Task<SessionLogResponse> PostPushUpAsync(
        HttpClient client,
        IReadOnlyList<int> values)
    {
        using var postResponse = await client.PostAsJsonAsync(
            "/session-logs",
            new
            {
                exerciseId = "push_up",
                sessionDay = 2,
                sets = values.Select((value, index) => new { setNumber = index + 1, value }),
            });
        var saved = await postResponse.Content.ReadFromJsonAsync<SessionLogResponse>();
        Assert.NotNull(saved);
        return saved!;
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
#endif
