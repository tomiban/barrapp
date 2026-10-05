using Barrapp.Domain.Athlete;
using Barrapp.Domain.Common;

namespace Barrapp.Domain.UnitTests;

public sealed class AthleteProfileTests
{
    private static readonly Guid UserId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    [Fact]
    public void Create_keeps_the_weight_height_and_training_days()
    {
        var result = AthleteProfile.Create(UserId, 78.5, 181, 4);

        Assert.True(result.IsSuccess);
        Assert.Equal(UserId, result.Value.UserId);
        Assert.Equal(78.5, result.Value.WeightKilograms);
        Assert.Equal(181, result.Value.HeightCentimeters);
        Assert.Equal(4, result.Value.TrainingDays);
    }

    [Theory]
    [InlineData(30, 120)]
    [InlineData(200, 220)]
    [InlineData(78.5, 181)]
    public void Create_accepts_values_within_the_range(double weightKilograms, double heightCentimeters)
    {
        var result = AthleteProfile.Create(UserId, weightKilograms, heightCentimeters, 4);

        Assert.True(result.IsSuccess);
        Assert.Equal(weightKilograms, result.Value.WeightKilograms);
        Assert.Equal(heightCentimeters, result.Value.HeightCentimeters);
    }

    [Theory]
    [InlineData(29.9)]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(200.1)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void Create_rejects_a_weight_out_of_range(double weightKilograms)
    {
        var result = AthleteProfile.Create(UserId, weightKilograms, 181, 4);

        Assert.True(result.IsFailure);
        Assert.Equal(DomainErrors.AthleteProfile.WeightOutOfRange, result.Error);
    }

    [Theory]
    [InlineData(119.9)]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(220.1)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void Create_rejects_a_height_out_of_range(double heightCentimeters)
    {
        var result = AthleteProfile.Create(UserId, 78, heightCentimeters, 4);

        Assert.True(result.IsFailure);
        Assert.Equal(DomainErrors.AthleteProfile.HeightOutOfRange, result.Error);
    }

    [Theory]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    public void Create_accepts_training_days_within_the_range(int trainingDays)
    {
        var result = AthleteProfile.Create(UserId, 78, 181, trainingDays);

        Assert.True(result.IsSuccess);
        Assert.Equal(trainingDays, result.Value.TrainingDays);
    }

    [Theory]
    [InlineData(2)]
    [InlineData(6)]
    public void Create_rejects_training_days_out_of_range(int trainingDays)
    {
        var result = AthleteProfile.Create(UserId, 78, 181, trainingDays);

        Assert.True(result.IsFailure);
        Assert.Equal(DomainErrors.AthleteProfile.TrainingDaysOutOfRange, result.Error);
    }

    [Fact]
    public void Update_replaces_the_measurements()
    {
        var profile = AthleteProfile.Create(UserId, 78, 181, 4).Value;

        var result = profile.Update(80, 182, 5);

        Assert.True(result.IsSuccess);
        Assert.Equal(80, profile.WeightKilograms);
        Assert.Equal(182, profile.HeightCentimeters);
        Assert.Equal(5, profile.TrainingDays);
    }

    [Fact]
    public void Update_accepts_the_range_boundaries()
    {
        var profile = AthleteProfile.Create(UserId, 78, 181, 4).Value;

        var result = profile.Update(30, 220, 3);

        Assert.True(result.IsSuccess);
        Assert.Equal(30, profile.WeightKilograms);
        Assert.Equal(220, profile.HeightCentimeters);
        Assert.Equal(3, profile.TrainingDays);
    }

    [Theory]
    [InlineData(29.9)]
    [InlineData(200.1)]
    public void Update_with_a_weight_out_of_range_keeps_the_previous_values(double weightKilograms)
    {
        var profile = AthleteProfile.Create(UserId, 78, 181, 4).Value;

        var result = profile.Update(weightKilograms, 182, 5);

        Assert.True(result.IsFailure);
        Assert.Equal(DomainErrors.AthleteProfile.WeightOutOfRange, result.Error);
        Assert.Equal(78, profile.WeightKilograms);
        Assert.Equal(181, profile.HeightCentimeters);
        Assert.Equal(4, profile.TrainingDays);
    }

    [Theory]
    [InlineData(119.9)]
    [InlineData(220.1)]
    public void Update_with_a_height_out_of_range_keeps_the_previous_values(double heightCentimeters)
    {
        var profile = AthleteProfile.Create(UserId, 78, 181, 4).Value;

        var result = profile.Update(80, heightCentimeters, 5);

        Assert.True(result.IsFailure);
        Assert.Equal(DomainErrors.AthleteProfile.HeightOutOfRange, result.Error);
        Assert.Equal(78, profile.WeightKilograms);
        Assert.Equal(181, profile.HeightCentimeters);
        Assert.Equal(4, profile.TrainingDays);
    }

    [Theory]
    [InlineData(2)]
    [InlineData(6)]
    public void Update_with_training_days_out_of_range_keeps_the_previous_values(int trainingDays)
    {
        var profile = AthleteProfile.Create(UserId, 78, 181, 4).Value;

        var result = profile.Update(80, 182, trainingDays);

        Assert.True(result.IsFailure);
        Assert.Equal(DomainErrors.AthleteProfile.TrainingDaysOutOfRange, result.Error);
        Assert.Equal(78, profile.WeightKilograms);
        Assert.Equal(181, profile.HeightCentimeters);
        Assert.Equal(4, profile.TrainingDays);
    }
}
