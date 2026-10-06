using Barrapp.Application.Abstractions;
using Barrapp.Application.Common;
using Barrapp.Domain.Common;
using Barrapp.Domain.Planning;
using Microsoft.EntityFrameworkCore;

namespace Barrapp.Application.Features.Plans;

/// <summary>
/// Devuelve el mesociclo en curso del atleta. Desde #27 (D7) el plan se persiste al generarse
/// (<c>POST /plan</c>): si hay un mesociclo <see cref="MesocycleStatus.Active"/>, se sirve el
/// snapshot guardado tal cual; si no, se genera on-read con el motor (comportamiento anterior,
/// sin guardar). En ambos casos la regla de programación vive en <see cref="PlanGenerator"/>;
/// la excepción pragmática a «las queries proyectan directo a DTO» sigue intacta (ver ADR-0012).
/// </summary>
internal sealed class GetPlanQueryHandler(IApplicationDbContext dbContext, IKnowledgeBase catalog)
    : IQueryHandler<GetPlanQuery, PlanResponse>
{
    public async Task<Result<PlanResponse>> Handle(
        GetPlanQuery request,
        CancellationToken cancellationToken)
    {
        var profile = await dbContext.AthleteProfiles
            .Include(candidate => candidate.Maximums)
            .FirstOrDefaultAsync(candidate => candidate.UserId == SingleUser.Id, cancellationToken);

        if (profile is null)
        {
            return Result.Failure<PlanResponse>(DomainErrors.AthleteProfile.NotFound);
        }

        var objective = await dbContext.Objectives
            .FirstOrDefaultAsync(candidate => candidate.UserId == SingleUser.Id, cancellationToken);

        if (objective is null)
        {
            return Result.Failure<PlanResponse>(DomainErrors.Objective.NotFound);
        }

        // Plan ya persistido al generarse (D7): se sirve el mesociclo activo sin regenerar.
        var active = await dbContext.Mesocycles
            .AsNoTracking()
            .FirstOrDefaultAsync(
                candidate => candidate.UserId == SingleUser.Id
                    && candidate.Status == MesocycleStatus.Active,
                cancellationToken);

        if (active is not null)
        {
            return PlanMappings.ToResponse(active.Snapshot.ToPlan(), catalog);
        }

        var progress = await dbContext.AthleteSkillProgresses
            .FirstOrDefaultAsync(
                candidate => candidate.UserId == SingleUser.Id && candidate.SkillId == objective.SkillId,
                cancellationToken);

        // Sin mesociclo persistido ni progreso guardado, el motor practica la primera etapa.
        var generation = PlanGenerator.Generate(profile, objective, progress?.StageOrder, catalog);

        return generation.IsFailure
            ? Result.Failure<PlanResponse>(generation.Error)
            : PlanMappings.ToResponse(generation.Value, catalog);
    }
}
