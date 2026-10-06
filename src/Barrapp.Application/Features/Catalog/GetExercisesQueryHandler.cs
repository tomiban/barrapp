using Barrapp.Application.Abstractions;
using Barrapp.Domain.Common;
using Barrapp.Domain.Knowledge;

namespace Barrapp.Application.Features.Catalog;

/// <summary>
/// Proyecta el catálogo en memoria de <see cref="IKnowledgeBase"/> al DTO agrupado por patrón.
/// Los grupos se emiten en el orden canónico del dominio; los vacíos se omiten.
/// </summary>
internal sealed class GetExercisesQueryHandler(IKnowledgeBase knowledgeBase)
    : IQueryHandler<GetExercisesQuery, ExerciseCatalogResponse>
{
    /// <summary>Orden canónico de los patrones en el catálogo.</summary>
    private static readonly ExerciseGroup[] GroupOrder =
    [
        ExerciseGroup.Push,
        ExerciseGroup.Pull,
        ExerciseGroup.Leg,
        ExerciseGroup.Core,
        ExerciseGroup.Cardio,
    ];

    public Task<Result<ExerciseCatalogResponse>> Handle(
        GetExercisesQuery request,
        CancellationToken cancellationToken)
    {
        if (knowledgeBase.Exercises.Count == 0)
        {
            return Task.FromResult(
                Result.Failure<ExerciseCatalogResponse>(DomainErrors.Knowledge.EmptyCatalog));
        }

        var groups = GroupOrder
            .Select(group => new ExerciseGroupResponse(
                CatalogMappings.ToCode(group),
                knowledgeBase.ExercisesByGroup(group).Select(ToResponse).ToList()))
            .Where(group => group.Exercises.Count > 0)
            .ToList();

        return Task.FromResult(Result.Success(new ExerciseCatalogResponse(groups)));
    }

    private static ExerciseResponse ToResponse(Exercise exercise) => new(
        exercise.Id,
        exercise.Name,
        CatalogMappings.ToCode(exercise.Metric),
        exercise.TracksMaximum,
        exercise.RegressionId,
        exercise.SkillId);
}
