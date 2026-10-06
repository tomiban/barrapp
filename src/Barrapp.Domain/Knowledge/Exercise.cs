namespace Barrapp.Domain.Knowledge;

/// <summary>
/// Ejercicio del catálogo: un identificador estable (slug en inglés) y un nombre para la UI en
/// español, con su naturaleza (<see cref="ExerciseKind"/>), grupo, métrica y referencias a su
/// regresión y a su skill de apoyo.
/// </summary>
/// <remarks>
/// Datos cargados de <c>knowledge/exercises.json</c> y validados en conjunto por
/// <see cref="KnowledgeBase.Create"/>. <see cref="Group"/> es <c>null</c> solo en los ejercicios
/// de tipo <see cref="ExerciseKind.Skill"/>.
/// </remarks>
public sealed class Exercise
{
    /// <summary>Slug estable y único entre ejercicios.</summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>Nombre para la UI, en español.</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>Acondicionamiento general o movimiento de skill.</summary>
    public ExerciseKind Kind { get; init; }

    /// <summary>Grupo del ejercicio; <c>null</c> cuando <see cref="Kind"/> es <see cref="ExerciseKind.Skill"/>.</summary>
    public ExerciseGroup? Group { get; init; }

    /// <summary>Unidad del ejercicio: repeticiones o segundos.</summary>
    public Metric Metric { get; init; }

    /// <summary>Indica si el atleta registra su máximo en este ejercicio.</summary>
    public bool TracksMaximum { get; init; }

    /// <summary>Ejercicio de regresión cuando el máximo es 0; opcional.</summary>
    public string? RegressionId { get; init; }

    /// <summary>Skill al que apoya el ejercicio; opcional en acondicionamiento y obligatorio en skill.</summary>
    public string? SkillId { get; init; }
}
