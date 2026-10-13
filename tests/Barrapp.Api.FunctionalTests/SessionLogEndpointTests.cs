using System.Net;
using System.Net.Http.Json;
using Barrapp.Application.Features.SessionLogs;
using Microsoft.AspNetCore.Mvc;

namespace Barrapp.Api.FunctionalTests;

/// <summary>
/// El registro de sesión se guarda serie a serie con un <c>POST</c> sobre el contrato agregado —
/// clave de sesión determinista más foto por ítem (ADR-0014)— y se lee con un <c>GET</c>
/// (spec 0001, US-34; decisión D5). La unidad —reps o segundos— la deriva el servidor del tipo de
/// ejercicio del catálogo, nunca el cliente.
/// </summary>
public sealed class SessionLogEndpointTests
{
    private static readonly Guid MesocycleId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

    [Fact]
    public async Task Post_registers_the_sets_and_get_returns_them_with_the_derived_unit()
    {
        using var factory = new BarrappApiFactory();
        using var client = factory.CreateClient();

        using var postResponse = await client.PostAsJsonAsync(
            "/session-logs",
            Request(
                "push_up",
                [new { setNumber = 1, value = 10 }, new { setNumber = 2, value = 11 }]));

        Assert.Equal(HttpStatusCode.OK, postResponse.StatusCode);

        var saved = await postResponse.Content.ReadFromJsonAsync<SessionLogResponse>();
        Assert.NotNull(saved);
        Assert.Equal("mesocycle", saved!.Kind);
        Assert.Equal(new DateOnly(2026, 10, 5), saved.SessionDate);
        Assert.Equal(MesocycleId, saved.MesocycleId);
        Assert.Equal(2, saved.MicrocycleNumber);
        Assert.Equal(1, saved.SessionDay);
        Assert.False(saved.Completed);

        var item = Assert.Single(saved.Items);
        Assert.Equal("push_up", item.ExerciseId);
        Assert.Equal("Flexiones", item.ExerciseName);
        Assert.Equal("strength", item.Role);
        Assert.Equal("push", item.Pattern);
        Assert.Equal("reps", item.Metric);
        Assert.Equal(2, item.Objective.Sets);
        Assert.Equal([8, 12], [item.Objective.RepsMin, item.Objective.RepsMax]);
        Assert.Equal([10, 11], item.Sets.Select(set => set.Value));
        Assert.Equal([1, 2], item.Sets.Select(set => set.SetNumber));

        using var getResponse = await client.GetAsync($"/session-logs/{saved.Id}");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        var loaded = await getResponse.Content.ReadFromJsonAsync<SessionLogResponse>();
        Assert.NotNull(loaded);
        Assert.Equal(saved.Id, loaded!.Id);
        Assert.Equal([10, 11], Assert.Single(loaded.Items).Sets.Select(set => set.Value));

        var logs = await client.GetFromJsonAsync<List<SessionLogResponse>>("/session-logs");
        Assert.NotNull(logs);
        Assert.Single(logs!, log => log.Id == saved.Id);
    }

