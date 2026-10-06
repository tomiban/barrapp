namespace Barrapp.Domain.Athlete;

/// <summary>
/// Palanca del atleta en los skills de tensión (ver <c>GLOSSARY.md</c>, término <i>Palanca</i>):
/// clasifica su mecánica en un <see cref="LeverBucket"/> y deriva de ahí el ajuste de series del
/// bloque de skill y la nota de ritmo esperado. Es un valor puro y determinista: mismas medidas,
/// mismo cubo.
/// </summary>
/// <remarks>
/// El proxy sigue el ADR-0011: <c>τ ≈ peso × altura × (envergadura / entrepierna)</c>. La masa y la
/// longitud alargan el brazo de palanca; a igual talla y peso, más envergadura respecto a la
/// entrepierna penaliza y más entrepierna favorece. Los umbrales están calibrados contra un atleta
/// de referencia y son heurísticos: se recalibrarán con datos reales post-MVP, según el ADR.
/// </remarks>
public sealed class AthleteLever
{
    // Atleta de referencia: 75 kg, 175 cm, envergadura igual a la talla y entrepierna de 80 cm.
    private const double ReferenceWeightKilograms = 75;
    private const double ReferenceHeightCentimeters = 175;
    private const double ReferenceArmSpanCentimeters = 175;
    private const double ReferenceInseamCentimeters = 80;

    // Banda neutra alrededor de la referencia: por debajo, palanca favorable; por encima, desfavorable.
    private const double FavorableRatioLimit = 0.75;
    private const double UnfavorableRatioLimit = 1.25;

    private static readonly double ReferenceIndex =
        ReferenceWeightKilograms
        * ReferenceHeightCentimeters
        * (ReferenceArmSpanCentimeters / ReferenceInseamCentimeters);

    private AthleteLever(LeverBucket bucket)
    {
        Bucket = bucket;
    }

    /// <summary>Cubo de palanca del atleta.</summary>
    public LeverBucket Bucket { get; }

    /// <summary>Ajuste de series del bloque de skill: −1 favorable, 0 neutra, +1 desfavorable.</summary>
    public int SetAdjustment => Bucket switch
    {
        LeverBucket.Favorable => -1,
        LeverBucket.Neutral => 0,
        LeverBucket.Unfavorable => 1,
        _ => throw new ArgumentOutOfRangeException(nameof(Bucket)),
    };

    /// <summary>Nota de ritmo esperado, en español, para la UI.</summary>
    public string Note => Bucket switch
    {
        LeverBucket.Favorable =>
            "Tu palanca es favorable para este skill: espera un progreso más rápido.",
        LeverBucket.Neutral =>
            "Tu palanca es neutra para este skill: espera un progreso en línea con el criterio de la etapa.",
        LeverBucket.Unfavorable =>
            "Tu palanca es desfavorable para este skill: espera un progreso más lento.",
        _ => throw new ArgumentOutOfRangeException(nameof(Bucket)),
    };

    /// <summary>Clasifica la palanca de <paramref name="profile"/> en su cubo.</summary>
    public static AthleteLever Classify(AthleteProfile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);

        var ratio = LeverIndex(profile) / ReferenceIndex;
        var bucket = ratio < FavorableRatioLimit
            ? LeverBucket.Favorable
            : ratio > UnfavorableRatioLimit
                ? LeverBucket.Unfavorable
                : LeverBucket.Neutral;

        return new AthleteLever(bucket);
    }

    // Proxy determinista del torque: masa × longitud del brazo de palanca × proporción.
    private static double LeverIndex(AthleteProfile profile) =>
        profile.WeightKilograms
        * profile.HeightCentimeters
        * (profile.ArmSpanCentimeters / profile.InseamCentimeters);
}
