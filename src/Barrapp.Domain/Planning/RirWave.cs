namespace Barrapp.Domain.Planning;

/// <summary>
/// Onda semanal de RIR del mesociclo (#12): la reserva baja de 3 (semana 1) a 1 (semana 3), de
/// modo que las repeticiones prescritas y, con las series fijas, el volumen crecen semana a
/// semana. El RIR es la unidad con la que se fija la intensidad del mesociclo (ver
/// <c>GLOSSARY.md</c>, término <i>RIR</i>).
/// </summary>
/// <remarks>
/// Es pura y determinista. El motor nunca prescribe al fallo: la reserva mínima de la onda es 1 y
/// <see cref="StrengthLoad"/> garantiza al menos una repetición reservada. La semana 4 será el
/// <i>deload</i> (RIR 4 y ~50 % de volumen, ticket #13); hasta entonces conserva la reserva base
/// para no alterar el comportamiento anterior.
/// </remarks>
internal static class RirWave
{
    /// <summary>Repeticiones en reserva que le corresponden a un microciclo (1–4).</summary>
    internal static int RepsInReserve(int microcycleNumber) => microcycleNumber switch
    {
        1 => StrengthLoad.BaseRepsInReserve,     // RIR 3 — base
        2 => StrengthLoad.BaseRepsInReserve - 1, // RIR 2 — +volumen
        3 => StrengthLoad.BaseRepsInReserve - 2, // RIR 1 — +volumen

        // TODO(#13): la semana 4 es el deload (RIR 4 y ~50 % del volumen). Mientras el deload no
        // exista, conserva la reserva base para no cambiar el comportamiento previo.
        4 => StrengthLoad.BaseRepsInReserve,
        _ => throw new ArgumentOutOfRangeException(nameof(microcycleNumber)),
    };
}
