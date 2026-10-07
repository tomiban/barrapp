namespace Barrapp.Domain.Planning;

/// <summary>
/// Sesión de un microciclo: el día que ocupa dentro de la semana (empezando en 1), el
/// <b>día de la semana</b> que el atleta entrena ese día y la fecha concreta que le corresponde en
/// el calendario (#94), y sus filas, en orden —bloque de skill, fuerza por patrón y core— (ver
/// <c>GLOSSARY.md</c>, término <i>Sesión</i>).
/// </summary>
/// <remarks>
/// <see cref="Weekday"/> y <see cref="Date"/> solo existen para las sesiones del mesociclo: una
/// <b>sesión suelta</b> se entrena cuando el atleta decide y no ocupa ningún día del calendario, así
/// que viaja con ambos a <c>null</c>.
/// </remarks>
public sealed class Session
{
    internal Session(int day, DayOfWeek? weekday, DateOnly? date, IReadOnlyList<SessionItem> items)
    {
        Day = day;
        Weekday = weekday;
        Date = date;
        Items = items;
    }

    /// <summary>Día de la sesión dentro del microciclo (1..días de entrenamiento).</summary>
    public int Day { get; }

    /// <summary>Día de la semana en el que cae la sesión; <c>null</c> en una sesión suelta.</summary>
    public DayOfWeek? Weekday { get; }

    /// <summary>
    /// Fecha en la que se entrena la sesión, a partir de la fecha de inicio del mesociclo;
    /// <c>null</c> en una sesión suelta.
    /// </summary>
    public DateOnly? Date { get; }

    /// <summary>Filas de la sesión, en orden de ejecución.</summary>
    public IReadOnlyList<SessionItem> Items { get; }
}
