namespace Barrapp.Domain.Planning;

/// <summary>
/// Onda semanal de RIR del mesociclo (#12, #13): la reserva baja de 3 (semana 1) a 1 (semana 3) y
/// sube a 4 en la semana 4 (<i>deload</i>). Con las series de las tres primeras semanas fijas, las
/// repeticiones prescritas y el volumen crecen semana a semana hasta el deload. El RIR es la unidad
/// con la que se fija la intensidad del mesociclo (ver <c>GLOSSARY.md</c>, término <i>RIR</i>).
/// </summary>
/// <remarks>
/// Es pura y determinista. El motor nunca prescribe al fallo: la reserva mínima de la onda es 1
/// (semana 3) y <see cref="StrengthLoad"/> garantiza al menos una repetición reservada. La semana 4
/// es el <i>deload</i> (<c>GLOSSARY.md</c>): RIR 4, el tope más conservador de la onda, y el
/// volumen ~50 % que acompaña lo reduce <see cref="PlanGenerator"/> bajando las series del mismo
/// microciclo.
/// </remarks>
internal static class RirWave
{
    /// <summary>Repeticiones en reserva que le corresponden a un microciclo (1–4).</summary>
    internal static int RepsInReserve(int microcycleNumber) => microcycleNumber switch
    {
        1 => StrengthLoad.BaseRepsInReserve,     // RIR 3 — base
        2 => StrengthLoad.BaseRepsInReserve - 1, // RIR 2 — +volumen
        3 => StrengthLoad.BaseRepsInReserve - 2, // RIR 1 — +volumen
        4 => StrengthLoad.BaseRepsInReserve + 1, // RIR 4 — deload
        _ => throw new ArgumentOutOfRangeException(nameof(microcycleNumber)),
    };
}
