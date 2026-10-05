using System.Net;
using System.Net.Http.Json;
using Barrapp.Application.Features.AthleteProfiles;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Barrapp.Api.FunctionalTests;

public sealed class AthleteProfileEndpointTests(WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact]
    public async Task Put_profile_then_get_profile_returns_the_saved_values()
    {
        using var client = factory.CreateClient();
        var payload = new AthleteProfileResponse(77.5, 180);

        using var putResponse = await client.PutAsJsonAsync("/profile", payload);
        Assert.Equal(HttpStatusCode.OK, putResponse.StatusCode);

        var saved = await putResponse.Content.ReadFromJsonAsync<AthleteProfileResponse>();
        Assert.NotNull(saved);
        Assert.Equal(77.5, saved!.WeightKilograms);
        Assert.Equal(180, saved.HeightCentimeters);

        using var getResponse = await client.GetAsync("/profile");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        var loaded = await getResponse.Content.ReadFromJsonAsync<AthleteProfileResponse>();
        Assert.NotNull(loaded);
        Assert.Equal(77.5, loaded!.WeightKilograms);
        Assert.Equal(180, loaded.HeightCentimeters);
    }

    [Fact]
    public async Task Put_profile_with_a_non_positive_weight_returns_400()
    {
        using var client = factory.CreateClient();

        using var response = await client.PutAsJsonAsync("/profile", new AthleteProfileResponse(0, 180));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
