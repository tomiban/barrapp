using Barrapp.Domain.Athlete;
using Barrapp.Domain.Common;

namespace Barrapp.Domain.UnitTests;

/// <summary>
/// <see cref="AthleteProfile.UpdateMaximums"/>: la costura por la que el cierre del mesociclo
/// (ticket #24) aplica al perfil los máximos calculados por <see cref="Barrapp.Domain.Planning.MaximumAdjustment"/>.
/// Sustituye los valores indicados sin exigir la cobertura completa (a diferencia de
/// <see cref="AthleteProfile.Update"/>), valida cada entrada y es atómico: si algo falla, el
/// perfil no cambia.
/// </summary>
public sealed class AthleteProfileUpdateMaximumsTests
{
    private static readonly Guid UserId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    private static AthleteProfile Profile(
        int pushUp = 10,
        int pullUp = 5,
        int squat = 20) =>
        AthleteProfile.Create(
            UserId,
            weightKilograms: 78,
            heightCentimeters: 180,
            armSpanCentimeters: 180,
            inseamCentimeters: 85,
            trainingDays: 3,
            [
                new MaximumInput("push_up", pushUp),
                new MaximumInput("pull_up", pullUp),
                new MaximumInput("squat", squat),
            ]).Value;

    [Fact]
    public void UpdateMaximums_sets_the_given_maximum_and_leaves_the_rest()
    {
        var profile = Profile(pushUp: 10, pullUp: 5, squat: 20);

        var result = profile.UpdateMaximums([new MaximumInput("push_up", 14)]);

        Assert.True(result.IsSuccess);
        Assert.Equal(14, profile.MaximumFor("push_up"));
        Assert.Equal(5, profile.MaximumFor("pull_up"));
        Assert.Equal(20, profile.MaximumFor("squat"));
    }

    [Fact]
    public void UpdateMaximums_can_raise_several_maximums_at_once()
    {
        var profile = Profile(pushUp: 10, pullUp: 5, squat: 20);

        var result = profile.UpdateMaximums(
        [
            new MaximumInput("push_up", 14),
            new MaximumInput("squat", 24),
        ]);

        Assert.True(result.IsSuccess);
        Assert.Equal(14, profile.MaximumFor("push_up"));
        Assert.Equal(24, profile.MaximumFor("squat"));
    }

    [Fact]
    public void UpdateMaximums_with_an_empty_collection_changes_nothing()
    {
        var profile = Profile(pushUp: 10, pullUp: 5, squat: 20);

        var result = profile.UpdateMaximums([]);

        Assert.True(result.IsSuccess);
        Assert.Equal(10, profile.MaximumFor("push_up"));
        Assert.Equal(5, profile.MaximumFor("pull_up"));
        Assert.Equal(20, profile.MaximumFor("squat"));
    }

    [Fact]
    public void UpdateMaximums_rejects_an_unknown_exercise_code_without_changing_anything()
    {
        var profile = Profile(pushUp: 10, pullUp: 5, squat: 20);

        var result = profile.UpdateMaximums([new MaximumInput("ejercicio-desconocido", 14)]);

        Assert.True(result.IsFailure);
        Assert.Equal(DomainErrors.AthleteProfile.UnknownExerciseCode, result.Error);
        Assert.Equal(10, profile.MaximumFor("push_up"));
    }

    [Fact]
    public void UpdateMaximums_rejects_negative_repetitions_without_changing_anything()
    {
        var profile = Profile(pushUp: 10, pullUp: 5, squat: 20);

        var result = profile.UpdateMaximums([new MaximumInput("push_up", -1)]);

        Assert.True(result.IsFailure);
        Assert.Equal(DomainErrors.AthleteProfile.MaximumMustBeNonNegative, result.Error);
        Assert.Equal(10, profile.MaximumFor("push_up"));
    }

    [Fact]
    public void UpdateMaximums_rejects_duplicate_codes_without_changing_anything()
    {
        var profile = Profile(pushUp: 10, pullUp: 5, squat: 20);

        var result = profile.UpdateMaximums(
        [
            new MaximumInput("push_up", 14),
            new MaximumInput("push_up", 16),
        ]);

        Assert.True(result.IsFailure);
        Assert.Equal(DomainErrors.AthleteProfile.DuplicateExerciseMaximum, result.Error);
        Assert.Equal(10, profile.MaximumFor("push_up"));
    }
}