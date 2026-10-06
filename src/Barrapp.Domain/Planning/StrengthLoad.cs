namespace Barrapp.Domain.Planning;

/// <summary>
/// Deriva la prescripción de repeticiones de fuerza a partir del máximo del atleta (#11): la
/// semana base deja <see cref="BaseRepsInReserve"/> repeticiones en reserva, de modo que ninguna
/// serie llega al fallo (ver <c>GLOSSARY.md</c>, término <i>RIR</i>). La onda semanal de RIR (#12)
/// reutilizará <see cref="Derive"/> variando las repeticiones en reserva por microciclo.
/// </summary>
/// <remarks>
/// Es pura y determinista. Un máximo de 0 (o 1) no permite derivar una carga con margen: hasta que
/// #17 resuelva la regresión, cae a una prescripción neutra y positiva, nunca a 0 repeticiones.
/// </remarks>
internal static class StrengthLoad
{
    /// <summary>Repeticiones en reserva de la semana base (RIR 3).</summary>
    internal const int BaseRepsInReserve = 3;

    /// <summary>Amplitud del rango prescrito por debajo del objetivo, en repeticiones.</summary>
    internal const int RepsRangeWidth = 2;

    /// <summary>Repetición mínima de la prescripción neutra de regresión (#17 pendiente).</summary>
    internal const int RegressionReps = 1;

    /// <summary>
    /// Repeticiones (mínimo y máximo) que le corresponden a un máximo dado, dejando
    /// <paramref name="repsInReserve"/> repeticiones antes del fallo. Para un máximo sin margen
    /// suficiente (0, 1 o menor que la reserva) devuelve la prescripción neutra de regresión.
    /// </summary>
    internal static (int Min, int Max) Derive(int maximum, int repsInReserve)
    {
        // El motor nunca prescribe al fallo: se reserva al menos una repetición, aunque el llamador
        // pida una intensidad mayor (la onda de #12 bajará hasta RIR 1, nunca a 0).
        var reserved = Math.Max(RegressionReps, repsInReserve);
        var target = maximum - reserved;
        if (target < RegressionReps)
        {
            return (RegressionReps, RegressionReps);
        }

        return (Math.Max(RegressionReps, target - RepsRangeWidth), target);
    }
}
