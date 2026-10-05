using System.Net;
using System.Net.Http.Json;
using Barrapp.Application.Features.AthleteProfiles;
using Microsoft.AspNetCore.Mvc;

namespace Barrapp.Api.FunctionalTests;

public sealed class AthleteProfileEndpointTests(BarrappApiFactory factory)
    : IClassFixture<BarrappApiFactory>
{
    private static MaximumResponse[] Maximums(int pushUp = 10, int pullUp = 0, int squat = 20) =>
    [
        new MaximumResponse("push_up", pushUp),
        new MaximumResponse("pull_up", pullUp),
        new MaximumResponse("squat", squat),
    ];

    private static AthleteProfileResponse Profile(
        double weightKilograms,
        double heightCentimeters,
        int trainingDays,
        IReadOnlyList<MaximumResponse>? maximums = null) =>
        new(weightKilograms, heightCentimeters, trainingDays, maximums ?? Maximums());

    [Fact]
    public async Task Put_profile_then_get_profile_returns_the_saved_values()
    {
        using var client = factory.CreateClient();
        var payload = Profile(77.5, 180, 4);

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
        Assert.Equal(3, loaded.Maximums.Count);
        Assert.Equal(10, loaded.Maximums.Single(maximum => maximum.ExerciseCode == "push_up").Repetitions);
        Assert.Equal(0, loaded.Maximums.Single(maximum => maximum.ExerciseCode == "pull_up").Repetitions);
        Assert.Equal(20, loaded.Maximums.Single(maximum => maximum.ExerciseCode == "squat").Repetitions);
    }

    [Fact]
    public async Task Put_profile_round_trips_a_zero_maximum()
    {
        using var client = factory.CreateClient();

        using var response = await client.PutAsJsonAsync("/profile", Profile(77.5, 180, 4, Maximums(squat: 0)));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var saved = await response.Content.ReadFromJsonAsync<AthleteProfileResponse>();
        Assert.NotNull(saved);
        Assert.Equal(0, saved!.Maximums.Single(maximum => maximum.ExerciseCode == "squat").Repetitions);
    }

    [Fact]
    public async Task Put_profile_with_a_non_positive_weight_returns_400()
    {
        using var client = factory.CreateClient();

        using var response = await client.PutAsJsonAsync("/profile", Profile(0, 180, 4));

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
            Profile(weightKilograms, heightCentimeters, 4));

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
            Profile(weightKilograms, heightCentimeters, 4));

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
            Profile(77.5, 180, trainingDays));

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
            Profile(77.5, 180, trainingDays));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(problem);
        Assert.Contains("Los días de entrenamiento deben estar entre 3 y 5.", problem!.Detail);
    }

    [Fact]
    public async Task Put_profile_rejects_a_negative_maximum_with_a_spanish_detail()
    {
        using var client = factory.CreateClient();

        using var response = await client.PutAsJsonAsync(
            "/profile",
            Profile(77.5, 180, 4, Maximums(pushUp: -1)));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(problem);
        Assert.Contains("El máximo no puede ser negativo.", problem!.Detail);
    }

    [Fact]
    public async Task Put_profile_rejects_an_unknown_exercise_code_with_a_spanish_detail()
    {
        using var client = factory.CreateClient();
        var payload = new AthleteProfileResponse(
            77.5,
            180,
            4,
            [
                new MaximumResponse("push_up", 10),
                new MaximumResponse("pull_up", 0),
                new MaximumResponse("bench_press", 20),
            ]);

        using var response = await client.PutAsJsonAsync("/profile", payload);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(problem);
        Assert.Contains("El ejercicio indicado no es un ejercicio básico.", problem!.Detail);
    }

    [Fact]
    public async Task Put_profile_rejects_a_missing_basic_exercise_with_a_spanish_detail()
    {
        using var client = factory.CreateClient();
        var payload = new AthleteProfileResponse(
            77.5,
            180,
            4,
            [
                new MaximumResponse("push_up", 10),
                new MaximumResponse("squat", 20),
            ]);

        using var response = await client.PutAsJsonAsync("/profile", payload);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(problem);
        Assert.Contains("Debes indicar el máximo de todos los ejercicios básicos.", problem!.Detail);
    }

    [Fact]
    public async Task Put_profile_without_any_maximums_rejects_with_a_spanish_detail()
    {
        using var client = factory.CreateClient();
        var payload = new
        {
            weightKilograms = 77.5,
            heightCentimeters = 180.0,
            trainingDays = 4,
        };

        using var response = await client.PutAsJsonAsync("/profile", payload);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(problem);
        Assert.Contains("Debes indicar el máximo de todos los ejercicios básicos.", problem!.Detail);
    }

    [Fact]
    public async Task Put_profile_rejects_a_duplicate_exercise_code_with_a_spanish_detail()
    {
        using var client = factory.CreateClient();
        var payload = new AthleteProfileResponse(
            77.5,
            180,
            4,
            [
                new MaximumResponse("push_up", 10),
                new MaximumResponse("push_up", 12),
                new MaximumResponse("squat", 20),
            ]);

        using var response = await client.PutAsJsonAsync("/profile", payload);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(problem);
        Assert.Contains("No puedes repetir el máximo de un mismo ejercicio.", problem!.Detail);
    }
}
