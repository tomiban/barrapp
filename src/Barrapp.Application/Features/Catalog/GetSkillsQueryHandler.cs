using Barrapp.Application.Abstractions;
using Barrapp.Domain.Common;
using Barrapp.Domain.Knowledge;

namespace Barrapp.Application.Features.Catalog;

/// <summary>
/// Proyecta los skills en memoria de <see cref="IKnowledgeBase"/> al DTO que consume la app:
/// escalera de progresión y rutinas de patrón, con el patrón y la métrica en minúsculas.
/// </summary>
internal sealed class GetSkillsQueryHandler(IKnowledgeBase knowledgeBase)
    : IQueryHandler<GetSkillsQuery, IReadOnlyList<SkillResponse>>
{
    public Task<Result<IReadOnlyList<SkillResponse>>> Handle(
        GetSkillsQuery request,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<SkillResponse> skills = knowledgeBase.Skills
            .Select(ToResponse)
            .ToList();

        return Task.FromResult(Result.Success(skills));
    }

    private static SkillResponse ToResponse(Skill skill) => new(
        skill.Id,
        skill.Name,
        CatalogMappings.ToCode(skill.Group),
        skill.Lever,
        skill.Stages.Select(ToResponse).ToList(),
        skill.PatternRoutines.Select(ToResponse).ToList());

    private static SkillStageResponse ToResponse(SkillStage stage) => new(
        stage.Order,
        stage.Name,
        stage.ExerciseId,
        new StageCriterionResponse(
            CatalogMappings.ToCode(stage.Criterion.Metric),
            stage.Criterion.Target,
            stage.Criterion.Sets),
        stage.Notes);

    private static PatternRoutineResponse ToResponse(PatternRoutine routine) => new(
        routine.Id,
        routine.Name,
        routine.Intensity,
        routine.Equipment,
        routine.Items.Select(CatalogMappings.ToResponse).ToList());
}
