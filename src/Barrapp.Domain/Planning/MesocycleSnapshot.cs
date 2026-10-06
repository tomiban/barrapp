using Barrapp.Domain.Knowledge;

namespace Barrapp.Domain.Planning;

/// <summary>
/// Snapshot puro del <see cref="Plan"/> que se guarda al perseguir el mesociclo (#27, D7): el
/// mismo contenido —skill, frecuencia, etapa actual y las cuatro semanas con sus sesiones y
/// filas— en una forma serializable que no depende de los constructores internos del motor. Es un
/// objeto valor inmutable que solo viaja entre el motor (<see cref="FromPlan"/>) y la proyección
/// (<see cref="ToPlan"/>); la serialización a columna JSON la decide la capa de persistencia.
/// </summary>
public sealed class MesocycleSnapshot
{
    /// <summary>Slug del skill objetivo del mesociclo.</summary>
    public string SkillId { get; init; } = string.Empty;

    /// <summary>Días de entrenamiento por semana.</summary>
    public int TrainingDays { get; init; }

    /// <summary>Fecha en la que arranca el mesociclo (#94).</summary>
    public DateOnly StartDate { get; init; }

    /// <summary>Etapa actual del skill objetivo con la que se generó el plan.</summary>
    public SnapshotStage CurrentStage { get; init; } = new();

    /// <summary>Semanas del mesociclo, en orden.</summary>
    public IReadOnlyList<SnapshotMicrocycle> Microcycles { get; init; } = [];

    /// <summary>Copia el plan del motor al snapshot, sin pérdida de contenido.</summary>
    public static MesocycleSnapshot FromPlan(Plan plan) => new()
    {
        SkillId = plan.SkillId,
        TrainingDays = plan.TrainingDays,
        StartDate = plan.StartDate,
        CurrentStage = SnapshotStage.From(plan.CurrentStage),
        Microcycles = plan.Microcycles
            .Select(microcycle => new SnapshotMicrocycle
            {
                Number = microcycle.Number,
                Sessions = microcycle.Sessions
                    .Select(session => new SnapshotSession
                    {
                        Day = session.Day,
                        Weekday = session.Weekday,
                        Date = session.Date,
                        Items = session.Items.Select(SnapshotSessionItem.From).ToList(),
                    })
                    .ToList(),
            })
            .ToList(),
    };

    /// <summary>Reconstruye el <see cref="Plan"/> del motor a partir del snapshot.</summary>
    public Plan ToPlan() => new(
        SkillId,
        TrainingDays,
        StartDate,
        CurrentStage.ToStage(),
        Microcycles
            .Select(microcycle => new Microcycle(
                microcycle.Number,
                microcycle.Sessions
                    .Select(session => new Session(
                        session.Day,
                        session.Weekday,
                        session.Date,
                        session.Items.Select(item => item.ToItem()).ToList()))
                    .ToList()))
            .ToList());
}

/// <summary>Etapa actual del skill en el snapshot (copia serializable de <see cref="SkillStage"/>).</summary>
public sealed class SnapshotStage
{
    /// <summary>Posición en la escalera; consecutiva desde 1.</summary>
    public int Order { get; init; }

    /// <summary>Nombre de la etapa para la UI, en español.</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>Ejercicio que representa la etapa.</summary>
    public string ExerciseId { get; init; } = string.Empty;

    /// <summary>Métrica del criterio de avance (repeticiones o segundos).</summary>
    public Metric Metric { get; init; }

    /// <summary>Objetivo del criterio de avance.</summary>
    public int Target { get; init; }

    /// <summary>Series del criterio de avance.</summary>
    public int Sets { get; init; }

    /// <summary>Notas para la UI, en español.</summary>
    public string Notes { get; init; } = string.Empty;

    public static SnapshotStage From(SkillStage stage) => new()
    {
        Order = stage.Order,
        Name = stage.Name,
        ExerciseId = stage.ExerciseId,
        Metric = stage.Criterion.Metric,
        Target = stage.Criterion.Target,
        Sets = stage.Criterion.Sets,
        Notes = stage.Notes,
    };

    public SkillStage ToStage() => new()
    {
        Order = Order,
        Name = Name,
        ExerciseId = ExerciseId,
        Criterion = new StageCriterion { Metric = Metric, Target = Target, Sets = Sets },
        Notes = Notes,
    };
}

/// <summary>Semana del mesociclo en el snapshot.</summary>
public sealed class SnapshotMicrocycle
{
    /// <summary>Número de la semana dentro del mesociclo (1–4).</summary>
    public int Number { get; init; }

    /// <summary>Sesiones de la semana, en orden.</summary>
    public IReadOnlyList<SnapshotSession> Sessions { get; init; } = [];
}

/// <summary>Sesión de una semana en el snapshot.</summary>
public sealed class SnapshotSession
{
    /// <summary>Día de la sesión dentro del microciclo (empieza en 1).</summary>
    public int Day { get; init; }

    /// <summary>Día de la semana en el que se entrena (#94).</summary>
    public DayOfWeek? Weekday { get; init; }

    /// <summary>Fecha en la que se entrena la sesión (#94).</summary>
    public DateOnly? Date { get; init; }

    /// <summary>Filas de la sesión, en orden de ejecución.</summary>
    public IReadOnlyList<SnapshotSessionItem> Items { get; init; } = [];
}

/// <summary>Fila de una sesión en el snapshot (copia serializable de <see cref="SessionItem"/>).</summary>
public sealed class SnapshotSessionItem
{
    /// <summary>Slug del ejercicio en el catálogo.</summary>
    public string ExerciseId { get; init; } = string.Empty;

    /// <summary>Papel del ejercicio dentro de la sesión.</summary>
    public SessionItemRole Role { get; init; }

    /// <summary>Patrón de fuerza; <c>null</c> en el bloque de skill y en el core.</summary>
    public ExerciseGroup? Pattern { get; init; }

    /// <summary>Número de series.</summary>
    public int Sets { get; init; }

    /// <summary>Repeticiones mínimas del rango; <c>null</c> cuando se mide en segundos.</summary>
    public int? RepsMin { get; init; }

    /// <summary>Repeticiones máximas del rango; <c>null</c> cuando se mide en segundos.</summary>
    public int? RepsMax { get; init; }

    /// <summary>Segundos mantenidos mínimos del rango; <c>null</c> cuando se mide en repeticiones.</summary>
    public int? HoldSecondsMin { get; init; }

    /// <summary>Segundos mantenidos máximos del rango; <c>null</c> cuando se mide en repeticiones.</summary>
    public int? HoldSecondsMax { get; init; }

    /// <summary>Nota de la fila para la UI; <c>null</c> cuando no hay nada que explicar.</summary>
    public string? Note { get; init; }

    public static SnapshotSessionItem From(SessionItem item) => new()
    {
        ExerciseId = item.ExerciseId,
        Role = item.Role,
        Pattern = item.Pattern,
        Sets = item.Sets,
        RepsMin = item.RepsMin,
        RepsMax = item.RepsMax,
        HoldSecondsMin = item.HoldSecondsMin,
        HoldSecondsMax = item.HoldSecondsMax,
        Note = item.Note,
    };

    public SessionItem ToItem() => new(
        ExerciseId,
        Role,
        Pattern,
        Sets,
        RepsMin,
        RepsMax,
        HoldSecondsMin,
        HoldSecondsMax,
        Note);
}
