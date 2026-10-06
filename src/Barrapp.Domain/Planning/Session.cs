namespace Barrapp.Domain.Planning;

/// <summary>
/// Sesión de un microciclo: el día que ocupa dentro de la semana (empezando en 1) y sus filas, en
/// orden —bloque de skill, fuerza por patrón y core— (ver <c>GLOSSARY.md</c>, término
/// <i>Sesión</i>).
/// </summary>
public sealed class Session
{
    internal Session(int day, IReadOnlyList<SessionItem> items)
    {
        Day = day;
        Items = items;
    }

    /// <summary>Día de la sesión dentro del microciclo (1..días de entrenamiento).</summary>
    public int Day { get; }

    /// <summary>Filas de la sesión, en orden de ejecución.</summary>
    public IReadOnlyList<SessionItem> Items { get; }
}
