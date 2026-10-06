namespace Barrapp.Domain.Planning;

/// <summary>
/// Deriva la prescripción de repeticiones de fuerza a partir del máximo del atleta (#11): la
/// semana base deja <see cref="BaseRepsInReserve"/> repeticiones en reserva, de modo que ninguna
/// serie llega al fallo (ver <c>GLOSSARY.md</c>, término <i>RIR</i>). La onda semanal de RIR (#12)
/// reutiliza <see cref="Derive"/> variando las repeticiones en reserva por microciclo
/// (<see cref="RirWave"/>).
/// </summary>
/// <remarks>
/// En un ejercicio medido en repeticiones, prescribir <c>M − rir</c> reps equivale a un
/// <c>(M − rir) / M</c> del máximo: el porcentaje queda determinado por el máximo y la reserva
/// (RIR), que es la unidad de intensidad del mesociclo (<c>GLOSSARY.md</c>), en vez de fijarse como
/// una constante. Por eso la derivación se parametriza por RIR y no por un porcentaje fijo.
/// Es pura y determinista. Siempre que el máximo lo permita (≥ 2) la derivación deja al menos una
/// repetición en reserva y el tope prescrito queda estrictamente por debajo del máximo. Un máximo
/// de 0 o 1 no admite reserva alguna, así que cae a un marcador neutro y positivo
/// (<see cref="NeutralReps"/>), nunca a 0 repeticiones. Convertir ese caso en una regresión real
/// es responsabilidad de #17.
/// </remarks>
internal static class StrengthLoad
{
    /// <summary>Repeticiones en reserva de la semana base (RIR 3).</summary>
    internal const int BaseRepsInReserve = 3;

    /// <summary>Amplitud del rango prescrito por debajo del objetivo, en repeticiones.</summary>
    internal const int RepsRangeWidth = 2;

    /// <summary>
    /// Repetición neutra y positiva que se usa cuando el máximo no permite dejar reserva (0 o 1).
    /// Es un marcador pendiente de la regresión (#17), no una garantía de seguridad.
    /// </summary>
    internal const int NeutralReps = 1;

    /// <summary>
    /// Repeticiones (mínimo y máximo) que le corresponden a un máximo dado, dejando
    /// <paramref name="repsInReserve"/> repeticiones antes del fallo. Si el máximo lo permite
    /// (≥ 2) deja al menos una repetición reservada y el tope queda por debajo del máximo; con un
    /// máximo de 0 o 1 no hay margen y devuelve el marcador neutro (<see cref="NeutralReps"/>).
    /// </summary>
    internal static (int Min, int Max) Derive(int maximum, int repsInReserve)
    {
        // El motor nunca prescribe al fallo: se reserva al menos una repetición, aunque el llamador
        // pida una intensidad mayor (la onda de #12 bajará hasta RIR 1, nunca a 0).
        var reserved = Math.Max(NeutralReps, repsInReserve);
        var target = maximum - reserved;
        if (target < NeutralReps)
        {
            return (NeutralReps, NeutralReps);
        }

        return (Math.Max(NeutralReps, target - RepsRangeWidth), target);
    }
}
