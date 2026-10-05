using System.Net;
using System.Net.Http.Json;
using Barrapp.Application.Features.Ping;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Barrapp.Api.FunctionalTests;

public sealed class PingEndpointTests(WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact]
    public async Task Get_ping_returns_200_with_pong()
    {
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/ping");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<PingResponse>();
        Assert.NotNull(payload);
        Assert.Equal("pong", payload!.Message);
    }

    [Fact]
    public async Task Get_health_returns_200_when_the_database_responds()
    {
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Correlation_id_header_is_echoed_back()
    {
        using var client = factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/ping");
        request.Headers.Add("X-Correlation-Id", "test-correlation-id");

        using var response = await client.SendAsync(request);

        Assert.Equal("test-correlation-id", response.Headers.GetValues("X-Correlation-Id").Single());
    }
}
