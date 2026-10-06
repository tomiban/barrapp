using System.Net;
using System.Net.Http.Json;
using Barrapp.Application.Features.SessionLogs;
using Microsoft.AspNetCore.Mvc;

namespace Barrapp.Api.FunctionalTests;

public sealed class SessionLogAggregateEndpointTests
{
    private static readonly Guid MesocycleId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

    [Fact]
    public async Task Post_and_get_use_the_aggregate_contract()
    {
        using var factory = new BarrappApiFactory();
        using var client = factory.CreateClient();
        using var response = await client.PostAsJsonAsync("/session-logs", Request(
            "push_up",
            [new { setNumber = 1, value = 10, actualRir = (int?)null, loadKg = (double?)null }],
            Guid.NewGuid()));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var saved = await response.Content.ReadFromJsonAsync<SessionLogResponse>();
        Assert.NotNull(saved);
        Assert.Equal("mesocycle", saved!.Kind);
        Assert.Equal(MesocycleId, saved.MesocycleId);
        var item = Assert.Single(saved.Items);
        Assert.Equal("push_up", item.ExerciseId);
        Assert.Equal("Flexiones", item.ExerciseName);
        Assert.Equal("reps", item.Metric);
        Assert.Equal(10, Assert.Single(item.Sets).Value);

        var sessions = await client.GetFromJsonAsync<List<SessionLogResponse>>("/session-logs");
        Assert.NotNull(sessions);
        Assert.Equal(saved.Id, Assert.Single(sessions!).Id);

        using var secondResponse = await client.PostAsJsonAsync("/session-logs", Request(
            "pull_up",
            [new { setNumber = 1, value = 5, actualRir = (int?)null, loadKg = (double?)null }],
            Guid.NewGuid()));
        Assert.Equal(HttpStatusCode.OK, secondResponse.StatusCode);
        var updatedSession = await secondResponse.Content.ReadFromJsonAsync<SessionLogResponse>();
        Assert.NotNull(updatedSession);
        Assert.Equal(saved.Id, updatedSession!.Id);
        Assert.Equal(["push_up", "pull_up"], updatedSession.Items.Select(sessionItem => sessionItem.ExerciseId));
    }

    [Fact]
    public async Task Item_update_and_delete_use_session_scoped_routes()
    {
        using var factory = new BarrappApiFactory();
        using var client = factory.CreateClient();
        using var post = await client.PostAsJsonAsync("/session-logs", Request(
            "push_up",
            [new { setNumber = 1, value = 10, actualRir = (int?)null, loadKg = (double?)null }],
            Guid.NewGuid()));
        var created = await post.Content.ReadFromJsonAsync<SessionLogResponse>();
        Assert.NotNull(created);
        var item = Assert.Single(created!.Items);

        using var update = await client.PutAsJsonAsync(
            $"/session-logs/{created.Id}/items/{item.Id}",
            new { sets = new[] { new { setNumber = 1, value = 12, actualRir = (int?)2, loadKg = (double?)5 } } });
        Assert.Equal(HttpStatusCode.OK, update.StatusCode);
        var updated = await update.Content.ReadFromJsonAsync<SessionLogItemResponse>();
        Assert.NotNull(updated);
        Assert.Equal(12, Assert.Single(updated!.Sets).Value);
        Assert.Equal(2, updated.Sets[0].ActualRir);
        Assert.Equal(5, updated.Sets[0].LoadKg);

        using var delete = await client.DeleteAsync($"/session-logs/{created.Id}/items/{item.Id}");
        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);
        Assert.Empty(await client.GetFromJsonAsync<List<SessionLogResponse>>("/session-logs") ?? []);
    }

    [Fact]
    public async Task Post_with_invalid_rir_returns_bad_request()
    {
        using var factory = new BarrappApiFactory();
        using var client = factory.CreateClient();
        using var response = await client.PostAsJsonAsync("/session-logs", Request(
            "push_up",
            [new { setNumber = 1, value = 10, actualRir = (int?)11, loadKg = (double?)null }],
            null));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.NotNull(await response.Content.ReadFromJsonAsync<ProblemDetails>());
    }

    private static object Request(string exerciseId, object[] sets, Guid? clientId) => new
    {
        session = new
        {
            kind = "mesocycle",
            date = "2026-10-05",
            mesocycleId = MesocycleId,
            microcycleNumber = 1,
            sessionDay = 2,
        },
        item = new
        {
            exerciseId,
            role = "strength",
            pattern = "push",
            prescribedSets = sets.Length,
            repsMin = (int?)8,
            repsMax = (int?)12,
            holdSecondsMin = (int?)null,
            holdSecondsMax = (int?)null,
            note = (string?)null,
            sets,
        },
        clientId,
    };
}
