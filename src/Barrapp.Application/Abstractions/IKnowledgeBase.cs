using Barrapp.Domain.Knowledge;

namespace Barrapp.Application.Abstractions;

/// <summary>
/// Puerto de lectura de la base de conocimiento: catálogo de ejercicios, skills con su escalera
/// y programas generales, ya validado y en memoria. Lo consumen el motor de generación, la sesión
/// suelta y las queries de catálogo.
/// </summary>
public interface IKnowledgeBase : IGenerationCatalog
{
    /// <summary>Todos los ejercicios del catálogo.</summary>
    IReadOnlyList<Exercise> Exercises { get; }

    /// <summary>Ejercicios del grupo indicado.</summary>
    IReadOnlyList<Exercise> ExercisesByGroup(ExerciseGroup group);

    /// <summary>Todos los skills.</summary>
    IReadOnlyList<Skill> Skills { get; }

    /// <summary>Todos los programas generales.</summary>
    IReadOnlyList<RoutineProgram> Programs { get; }
}
