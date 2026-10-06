namespace Barrapp.Domain.Planning;

/// <summary>
/// Semana del mesociclo (ver <c>GLOSSARY.md</c>, término <i>Microciclo</i>): su número (1–4) y sus
/// sesiones.
/// </summary>
public sealed class Microcycle
{
    internal Microcycle(int number, IReadOnlyList<Session> sessions)
    {
        Number = number;
        Sessions = sessions;
    }

    /// <summary>Número de la semana dentro del mesociclo (1–4).</summary>
    public int Number { get; }

    /// <summary>Sesiones de la semana.</summary>
    public IReadOnlyList<Session> Sessions { get; }
}
