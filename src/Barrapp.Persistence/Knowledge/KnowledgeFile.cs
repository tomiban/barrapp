using Barrapp.Domain.Knowledge;

namespace Barrapp.Persistence.Knowledge;

/// <summary>
/// Sobre común de los tres JSON: todos declaran <c>schemaVersion</c>.
/// </summary>
internal abstract class KnowledgeFile
{
    public int SchemaVersion { get; init; }
}

/// <summary>Estructura de <c>knowledge/exercises.json</c>.</summary>
internal sealed class ExercisesFile : KnowledgeFile
{
    public IReadOnlyList<Exercise> Exercises { get; init; } = [];
}

/// <summary>Estructura de <c>knowledge/skills.json</c>.</summary>
internal sealed class SkillsFile : KnowledgeFile
{
    public IReadOnlyList<Skill> Skills { get; init; } = [];
}

/// <summary>Estructura de <c>knowledge/routines.json</c>.</summary>
internal sealed class RoutinesFile : KnowledgeFile
{
    public IReadOnlyList<RoutineProgram> Programs { get; init; } = [];
}
