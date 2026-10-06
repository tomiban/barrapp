using Barrapp.Domain.Athlete;
using Barrapp.Domain.Common;

namespace Barrapp.Domain.UnitTests;

public sealed class AthleteProfileTests
{
    private static readonly Guid UserId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    private const double ArmSpanCentimeters = 180;

    private const double InseamCentimeters = 85;

    private static MaximumInput[] ValidMaximums() =>
    [
        new MaximumInput("push_up", 10),
        new MaximumInput("pull_up", 0),
        new MaximumInput("squat", 20),
    ];

    [Fact]
    public void Create_keeps_the_weight_height_and_training_days()
    {
        var result = AthleteProfile.Create(UserId, 78.5, 181, ArmSpanCentimeters, InseamCentimeters, 4, ValidMaximums());

        Assert.True(result.IsSuccess);
        Assert.Equal(UserId, result.Value.UserId);
        Assert.Equal(78.5, result.Value.WeightKilograms);
        Assert.Equal(181, result.Value.HeightCentimeters);
        Assert.Equal(ArmSpanCentimeters, result.Value.ArmSpanCentimeters);
        Assert.Equal(InseamCentimeters, result.Value.InseamCentimeters);
        Assert.Equal(4, result.Value.TrainingDays);
    }

    [Theory]
    [InlineData(30, 120)]
    [InlineData(200, 220)]
    [InlineData(78.5, 181)]
    public void Create_accepts_values_within_the_range(double weightKilograms, double heightCentimeters)
    {
        var result = AthleteProfile.Create(UserId, weightKilograms, heightCentimeters, ArmSpanCentimeters, InseamCentimeters, 4, ValidMaximums());

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
        var result = AthleteProfile.Create(UserId, weightKilograms, 181, ArmSpanCentimeters, InseamCentimeters, 4, ValidMaximums());

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
        var result = AthleteProfile.Create(UserId, 78, heightCentimeters, ArmSpanCentimeters, InseamCentimeters, 4, ValidMaximums());

        Assert.True(result.IsFailure);
        Assert.Equal(DomainErrors.AthleteProfile.HeightOutOfRange, result.Error);
    }

    [Theory]
    [InlineData(100, 50)]
    [InlineData(250, 130)]
    [InlineData(180, 85)]
    public void Create_accepts_the_arm_span_and_inseam_within_the_range(
        double armSpanCentimeters,
        double inseamCentimeters)
    {
        var result = AthleteProfile.Create(
            UserId,
            78,
            181,
            armSpanCentimeters,
            inseamCentimeters,
            4,
            ValidMaximums());

        Assert.True(result.IsSuccess);
        Assert.Equal(armSpanCentimeters, result.Value.ArmSpanCentimeters);
        Assert.Equal(inseamCentimeters, result.Value.InseamCentimeters);
    }

    [Theory]
    [InlineData(99.9)]
    [InlineData(250.1)]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void Create_rejects_an_arm_span_out_of_range(double armSpanCentimeters)
    {
        var result = AthleteProfile.Create(
            UserId,
            78,
            181,
            armSpanCentimeters,
            InseamCentimeters,
            4,
            ValidMaximums());

        Assert.True(result.IsFailure);
        Assert.Equal(DomainErrors.AthleteProfile.ArmSpanOutOfRange, result.Error);
    }

    [Theory]
    [InlineData(49.9)]
    [InlineData(130.1)]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void Create_rejects_an_inseam_out_of_range(double inseamCentimeters)
    {
        var result = AthleteProfile.Create(
            UserId,
            78,
            181,
            ArmSpanCentimeters,
            inseamCentimeters,
            4,
            ValidMaximums());

        Assert.True(result.IsFailure);
        Assert.Equal(DomainErrors.AthleteProfile.InseamOutOfRange, result.Error);
    }

    [Theory]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    public void Create_accepts_training_days_within_the_range(int trainingDays)
    {
        var result = AthleteProfile.Create(UserId, 78, 181, ArmSpanCentimeters, InseamCentimeters, trainingDays, ValidMaximums());

        Assert.True(result.IsSuccess);
        Assert.Equal(trainingDays, result.Value.TrainingDays);
    }

