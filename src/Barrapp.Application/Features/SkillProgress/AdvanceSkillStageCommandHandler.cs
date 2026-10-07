using Barrapp.Application.Abstractions;
using Barrapp.Application.Common;
using Barrapp.Domain.Common;
using Barrapp.Domain.Planning;
using Barrapp.Domain.SkillProgress;
using Microsoft.EntityFrameworkCore;

namespace Barrapp.Application.Features.SkillProgress;

/// <summary>
/// Carga la progresión del skill y los registros de sesión del atleta, delega la evaluación del
/// avance en el motor de dominio (<see cref="SkillStageAdvancer"/>) y persiste la etapa nueva solo
/// cuando el motor decide avanzar. No hace falta perfil ni objetivo guardados: la regla mira los
/// registros del ejercicio de la etapa actual contra la escalera del skill.
/// </summary>
/// <remarks>
/// El avance solo consume los registros del <b>mesociclo en curso</b> (FIX-3): los del mesociclo
/// anterior —ya cerrado— no deben disparar una subida de etapa del mes nuevo. La frontera temporal
/// se resuelve así: si hay un mesociclo <see cref="MesocycleStatus.Active"/>, arranca en su
/// <see cref="Mesocycle.StartedAtUtc"/>; si no (la app solo genera el plan on-read con
/// <c>GET /plan</c>), arranca en el <b>mes en curso</b>, es decir, el mayor entre hace cuatro
/// semanas y el cierre del último mesociclo cerrado. El filtro se hace en memoria: SQLite no
/// compara <see cref="DateTimeOffset"/> en SQL, la misma convención que el cierre y el historial.
/// </remarks>
internal sealed class AdvanceSkillStageCommandHandler(
    IApplicationDbContext dbContext,
    IKnowledgeBase knowledgeBase,
    IAthleteSkillProgressRepository repository,
    IMesocycleRepository mesocycles,
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

        var windowStart = await ResolveWindowStartAsync(cancellationToken);

        // Los registros viajan con sus series (objetos valor owned), así que no hace falta include.
        var logs = await dbContext.SessionLogs
            .AsNoTracking()
            .Include(log => log.Items)
            .Where(log => log.UserId == SingleUser.Id)
            .ToListAsync(cancellationToken);

        // Solo cuentan los registros posteriores al arranque del mesociclo en curso (FIX-3).
        var currentLogs = logs
            .Where(log => log.RecordedAtUtc >= windowStart)
            .ToList();

        var evaluation = SkillStageAdvancer.Evaluate(skill, currentStageOrder, currentLogs);
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

    /// <summary>
    /// Frontera temporal de los registros que cuenta el avance (FIX-3): el arranque del mesociclo
    /// activo o, sin él, el del mes en curso —hace cuatro semanas, o el cierre del último
    /// mesociclo cerrado si es más reciente—. Así las sesiones de un mesociclo ya cerrado no
    /// disparan el avance del mes siguiente.
    /// </summary>
    private async Task<DateTimeOffset> ResolveWindowStartAsync(CancellationToken cancellationToken)
    {
        var active = await mesocycles.GetActiveByUserIdAsync(SingleUser.Id, cancellationToken);
        if (active is not null)
        {
            return active.StartedAtUtc;
        }

        var synthesizedStart = DateTimeOffset.UtcNow.AddDays(-28);

        // SQLite no ordena por DateTimeOffset en SQL; se resuelve el cierre más reciente en memoria
        // (la misma convención que el historial).
        var closed = await dbContext.Mesocycles
            .AsNoTracking()
            .Where(candidate => candidate.UserId == SingleUser.Id
                && candidate.Status == MesocycleStatus.Closed)
            .ToListAsync(cancellationToken);

        var lastClosedAt = closed.Count == 0
            ? (DateTimeOffset?)null
            : closed.Max(candidate => candidate.ClosedAtUtc);

        return lastClosedAt is not null && lastClosedAt > synthesizedStart
            ? lastClosedAt.Value
            : synthesizedStart;
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
