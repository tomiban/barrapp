namespace Barrapp.Domain.Sessions;

/// <summary>
/// Origen de la sesión registrada (spec 0001, US-34; ADR-0014): una sesión del <b>mesociclo</b> en
/// curso o una <b>sesión suelta</b> generada a demanda. El tipo decide qué clave de sesión la
/// identifica: la del mesociclo (mesociclo, microciclo y día) o, en la suelta, solo la fecha.
/// </summary>
public enum SessionLogKind
{
    /// <summary>Sesión del mesociclo en curso; se ancla por mesociclo, microciclo y día.</summary>
    Mesocycle = 0,

    /// <summary>Sesión suelta, fuera del mesociclo (ADR-0014).</summary>
    Suelta = 1,
}
