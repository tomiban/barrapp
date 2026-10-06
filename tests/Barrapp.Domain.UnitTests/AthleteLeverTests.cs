using Barrapp.Domain.Athlete;

namespace Barrapp.Domain.UnitTests;

/// <summary>
/// Clasificación de la palanca del atleta (#68, ADR-0011): un proxy determinista de peso × altura ×
/// proporción (envergadura/entrepierna) reparte al atleta en tres cubos y deriva de ahí el ajuste de
/// series del bloque de skill y la nota de ritmo esperado.
/// </summary>
public sealed class AthleteLeverTests
{
    private static readonly Guid UserId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    [Fact]
    public void Classify_is_deterministic_for_the_same_measurements()
    {
        var first = AthleteLever.Classify(Profile(78, 180, 180, 85));
        var second = AthleteLever.Classify(Profile(78, 180, 180, 85));

        Assert.Equal(first.Bucket, second.Bucket);
        Assert.Equal(first.SetAdjustment, second.SetAdjustment);
        Assert.Equal(first.Note, second.Note);
    }

    [Fact]
    public void Classify_returns_favorable_for_a_light_short_athlete()
    {
        var lever = AthleteLever.Classify(Profile(55, 165, 165, 82));

        Assert.Equal(LeverBucket.Favorable, lever.Bucket);
        Assert.Equal(-1, lever.SetAdjustment);
    }

    [Fact]
    public void Classify_returns_neutral_for_a_mid_size_athlete()
    {
        var lever = AthleteLever.Classify(Profile(75, 175, 175, 80));

        Assert.Equal(LeverBucket.Neutral, lever.Bucket);
        Assert.Equal(0, lever.SetAdjustment);
    }

    [Fact]
    public void Classify_returns_unfavorable_for_a_heavy_tall_athlete()
    {
        var lever = AthleteLever.Classify(Profile(100, 185, 190, 80));

        Assert.Equal(LeverBucket.Unfavorable, lever.Bucket);
        Assert.Equal(1, lever.SetAdjustment);
    }

    [Fact]
    public void Classify_penalizes_more_mass_and_length()
    {
        // A igualdad de proporciones, más masa y más altura solo pueden empeorar el cubo.
        var lighter = AthleteLever.Classify(Profile(60, 165, 165, 80));
        var heavier = AthleteLever.Classify(Profile(90, 165, 165, 80));
        var taller = AthleteLever.Classify(Profile(60, 200, 200, 80));

        Assert.True(lighter.SetAdjustment < heavier.SetAdjustment);
        Assert.True(lighter.SetAdjustment < taller.SetAdjustment);
    }

    [Fact]
    public void Classify_penalizes_a_longer_arm_span_relative_to_the_inseam()
    {
        // Mismos peso y altura: más envergadura respecto a la entrepierna alarga la palanca.
        var longerArms = AthleteLever.Classify(Profile(80, 175, 195, 75));
        var longerLegs = AthleteLever.Classify(Profile(80, 175, 160, 95));

        Assert.True(longerArms.SetAdjustment > longerLegs.SetAdjustment);
    }

    [Fact]
    public void Every_bucket_carries_a_pace_note()
    {
        var favorable = AthleteLever.Classify(Profile(55, 165, 165, 82)).Note;
        var neutral = AthleteLever.Classify(Profile(75, 175, 175, 80)).Note;
        var unfavorable = AthleteLever.Classify(Profile(100, 185, 190, 80)).Note;

        Assert.All(new[] { favorable, neutral, unfavorable }, note => Assert.Contains("progreso", note));
        Assert.NotEqual(favorable, unfavorable);
    }

    private static AthleteProfile Profile(
        double weightKilograms,
        double heightCentimeters,
        double armSpanCentimeters,
        double inseamCentimeters) =>
        AthleteProfile.Create(
            UserId,
            weightKilograms,
            heightCentimeters,
            armSpanCentimeters,
            inseamCentimeters,
            3,
            [
                new MaximumInput("push_up", 10),
                new MaximumInput("pull_up", 5),
                new MaximumInput("squat", 20),
            ]).Value;
}
