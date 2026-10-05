using Barrapp.Application.Abstractions;
using Barrapp.Domain.Knowledge;

namespace Barrapp.Persistence.Knowledge;

/// <summary>
/// Adaptador del puerto <see cref="IKnowledgeBase"/> sobre el catálogo de dominio ya validado.
/// La búsqueda y la agrupación viven en <see cref="KnowledgeBase"/> para no duplicar lógica.
/// </summary>
internal sealed class KnowledgeBaseCatalog(KnowledgeBase knowledgeBase) : IKnowledgeBase
{
    public IReadOnlyList<Exercise> Exercises => knowledgeBase.Exercises;

    public IReadOnlyList<Exercise> ExercisesByGroup(ExerciseGroup group) =>
        knowledgeBase.ExercisesByGroup(group);

    public Exercise? FindExercise(string exerciseId) => knowledgeBase.FindExercise(exerciseId);

    public IReadOnlyList<Skill> Skills => knowledgeBase.Skills;

    public Skill? FindSkill(string skillId) => knowledgeBase.FindSkill(skillId);

    public IReadOnlyList<RoutineProgram> Programs => knowledgeBase.Programs;
}
