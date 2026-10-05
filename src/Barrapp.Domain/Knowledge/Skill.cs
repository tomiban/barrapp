namespace Barrapp.Domain.Knowledge;

/// <summary>
/// Skill de calistenia: destreza con una escalera de progresión propia, un patrón que entrena y
/// sus rutinas de patrón (ver <c>GLOSSARY.md</c>, término <i>Skill</i>).
/// </summary>
public sealed class Skill
{
    /// <summary>Slug estable y único entre skills.</summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>Nombre para la UI, en español.</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>Patrón que entrena (<see cref="ExerciseGroup.Push"/>, <see cref="ExerciseGroup.Pull"/>, <see cref="ExerciseGroup.Leg"/> o <see cref="ExerciseGroup.Core"/>).</summary>
    public ExerciseGroup Group { get; init; }

    /// <summary>Indica si es un skill apalancado (la carga se ajusta por palanca).</summary>
    public bool Lever { get; init; }

    /// <summary>Escalera de progresión: 4–6 etapas con <c>order</c> consecutivo desde 1.</summary>
    public IReadOnlyList<SkillStage> Stages { get; init; } = [];

    /// <summary>Rutinas de patrón del skill; no puede estar vacía.</summary>
    public IReadOnlyList<PatternRoutine> PatternRoutines { get; init; } = [];
}
