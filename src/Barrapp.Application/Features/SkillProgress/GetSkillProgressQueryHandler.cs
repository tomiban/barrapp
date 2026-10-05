using Barrapp.Application.Abstractions;
using Barrapp.Application.Common;
using Barrapp.Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace Barrapp.Application.Features.SkillProgress;

/// <summary>
/// Lee la etapa actual de todos los skills del catálogo. Las filas guardadas se proyectan directo
/// a DTO sobre <see cref="IApplicationDbContext"/>; los skills sin fila parten de la etapa 1.
/// </summary>
internal sealed class GetSkillProgressQueryHandler(
    IApplicationDbContext dbContext,
    IKnowledgeBase knowledgeBase)
    : IQueryHandler<GetSkillProgressQuery, IReadOnlyList<SkillProgressResponse>>
{
    public async Task<Result<IReadOnlyList<SkillProgressResponse>>> Handle(
        GetSkillProgressQuery request,
        CancellationToken cancellationToken)
    {
        var savedStageBySkill = await dbContext.AthleteSkillProgresses
            .Where(progress => progress.UserId == SingleUser.Id)
            .ToDictionaryAsync(
                progress => progress.SkillId,
                progress => progress.StageOrder,
                StringComparer.Ordinal,
                cancellationToken);

        IReadOnlyList<SkillProgressResponse> progress = knowledgeBase.Skills
            .Select(skill => new SkillProgressResponse(
                skill.Id,
                savedStageBySkill.TryGetValue(skill.Id, out var stageOrder) ? stageOrder : 1))
            .ToList();

        return Result.Success(progress);
    }
}
