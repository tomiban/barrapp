using Barrapp.Domain.Athlete;
using Barrapp.Domain.Common;

namespace Barrapp.Domain.UnitTests;

public sealed class MaximumTests
{
    [Fact]
    public void Create_accepts_zero_repetitions_as_a_regression()
    {
        var result = Maximum.Create("push_up", 0);

        Assert.True(result.IsSuccess);
        Assert.Equal("push_up", result.Value.ExerciseCode);
        Assert.Equal(0, result.Value.Repetitions);
    }

    [Fact]
    public void Create_keeps_a_positive_number_of_repetitions()
    {
        var result = Maximum.Create("pull_up", 12);

        Assert.True(result.IsSuccess);
        Assert.Equal(12, result.Value.Repetitions);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(int.MinValue)]
    public void Create_rejects_negative_repetitions(int repetitions)
    {
        var result = Maximum.Create("squat", repetitions);

        Assert.True(result.IsFailure);
        Assert.Equal(DomainErrors.AthleteProfile.MaximumMustBeNonNegative, result.Error);
    }

    [Theory]
    [InlineData("bench_press")]
    [InlineData("")]
    [InlineData("PUSH_UP")]
    public void Create_rejects_an_unknown_exercise_code(string exerciseCode)
    {
        var result = Maximum.Create(exerciseCode, 10);

        Assert.True(result.IsFailure);
        Assert.Equal(DomainErrors.AthleteProfile.UnknownExerciseCode, result.Error);
    }

    [Fact]
    public void The_basic_exercise_catalog_has_one_stable_code_per_pattern()
    {
        Assert.Equal(["push_up", "pull_up", "squat"], BasicExercises.All.Select(exercise => exercise.Code));
        Assert.Equal(3, BasicExercises.All.Select(exercise => exercise.Pattern).Distinct().Count());
        Assert.Equal("Flexión", BasicExercises.FindByCode("push_up")!.Name);
        Assert.Null(BasicExercises.FindByCode("bench_press"));
    }
}
