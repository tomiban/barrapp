using Barrapp.Application.Abstractions;
using Barrapp.Application.Common;
using Barrapp.Domain.Common;
using Barrapp.Domain.SkillProgress;
using Microsoft.EntityFrameworkCore;

namespace Barrapp.Application.Features.SkillProgress;

/// <summary>
/// Carga la progresión del skill y los registros de sesión del atleta, delega la evaluación del
/// avance en el motor de dominio (<see cref="SkillStageAdvancer"/>) y persiste la etapa nueva solo
/// cuando el motor decide avanzar. No hace falta perfil ni objetivo guardados: la regla mira los
/// registros del ejercicio de la etapa actual contra la escalera del skill.
/// </summary>
internal sealed class AdvanceSkillStageCommandHandler(
    IApplicationDbContext dbContext,
    IKnowledgeBase knowledgeBase,
    IAthleteSkillProgressRepository repository,
    IUnitOfWork unitOfWork)
    : ICommandHandler<AdvanceSkillStageCommand, SkillStageAdvanceResponse>
{
    public async Task<Result<SkillStageAdvanceResponse>> Handle(
        AdvanceSkillStageCommand request,
        CancellationToken cancellationToken)
    {
        var skill = knowledgeBase.FindSkill(request.SkillId);
        if (skill is null)
        {
            return Result.Failure<SkillStageAdvanceResponse>(DomainErrors.SkillProgress.UnknownSkill);
        }

        var progress = await repository.GetAsync(SingleUser.Id, request.SkillId, cancellationToken);
        var currentStageOrder = progress?.StageOrder ?? AthleteSkillProgress.InitialStageOrder;

        // Los registros viajan con sus series (objetos valor owned), así que no hace falta include.
        var logs = await dbContext.SessionLogs
            .AsNoTracking()
            .Where(log => log.UserId == SingleUser.Id)
            .ToListAsync(cancellationToken);

        var evaluation = SkillStageAdvancer.Evaluate(skill, currentStageOrder, logs);
        if (evaluation.IsFailure)
        {
            return Result.Failure<SkillStageAdvanceResponse>(evaluation.Error);
        }

        if (evaluation.Value.Advanced)
        {
            var persist = progress is null
                ? PersistNewProgress(request.SkillId, evaluation.Value.StageOrder)
                : progress.Update(evaluation.Value.StageOrder, knowledgeBase);

            if (persist.IsFailure)
            {
                return Result.Failure<SkillStageAdvanceResponse>(persist.Error);
            }

            await unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return new SkillStageAdvanceResponse(
            request.SkillId,
            evaluation.Value.StageOrder,
            evaluation.Value.Advanced);
    }

    private Result PersistNewProgress(string skillId, int stageOrder)
    {
        var creation = AthleteSkillProgress.Create(SingleUser.Id, skillId, stageOrder, knowledgeBase);
        if (creation.IsFailure)
        {
            return creation;
        }

        repository.Add(creation.Value);
        return Result.Success();
    }
}