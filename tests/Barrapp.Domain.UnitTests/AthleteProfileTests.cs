using Barrapp.Domain.Athlete;
using Barrapp.Domain.Common;

namespace Barrapp.Domain.UnitTests;

public sealed class AthleteProfileTests
{
    private static readonly Guid UserId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    [Fact]
    public void Create_keeps_the_weight_and_height()
    {
        var result = AthleteProfile.Create(UserId, 78.5, 181);

        Assert.True(result.IsSuccess);
        Assert.Equal(UserId, result.Value.UserId);
        Assert.Equal(78.5, result.Value.WeightKilograms);
        Assert.Equal(181, result.Value.HeightCentimeters);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(double.NaN)]
    public void Create_rejects_a_non_positive_weight(double weightKilograms)
    {
        var result = AthleteProfile.Create(UserId, weightKilograms, 181);

        Assert.True(result.IsFailure);
        Assert.Equal(DomainErrors.AthleteProfile.WeightMustBePositive, result.Error);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(double.NaN)]
    public void Create_rejects_a_non_positive_height(double heightCentimeters)
    {
        var result = AthleteProfile.Create(UserId, 78, heightCentimeters);

        Assert.True(result.IsFailure);
        Assert.Equal(DomainErrors.AthleteProfile.HeightMustBePositive, result.Error);
    }

    [Fact]
    public void Update_replaces_the_measurements()
    {
        var profile = AthleteProfile.Create(UserId, 78, 181).Value;

        var result = profile.Update(80, 182);

        Assert.True(result.IsSuccess);
        Assert.Equal(80, profile.WeightKilograms);
        Assert.Equal(182, profile.HeightCentimeters);
    }

    [Fact]
    public void Update_with_an_invalid_measurement_keeps_the_previous_values()
    {
        var profile = AthleteProfile.Create(UserId, 78, 181).Value;

        var result = profile.Update(-5, 182);

        Assert.True(result.IsFailure);
        Assert.Equal(DomainErrors.AthleteProfile.WeightMustBePositive, result.Error);
        Assert.Equal(78, profile.WeightKilograms);
        Assert.Equal(181, profile.HeightCentimeters);
    }
}
