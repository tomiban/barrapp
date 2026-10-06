namespace Barrapp.Domain.Athlete;

/// <summary>
/// Cubo de palanca del atleta para los skills de tensión (ver <c>GLOSSARY.md</c>, término
/// <i>Palanca</i>): si su mecánica favorece, deja igual o penaliza el aprendizaje del skill.
/// </summary>
public enum LeverBucket
{
    /// <summary>Palanca corta: menos torque relativo, progreso esperado más rápido.</summary>
    Favorable = 0,

    /// <summary>Palanca en el rango de referencia: sin ajuste.</summary>
    Neutral = 1,

    /// <summary>Palanca larga: más torque relativo, progreso esperado más lento.</summary>
    Unfavorable = 2,
}
