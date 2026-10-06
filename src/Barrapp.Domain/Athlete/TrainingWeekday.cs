namespace Barrapp.Domain.Athlete;

/// <summary>
/// Día de entrenamiento (ver <c>GLOSSARY.md</c>): uno de los días de la semana que el atleta
/// entrena. Es parte del perfil y lo elige él (3–5 días); el número de días de entrenamiento es
/// simplemente cuántos elige.
/// </summary>
/// <remarks>
/// Solo se guarda el día de la semana, sin hora ni fecha: el calendario del mesociclo (#94) lo
/// combina con la fecha de inicio para situar cada sesión en una fecha concreta.
/// </remarks>
public sealed class TrainingWeekday
{
    internal TrainingWeekday(DayOfWeek day)
    {
        Day = day;
    }

    // Requerido por EF Core para materializar la entidad; nunca se usa desde el dominio.
    private TrainingWeekday()
    {
    }

    /// <summary>Día de la semana entrenado.</summary>
    public DayOfWeek Day { get; private set; }

    /// <summary>
    /// Días de la semana por defecto de una frecuencia (3–5), para cuando el atleta todavía no ha
    /// elegido cuáles: 3 días lunes/miércoles/viernes, 4 lunes/martes/jueves/viernes y 5 de lunes a
    /// viernes. Es la única forma de saber «qué día» sin preguntar, y el motor la usa igual que
    /// cualquier otra elección.
    /// </summary>
    public static IReadOnlyList<DayOfWeek> DefaultFor(int trainingDays) => trainingDays switch
    {
        3 => [DayOfWeek.Monday, DayOfWeek.Wednesday, DayOfWeek.Friday],
        4 => [DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Thursday, DayOfWeek.Friday],
        5 => [DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday],
        _ => throw new ArgumentOutOfRangeException(nameof(trainingDays)),
    };
}
