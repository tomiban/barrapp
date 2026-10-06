using Barrapp.Application.Abstractions;
using Barrapp.Application.Common;
using Barrapp.Domain.Common;
using Barrapp.Domain.Planning;
using Microsoft.EntityFrameworkCore;

namespace Barrapp.Application.Features.Plans;

/// <summary>
/// Carga el perfil (con sus máximos), el objetivo y la etapa actual del atleta en ese skill, y
/// delega la generación en el motor de dominio. La regla de programación vive entera en
/// <see cref="PlanGenerator"/>; el handler solo orquesta y proyecta a DTO. Es la excepción
/// pragmática a «las queries proyectan directo a DTO»: el motor necesita el agregado del atleta
/// como entrada.
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

        var progress = await dbContext.AthleteSkillProgresses
            .FirstOrDefaultAsync(
                candidate => candidate.UserId == SingleUser.Id && candidate.SkillId == objective.SkillId,
                cancellationToken);

        // Sin progreso guardado, el motor practica la primera etapa de la escalera.
        var generation = PlanGenerator.Generate(profile, objective, progress?.StageOrder, catalog);

        return generation.IsFailure
            ? Result.Failure<PlanResponse>(generation.Error)
            : PlanMappings.ToResponse(generation.Value, catalog);
    }
}
