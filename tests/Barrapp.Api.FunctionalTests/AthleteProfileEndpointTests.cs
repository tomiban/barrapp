using System.Net;
using System.Net.Http.Json;
using Barrapp.Application.Features.AthleteProfiles;
using Microsoft.AspNetCore.Mvc;

namespace Barrapp.Api.FunctionalTests;

public sealed class AthleteProfileEndpointTests(BarrappApiFactory factory)
    : IClassFixture<BarrappApiFactory>
{
    [Fact]
    public async Task Put_profile_then_get_profile_returns_the_saved_values()
    {
        using var client = factory.CreateClient();
        var payload = new AthleteProfileResponse(77.5, 180, 4);

        using var putResponse = await client.PutAsJsonAsync("/profile", payload);
        Assert.Equal(HttpStatusCode.OK, putResponse.StatusCode);

        var saved = await putResponse.Content.ReadFromJsonAsync<AthleteProfileResponse>();
        Assert.NotNull(saved);
        Assert.Equal(77.5, saved!.WeightKilograms);
        Assert.Equal(180, saved.HeightCentimeters);
        Assert.Equal(4, saved.TrainingDays);

        using var getResponse = await client.GetAsync("/profile");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        var loaded = await getResponse.Content.ReadFromJsonAsync<AthleteProfileResponse>();
        Assert.NotNull(loaded);
        Assert.Equal(77.5, loaded!.WeightKilograms);
        Assert.Equal(180, loaded.HeightCentimeters);
        Assert.Equal(4, loaded.TrainingDays);
    }

    [Fact]
    public async Task Put_profile_with_a_non_positive_weight_returns_400()
    {
        using var client = factory.CreateClient();

        using var response = await client.PutAsJsonAsync("/profile", new AthleteProfileResponse(0, 180, 4));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [InlineData(30, 120)]
    [InlineData(200, 220)]
    public async Task Put_profile_accepts_the_range_boundaries(double weightKilograms, double heightCentimeters)
    {
        using var client = factory.CreateClient();

        using var response = await client.PutAsJsonAsync(
            "/profile",
            new AthleteProfileResponse(weightKilograms, heightCentimeters, 4));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var saved = await response.Content.ReadFromJsonAsync<AthleteProfileResponse>();
        Assert.NotNull(saved);
        Assert.Equal(weightKilograms, saved!.WeightKilograms);
        Assert.Equal(heightCentimeters, saved.HeightCentimeters);
    }

    [Theory]
    [InlineData(29.9, 180, "El peso debe estar entre 30 y 200 kg.")]
    [InlineData(200.1, 180, "El peso debe estar entre 30 y 200 kg.")]
    [InlineData(80, 119.9, "La altura debe estar entre 120 y 220 cm.")]
    [InlineData(80, 220.1, "La altura debe estar entre 120 y 220 cm.")]
    public async Task Put_profile_rejects_values_out_of_range(
        double weightKilograms,
        double heightCentimeters,
        string expectedDetail)
    {
        using var client = factory.CreateClient();

        using var response = await client.PutAsJsonAsync(
            "/profile",
            new AthleteProfileResponse(weightKilograms, heightCentimeters, 4));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(problem);
        Assert.Contains(expectedDetail, problem!.Detail);
    }

    [Theory]
    [InlineData(3)]
    [InlineData(5)]
    public async Task Put_profile_accepts_the_training_days_boundaries(int trainingDays)
    {
        using var client = factory.CreateClient();

        using var response = await client.PutAsJsonAsync(
            "/profile",
            new AthleteProfileResponse(77.5, 180, trainingDays));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var saved = await response.Content.ReadFromJsonAsync<AthleteProfileResponse>();
        Assert.NotNull(saved);
        Assert.Equal(trainingDays, saved!.TrainingDays);
    }

    [Theory]
    [InlineData(2)]
    [InlineData(6)]
    public async Task Put_profile_rejects_training_days_out_of_range(int trainingDays)
    {
        using var client = factory.CreateClient();

        using var response = await client.PutAsJsonAsync(
            "/profile",
            new AthleteProfileResponse(77.5, 180, trainingDays));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(problem);
        Assert.Contains("Los días de entrenamiento deben estar entre 3 y 5.", problem!.Detail);
    }
}