    [Theory]
    [InlineData(2)]
    [InlineData(6)]
    public void Create_rejects_training_days_out_of_range(int trainingDays)
    {
        var result = AthleteProfile.Create(UserId, 78, 181, ArmSpanCentimeters, InseamCentimeters, trainingDays, ValidMaximums());

        Assert.True(result.IsFailure);
        Assert.Equal(DomainErrors.AthleteProfile.TrainingDaysOutOfRange, result.Error);
    }

    [Fact]
    public void Create_keeps_the_maximums_including_a_zero_regression()
    {
        var result = AthleteProfile.Create(UserId, 78, 181, ArmSpanCentimeters, InseamCentimeters, 4, ValidMaximums());

        Assert.True(result.IsSuccess);
        var maximums = result.Value.Maximums.ToDictionary(maximum => maximum.ExerciseCode);
        Assert.Equal(3, maximums.Count);
        Assert.Equal(10, maximums["push_up"].Repetitions);
        Assert.Equal(0, maximums["pull_up"].Repetitions);
        Assert.Equal(20, maximums["squat"].Repetitions);
    }

    [Fact]
    public void MaximumFor_returns_the_repetitions_of_a_known_exercise()
    {
        var profile = AthleteProfile
            .Create(UserId, 78, 181, ArmSpanCentimeters, InseamCentimeters, 4, ValidMaximums())
            .Value;

        Assert.Equal(10, profile.MaximumFor("push_up"));
    }

    [Fact]
    public void MaximumFor_returns_zero_when_the_maximum_is_zero()
    {
        var profile = AthleteProfile
            .Create(UserId, 78, 181, ArmSpanCentimeters, InseamCentimeters, 4, ValidMaximums())
            .Value;

        Assert.Equal(0, profile.MaximumFor("pull_up"));
    }

    [Fact]
    public void MaximumFor_returns_null_for_an_unknown_exercise()
    {
        var profile = AthleteProfile
            .Create(UserId, 78, 181, ArmSpanCentimeters, InseamCentimeters, 4, ValidMaximums())
            .Value;

        Assert.Null(profile.MaximumFor("bench_press"));
    }

    [Fact]
    public void Create_rejects_a_missing_basic_exercise()
    {
        var maximums = new MaximumInput[]
        {
            new("push_up", 10),
            new("squat", 20),
        };

        var result = AthleteProfile.Create(UserId, 78, 181, ArmSpanCentimeters, InseamCentimeters, 4, maximums);

        Assert.True(result.IsFailure);
        Assert.Equal(DomainErrors.AthleteProfile.MissingExerciseMaximum, result.Error);
    }

    [Fact]
    public void Create_rejects_a_duplicate_exercise_code()
    {
        var maximums = new MaximumInput[]
        {
            new("push_up", 10),
            new("push_up", 12),
            new("squat", 20),
        };

        var result = AthleteProfile.Create(UserId, 78, 181, ArmSpanCentimeters, InseamCentimeters, 4, maximums);

        Assert.True(result.IsFailure);
        Assert.Equal(DomainErrors.AthleteProfile.DuplicateExerciseMaximum, result.Error);
    }

    [Fact]
    public void Create_rejects_an_unknown_exercise_code()
    {
        var maximums = new MaximumInput[]
        {
            new("push_up", 10),
            new("pull_up", 0),
            new("bench_press", 20),
        };

        var result = AthleteProfile.Create(UserId, 78, 181, ArmSpanCentimeters, InseamCentimeters, 4, maximums);

        Assert.True(result.IsFailure);
        Assert.Equal(DomainErrors.AthleteProfile.UnknownExerciseCode, result.Error);
    }

    [Fact]
    public void Create_rejects_a_negative_maximum()
    {
        var maximums = new MaximumInput[]
        {
            new("push_up", -1),
            new("pull_up", 0),
            new("squat", 20),
        };

        var result = AthleteProfile.Create(UserId, 78, 181, ArmSpanCentimeters, InseamCentimeters, 4, maximums);

        Assert.True(result.IsFailure);
        Assert.Equal(DomainErrors.AthleteProfile.MaximumMustBeNonNegative, result.Error);
    }

