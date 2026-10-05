namespace Barrapp.Domain.Knowledge;

/// <summary>
/// Naturaleza de un programa general de acondicionamiento: circuito por tiempo o sesión de
/// fuerza por repeticiones.
/// </summary>
public enum RoutineProgramType
{
    /// <summary>Circuito por tiempo.</summary>
    Circuit = 0,

    /// <summary>Sesión de fuerza por repeticiones.</summary>
    Strength = 1,
}