    [Fact]
    public async Task Post_saves_and_returns_the_optional_rir_and_load_per_set()
    {
        using var factory = new BarrappApiFactory();
        using var client = factory.CreateClient();

        using var postResponse = await client.PostAsJsonAsync(
            "/session-logs",
            Request(
                "push_up",
                [
                    new { setNumber = 1, value = 10, actualRir = (int?)2, loadKg = (double?)5 },
                    new { setNumber = 2, value = 11, actualRir = (int?)null, loadKg = (double?)null },
                ]));

        Assert.Equal(HttpStatusCode.OK, postResponse.StatusCode);

        var saved = await postResponse.Content.ReadFromJsonAsync<SessionLogResponse>();
        Assert.NotNull(saved);
        var sets = Assert.Single(saved!.Items).Sets;
        Assert.Equal(2, sets.ElementAt(0).ActualRir);
        Assert.Equal(5, sets.ElementAt(0).LoadKg);
        Assert.Null(sets.ElementAt(1).ActualRir);
        Assert.Null(sets.ElementAt(1).LoadKg);

        var logs = await client.GetFromJsonAsync<List<SessionLogResponse>>("/session-logs");
        Assert.NotNull(logs);
        var loaded = Assert.Single(logs!, log => log.Id == saved.Id);
        var loadedSets = Assert.Single(loaded.Items).Sets;
        Assert.Equal(2, loadedSets.ElementAt(0).ActualRir);
        Assert.Null(loadedSets.ElementAt(1).ActualRir);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(11)]
    public async Task Post_with_an_rir_out_of_range_returns_400(int actualRir)
    {
        using var factory = new BarrappApiFactory();
        using var client = factory.CreateClient();

        using var response = await client.PostAsJsonAsync(
            "/session-logs",
            Request("push_up", [new { setNumber = 1, value = 10, actualRir = (int?)actualRir }]));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(problem);
        Assert.Contains("El RIR real debe estar entre 0 y 10.", problem!.Detail);
    }

    [Fact]
    public async Task Post_derives_seconds_for_a_hold_exercise()
    {
        using var factory = new BarrappApiFactory();
        using var client = factory.CreateClient();

        using var response = await client.PostAsJsonAsync("/session-logs", HoldRequest());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var saved = await response.Content.ReadFromJsonAsync<SessionLogResponse>();
        Assert.NotNull(saved);
        var item = Assert.Single(saved!.Items);
        Assert.Equal("seconds", item.Metric);
        Assert.Equal("Cuerpo hueco", item.ExerciseName);
        Assert.Null(item.Objective.RepsMin);
        Assert.Equal([20, 30], [item.Objective.HoldSecondsMin, item.Objective.HoldSecondsMax]);
    }

    [Fact]
    public async Task Post_derives_seconds_for_a_skill_exercise()
    {
        using var factory = new BarrappApiFactory();
        using var client = factory.CreateClient();

        using var response = await client.PostAsJsonAsync(
            "/session-logs",
            Request(
                "handstand-wall-support",
                [new { setNumber = 1, value = 30 }, new { setNumber = 2, value = 28 }],
                role: "skill",
                pattern: null,
                repsMin: null,
                repsMax: null,
                holdSecondsMin: 25,
                holdSecondsMax: 35));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var saved = await response.Content.ReadFromJsonAsync<SessionLogResponse>();
        Assert.NotNull(saved);
        var item = Assert.Single(saved!.Items);
        Assert.Equal("seconds", item.Metric);
        Assert.Equal("Pino en pared (pies apoyados)", item.ExerciseName);
        Assert.Equal([30, 28], item.Sets.Select(set => set.Value));
    }

    [Fact]
    public async Task Post_derives_reps_for_a_reps_metric_skill_exercise()
    {
        using var factory = new BarrappApiFactory();
        using var client = factory.CreateClient();

        using var response = await client.PostAsJsonAsync(
            "/session-logs",
            Request(
                "pistol-full",
                [new { setNumber = 1, value = 5 }, new { setNumber = 2, value = 4 }],
                role: "skill",
                pattern: null));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var saved = await response.Content.ReadFromJsonAsync<SessionLogResponse>();
        Assert.NotNull(saved);
        var item = Assert.Single(saved!.Items);
        Assert.Equal("reps", item.Metric);
        Assert.Equal("Pistol completa", item.ExerciseName);
        Assert.Equal([5, 4], item.Sets.Select(set => set.Value));
    }

    [Fact]
    public async Task Post_with_an_unknown_exercise_returns_400()
    {
        using var factory = new BarrappApiFactory();
        using var client = factory.CreateClient();

        using var response = await client.PostAsJsonAsync(
            "/session-logs",
            Request("ghost", [new { setNumber = 1, value = 10 }]));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(problem);
        Assert.Contains("El ejercicio indicado no existe en el catálogo.", problem!.Detail);
    }