    [Fact]
    public void Update_replaces_the_measurements()
    {
        var profile = AthleteProfile.Create(UserId, 78, 181, ArmSpanCentimeters, InseamCentimeters, 4, ValidMaximums()).Value;

        var result = profile.Update(80, 182, 190, 90, 5, ValidMaximums());

        Assert.True(result.IsSuccess);
        Assert.Equal(80, profile.WeightKilograms);
        Assert.Equal(182, profile.HeightCentimeters);
        Assert.Equal(190, profile.ArmSpanCentimeters);
        Assert.Equal(90, profile.InseamCentimeters);
        Assert.Equal(5, profile.TrainingDays);
    }

    [Fact]
    public void Update_accepts_the_range_boundaries()
    {
        var profile = AthleteProfile.Create(UserId, 78, 181, ArmSpanCentimeters, InseamCentimeters, 4, ValidMaximums()).Value;

        var result = profile.Update(30, 220, 250, 130, 3, ValidMaximums());

        Assert.True(result.IsSuccess);
        Assert.Equal(30, profile.WeightKilograms);
        Assert.Equal(220, profile.HeightCentimeters);
        Assert.Equal(250, profile.ArmSpanCentimeters);
        Assert.Equal(130, profile.InseamCentimeters);
        Assert.Equal(3, profile.TrainingDays);
    }

    [Fact]
    public void Update_replaces_the_maximums_and_accepts_zero()
    {
        var profile = AthleteProfile.Create(UserId, 78, 181, ArmSpanCentimeters, InseamCentimeters, 4, ValidMaximums()).Value;

        var result = profile.Update(
            80,
            182,
            ArmSpanCentimeters,
            InseamCentimeters,
            5,
            [
                new MaximumInput("push_up", 15),
                new MaximumInput("pull_up", 1),
                new MaximumInput("squat", 0),
            ]);

        Assert.True(result.IsSuccess);
        var maximums = profile.Maximums.ToDictionary(maximum => maximum.ExerciseCode);
        Assert.Equal(15, maximums["push_up"].Repetitions);
        Assert.Equal(1, maximums["pull_up"].Repetitions);
        Assert.Equal(0, maximums["squat"].Repetitions);
        Assert.Equal(3, maximums.Count);
    }

    [Theory]
    [InlineData(29.9)]
    [InlineData(200.1)]
    public void Update_with_a_weight_out_of_range_keeps_the_previous_values(double weightKilograms)
    {
        var profile = AthleteProfile.Create(UserId, 78, 181, ArmSpanCentimeters, InseamCentimeters, 4, ValidMaximums()).Value;

        var result = profile.Update(weightKilograms, 182, ArmSpanCentimeters, InseamCentimeters, 5, ValidMaximums());

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
        var profile = AthleteProfile.Create(UserId, 78, 181, ArmSpanCentimeters, InseamCentimeters, 4, ValidMaximums()).Value;

        var result = profile.Update(80, heightCentimeters, ArmSpanCentimeters, InseamCentimeters, 5, ValidMaximums());

        Assert.True(result.IsFailure);
        Assert.Equal(DomainErrors.AthleteProfile.HeightOutOfRange, result.Error);
        Assert.Equal(78, profile.WeightKilograms);
        Assert.Equal(181, profile.HeightCentimeters);
        Assert.Equal(4, profile.TrainingDays);
    }

    [Theory]
    [InlineData(99.9)]
    [InlineData(250.1)]
    public void Update_with_an_arm_span_out_of_range_keeps_the_previous_values(
        double armSpanCentimeters)
    {
        var profile = AthleteProfile.Create(UserId, 78, 181, ArmSpanCentimeters, InseamCentimeters, 4, ValidMaximums()).Value;

        var result = profile.Update(80, 182, armSpanCentimeters, InseamCentimeters, 5, ValidMaximums());

        Assert.True(result.IsFailure);
        Assert.Equal(DomainErrors.AthleteProfile.ArmSpanOutOfRange, result.Error);
        Assert.Equal(78, profile.WeightKilograms);
        Assert.Equal(181, profile.HeightCentimeters);
        Assert.Equal(ArmSpanCentimeters, profile.ArmSpanCentimeters);
        Assert.Equal(InseamCentimeters, profile.InseamCentimeters);
        Assert.Equal(4, profile.TrainingDays);
    }

    [Theory]
    [InlineData(49.9)]
    [InlineData(130.1)]
    public void Update_with_an_inseam_out_of_range_keeps_the_previous_values(
        double inseamCentimeters)
    {
        var profile = AthleteProfile.Create(UserId, 78, 181, ArmSpanCentimeters, InseamCentimeters, 4, ValidMaximums()).Value;

        var result = profile.Update(80, 182, ArmSpanCentimeters, inseamCentimeters, 5, ValidMaximums());

        Assert.True(result.IsFailure);
        Assert.Equal(DomainErrors.AthleteProfile.InseamOutOfRange, result.Error);
        Assert.Equal(78, profile.WeightKilograms);
        Assert.Equal(181, profile.HeightCentimeters);
        Assert.Equal(ArmSpanCentimeters, profile.ArmSpanCentimeters);
        Assert.Equal(InseamCentimeters, profile.InseamCentimeters);
        Assert.Equal(4, profile.TrainingDays);
    }

    [Theory]
    [InlineData(2)]
    [InlineData(6)]
    public void Update_with_training_days_out_of_range_keeps_the_previous_values(int trainingDays)
    {
        var profile = AthleteProfile.Create(UserId, 78, 181, ArmSpanCentimeters, InseamCentimeters, 4, ValidMaximums()).Value;

        var result = profile.Update(80, 182, ArmSpanCentimeters, InseamCentimeters, trainingDays, ValidMaximums());

        Assert.True(result.IsFailure);
        Assert.Equal(DomainErrors.AthleteProfile.TrainingDaysOutOfRange, result.Error);
        Assert.Equal(78, profile.WeightKilograms);
        Assert.Equal(181, profile.HeightCentimeters);
        Assert.Equal(4, profile.TrainingDays);
    }

    [Fact]
    public void Update_with_a_missing_basic_exercise_keeps_the_previous_maximums()
    {
        var profile = AthleteProfile.Create(UserId, 78, 181, ArmSpanCentimeters, InseamCentimeters, 4, ValidMaximums()).Value;

        var result = profile.Update(
            80,
            182,
            ArmSpanCentimeters,
            InseamCentimeters,
            5,
            [
                new MaximumInput("push_up", 15),
                new MaximumInput("squat", 25),
            ]);

        Assert.True(result.IsFailure);
        Assert.Equal(DomainErrors.AthleteProfile.MissingExerciseMaximum, result.Error);
        Assert.Equal(78, profile.WeightKilograms);
        Assert.Equal(181, profile.HeightCentimeters);
        Assert.Equal(4, profile.TrainingDays);
        var maximums = profile.Maximums.ToDictionary(maximum => maximum.ExerciseCode);
        Assert.Equal(10, maximums["push_up"].Repetitions);
        Assert.Equal(0, maximums["pull_up"].Repetitions);
        Assert.Equal(20, maximums["squat"].Repetitions);
    }

    [Fact]
    public void Update_with_a_negative_maximum_keeps_the_previous_maximums()
    {
        var profile = AthleteProfile.Create(UserId, 78, 181, ArmSpanCentimeters, InseamCentimeters, 4, ValidMaximums()).Value;

        var result = profile.Update(
            80,
            182,
            ArmSpanCentimeters,
            InseamCentimeters,
            5,
            [
                new MaximumInput("push_up", -2),
                new MaximumInput("pull_up", 0),
                new MaximumInput("squat", 20),
            ]);

        Assert.True(result.IsFailure);
        Assert.Equal(DomainErrors.AthleteProfile.MaximumMustBeNonNegative, result.Error);
        var maximums = profile.Maximums.ToDictionary(maximum => maximum.ExerciseCode);
        Assert.Equal(10, maximums["push_up"].Repetitions);
    }
}