    [Fact]
    public async Task Post_without_sets_returns_400()
    {
        using var factory = new BarrappApiFactory();
        using var client = factory.CreateClient();

        using var response = await client.PostAsJsonAsync(
            "/session-logs",
            Request("push_up", Array.Empty<object>()));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Post_with_a_negative_value_returns_400()
    {
        using var factory = new BarrappApiFactory();
        using var client = factory.CreateClient();

        using var response = await client.PostAsJsonAsync(
            "/session-logs",
            Request("push_up", [new { setNumber = 1, value = -1 }]));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(problem);
        Assert.Contains("El valor real de una serie no puede ser negativo.", problem!.Detail);
    }

    [Fact]
    public async Task Post_with_a_legacy_body_without_session_returns_a_problem_details_400()
    {
        using var factory = new BarrappApiFactory();
        using var client = factory.CreateClient();

        using var response = await client.PostAsJsonAsync(
            "/session-logs",
            new { sets = Array.Empty<object>() });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(problem);
        Assert.Contains("Debes indicar el ejercicio registrado.", problem!.Detail);
    }

    [Fact]
    public async Task Put_updates_the_sets_of_the_first_item_and_returns_the_updated_item()
    {
        using var factory = new BarrappApiFactory();
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

        var updated = await putResponse.Content.ReadFromJsonAsync<SessionLogItemResponse>();
        Assert.NotNull(updated);
        Assert.Equal([12, 13], updated!.Sets.Select(set => set.Value));
        Assert.Equal(2, updated.Sets.ElementAt(0).ActualRir);
        Assert.Null(updated.Sets.ElementAt(1).ActualRir);
        Assert.Equal("Flexiones", updated.ExerciseName);
        Assert.Equal("reps", updated.Metric);

        var logs = await client.GetFromJsonAsync<List<SessionLogResponse>>("/session-logs");
        Assert.NotNull(logs);
        var loaded = Assert.Single(logs!, log => log.Id == created.Id);
        Assert.Equal([12, 13], Assert.Single(loaded.Items).Sets.Select(set => set.Value));
    }

    [Fact]
    public async Task Put_derives_the_unit_of_the_exercise_in_the_response()
    {
        using var factory = new BarrappApiFactory();
        using var client = factory.CreateClient();
        var created = await PostHoldAsync(client);

        using var putResponse = await client.PutAsJsonAsync(
            $"/session-logs/{created.Id}",
            new { sets = new[] { new { setNumber = 1, value = 30 } } });

        Assert.Equal(HttpStatusCode.OK, putResponse.StatusCode);
        var updated = await putResponse.Content.ReadFromJsonAsync<SessionLogItemResponse>();
        Assert.NotNull(updated);
        Assert.Equal("seconds", updated!.Metric);
        Assert.Equal("Cuerpo hueco", updated.ExerciseName);
        Assert.Equal([30], updated.Sets.Select(set => set.Value));
    }

    [Fact]
    public async Task Put_with_an_unknown_id_returns_404()
    {
        using var factory = new BarrappApiFactory();
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
        using var factory = new BarrappApiFactory();
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
        using var factory = new BarrappApiFactory();
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
        Assert.Equal([10], Assert.Single(loaded.Items).Sets.Select(set => set.Value));
    }

    [Fact]
    public async Task Post_twice_with_the_same_client_id_updates_the_existing_log_without_duplicating()
    {
        using var factory = new BarrappApiFactory();
        using var client = factory.CreateClient();
        var clientId = Guid.NewGuid();

        // Primer envío: se crea el registro.
        using var first = await client.PostAsJsonAsync(
            "/session-logs",
            Request(
                "push_up",
                [new { setNumber = 1, value = 10 }, new { setNumber = 2, value = 11 }],
                clientId: clientId));
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        var firstSaved = await first.Content.ReadFromJsonAsync<SessionLogResponse>();
        Assert.NotNull(firstSaved);

        // El cliente reenvía el mismo ítem tras perder la respuesta (mismo clientId): la
        // reescritura gana last-write-wins, actualiza las series en su sitio y no duplica nada.
        using var second = await client.PostAsJsonAsync(
            "/session-logs",
            Request(
                "push_up",
                [new { setNumber = 1, value = 14 }, new { setNumber = 2, value = 14 }],
                clientId: clientId));
        Assert.Equal(HttpStatusCode.OK, second.StatusCode);
        var secondSaved = await second.Content.ReadFromJsonAsync<SessionLogResponse>();

        Assert.Equal(firstSaved!.Id, secondSaved!.Id);
        var item = Assert.Single(secondSaved.Items);
        Assert.Equal(Assert.Single(firstSaved.Items).Id, item.Id);
        Assert.Equal([14, 14], item.Sets.Select(set => set.Value));

        // En la base solo queda una sesión con ese ítem y esas series.
        var logs = await client.GetFromJsonAsync<List<SessionLogResponse>>("/session-logs");
        Assert.NotNull(logs);
        var loaded = Assert.Single(logs!, log => log.Id == firstSaved.Id);
        Assert.Equal([14, 14], Assert.Single(loaded.Items).Sets.Select(set => set.Value));
    }

    [Fact]
    public async Task Post_twice_without_a_client_id_updates_the_same_item_in_place()
    {
        using var factory = new BarrappApiFactory();
        using var client = factory.CreateClient();

        using var first = await client.PostAsJsonAsync(
            "/session-logs",
            Request("push_up", [new { setNumber = 1, value = 10 }]));
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        var firstSaved = await first.Content.ReadFromJsonAsync<SessionLogResponse>();

        using var second = await client.PostAsJsonAsync(
            "/session-logs",
            Request("push_up", [new { setNumber = 1, value = 12 }]));
        Assert.Equal(HttpStatusCode.OK, second.StatusCode);
        var secondSaved = await second.Content.ReadFromJsonAsync<SessionLogResponse>();

        // La misma clave de sesión y el mismo ejercicio son la misma fila: última escritura gana.
        Assert.Equal(firstSaved!.Id, secondSaved!.Id);
        var item = Assert.Single(secondSaved.Items);
        Assert.Equal(Assert.Single(firstSaved.Items).Id, item.Id);
        Assert.Equal([12], item.Sets.Select(set => set.Value));
    }

    [Fact]
    public async Task Post_on_a_different_date_creates_a_separate_session()
    {
        using var factory = new BarrappApiFactory();
        using var client = factory.CreateClient();

        using var first = await client.PostAsJsonAsync(
            "/session-logs",
            Request("push_up", [new { setNumber = 1, value = 10 }]));
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        var firstSaved = await first.Content.ReadFromJsonAsync<SessionLogResponse>();

        // La fecha entra en la clave de sesión determinista (ADR-0014): el mismo día de sesión en
        // otra fecha es otro registro, y el índice único filtrado lo admite.
        using var second = await client.PostAsJsonAsync(
            "/session-logs",
            Request("push_up", [new { setNumber = 1, value = 14 }], date: "2026-10-12"));
        Assert.Equal(HttpStatusCode.OK, second.StatusCode);
        var secondSaved = await second.Content.ReadFromJsonAsync<SessionLogResponse>();

        Assert.NotEqual(firstSaved!.Id, secondSaved!.Id);
        Assert.Equal([10], Assert.Single(firstSaved.Items).Sets.Select(set => set.Value));
        Assert.Equal([14], Assert.Single(secondSaved.Items).Sets.Select(set => set.Value));
    }

    [Fact]
    public async Task Post_with_the_same_client_id_on_a_different_date_updates_the_original_session()
    {
        using var factory = new BarrappApiFactory();
        using var client = factory.CreateClient();
        var clientId = Guid.NewGuid();

        using var first = await client.PostAsJsonAsync(
            "/session-logs",
            Request(
                "push_up",
                [new { setNumber = 1, value = 10 }],
                clientId: clientId));
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        var firstSaved = await first.Content.ReadFromJsonAsync<SessionLogResponse>();

        // El cliente reenvía tras perder la respuesta con la fecha ya cambiada (medianoche o plan
        // regenerado): la idempotencia por clientId (ADR-0003) manda sobre la clave con fecha y el
        // ítem se actualiza en su sesión original, sin crear un registro paralelo.
        using var second = await client.PostAsJsonAsync(
            "/session-logs",
            Request(
                "push_up",
                [new { setNumber = 1, value = 14 }],
                date: "2026-10-12",
                clientId: clientId));
        Assert.Equal(HttpStatusCode.OK, second.StatusCode);
        var secondSaved = await second.Content.ReadFromJsonAsync<SessionLogResponse>();

        Assert.Equal(firstSaved!.Id, secondSaved!.Id);
        Assert.Equal(new DateOnly(2026, 10, 5), secondSaved.SessionDate);
        Assert.Equal([14], Assert.Single(secondSaved.Items).Sets.Select(set => set.Value));

        var logs = await client.GetFromJsonAsync<List<SessionLogResponse>>("/session-logs");
        Assert.NotNull(logs);
        Assert.Single(logs!);
    }

    [Fact]
    public async Task Post_suelta_sessions_of_the_same_day_share_one_session()
    {
        using var factory = new BarrappApiFactory();
        using var client = factory.CreateClient();

        using var first = await client.PostAsJsonAsync(
            "/session-logs",
            SueltaRequest("push_up", [new { setNumber = 1, value = 10 }]));
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        var firstSaved = await first.Content.ReadFromJsonAsync<SessionLogResponse>();
        Assert.NotNull(firstSaved);
        Assert.Equal("suelta", firstSaved!.Kind);
        Assert.Null(firstSaved.MesocycleId);
        Assert.Null(firstSaved.MicrocycleNumber);
        Assert.Null(firstSaved.SessionDay);

        // La fecha es la identidad de una sesión suelta (ADR-0014): registrar otro ejercicio el
        // mismo día entra en la misma sesión.
        using var second = await client.PostAsJsonAsync(
            "/session-logs",
            SueltaRequest("pull_up", [new { setNumber = 1, value = 5 }], pattern: "pull", date: "2026-10-05"));
        Assert.Equal(HttpStatusCode.OK, second.StatusCode);
        var secondSaved = await second.Content.ReadFromJsonAsync<SessionLogResponse>();

        Assert.Equal(firstSaved.Id, secondSaved!.Id);
        Assert.Equal(["push_up", "pull_up"], secondSaved.Items.Select(item => item.ExerciseId));

        var logs = await client.GetFromJsonAsync<List<SessionLogResponse>>("/session-logs");
        Assert.NotNull(logs);
        Assert.Single(logs!);
    }

    [Fact]
    public async Task Delete_removes_the_log_and_returns_no_content()
    {
        using var factory = new BarrappApiFactory();
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
        using var factory = new BarrappApiFactory();
        using var client = factory.CreateClient();

        using var response = await client.DeleteAsync($"/session-logs/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(problem);
        Assert.Contains("El registro de sesión indicado no existe.", problem!.Detail);
    }

    [Fact]
    public async Task Complete_marks_the_session_and_uncomplete_clears_it_keeping_the_items()
    {
        using var factory = new BarrappApiFactory();
        using var client = factory.CreateClient();
        var created = await PostPushUpAsync(client, values: [10]);

        using var complete = await client.PostAsync($"/session-logs/{created.Id}/complete", null);
        Assert.Equal(HttpStatusCode.OK, complete.StatusCode);
        var completed = await complete.Content.ReadFromJsonAsync<SessionLogResponse>();
        Assert.NotNull(completed);
        Assert.True(completed!.Completed);
        Assert.NotNull(completed.CompletedAtUtc);

        using var uncomplete = await client.DeleteAsync($"/session-logs/{created.Id}/complete");
        Assert.Equal(HttpStatusCode.OK, uncomplete.StatusCode);
        var uncompleted = await uncomplete.Content.ReadFromJsonAsync<SessionLogResponse>();
        Assert.NotNull(uncompleted);
        Assert.False(uncompleted!.Completed);
        Assert.Null(uncompleted.CompletedAtUtc);

        // Des-completar no pierde lo registrado: la sesión sigue con su ítem y sus series.
        Assert.Equal([10], Assert.Single(uncompleted.Items).Sets.Select(set => set.Value));
    }

    private static object Request(
        string exerciseId,
        object[] sets,
        string role = "strength",
        string? pattern = "push",
        int? repsMin = 8,
        int? repsMax = 12,
        int? holdSecondsMin = null,
        int? holdSecondsMax = null,
        string date = "2026-10-05",
        Guid? clientId = null) => new
        {
            session = new
            {
                kind = "mesocycle",
                date,
                mesocycleId = MesocycleId,
                microcycleNumber = 2,
                sessionDay = 1,
            },
            item = new
            {
                exerciseId,
                role,
                pattern,
                prescribedSets = sets.Length,
                repsMin,
                repsMax,
                holdSecondsMin,
                holdSecondsMax,
                note = (string?)null,
                sets,
            },
            clientId,
        };

    private static object HoldRequest() => Request(
        "hollow-body-hold",
        [new { setNumber = 1, value = 25 }],
        role: "core",
        pattern: "core",
        repsMin: null,
        repsMax: null,
        holdSecondsMin: 20,
        holdSecondsMax: 30);

    private static object SueltaRequest(
        string exerciseId,
        object[] sets,
        string pattern = "push",
        string date = "2026-10-05") => new
        {
            session = new
            {
                kind = "suelta",
                date,
            },
            item = new
            {
                exerciseId,
                role = "strength",
                pattern,
                prescribedSets = sets.Length,
                repsMin = 8,
                repsMax = 12,
                holdSecondsMin = (int?)null,
                holdSecondsMax = (int?)null,
                note = (string?)null,
                sets,
            },
            clientId = (Guid?)null,
        };

    private static async Task<SessionLogResponse> PostPushUpAsync(
        HttpClient client,
        IReadOnlyList<int> values)
    {
        using var postResponse = await client.PostAsJsonAsync(
            "/session-logs",
            Request(
                "push_up",
                values.Select((value, index) => new { setNumber = index + 1, value }).ToArray<object>()));
        var saved = await postResponse.Content.ReadFromJsonAsync<SessionLogResponse>();
        Assert.NotNull(saved);
        return saved!;
    }

    private static async Task<SessionLogResponse> PostHoldAsync(HttpClient client)
    {
        using var postResponse = await client.PostAsJsonAsync("/session-logs", HoldRequest());
        var saved = await postResponse.Content.ReadFromJsonAsync<SessionLogResponse>();
        Assert.NotNull(saved);
        return saved!;
    }
}

/// <summary>
/// Clase aparte para arrancar una base vacía: sin registros, el listado devuelve una lista vacía.
/// </summary>
public sealed class SessionLogEmptyEndpointTests
{
    [Fact]
    public async Task Get_logs_without_saved_logs_returns_an_empty_list()
    {
        using var factory = new BarrappApiFactory();
        using var client = factory.CreateClient();

        var logs = await client.GetFromJsonAsync<List<SessionLogResponse>>("/session-logs");

        Assert.NotNull(logs);
        Assert.Empty(logs!);
    }
}
