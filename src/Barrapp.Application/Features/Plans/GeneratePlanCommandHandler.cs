using Barrapp.Application.Abstractions;
using Barrapp.Application.Common;
using Barrapp.Domain.Common;
using Barrapp.Domain.Planning;
using Microsoft.EntityFrameworkCore;

namespace Barrapp.Application.Features.Plans;

/// <summary>
/// Genera el mesociclo (perfil + objetivo + etapa actual → motor) y lo persiste como snapshot:
/// reemplaza el mesociclo activo anterior para que <c>GET /plan</c> sirva el plan guardado. La
/// regla de programación vive en <see cref="PlanGenerator"/>; este caso de uso le añade el efecto
/// de guardar la generación, que es lo que arranca el historial (#27, D7).
/// </summary>
internal sealed class GeneratePlanCommandHandler(
    IApplicationDbContext dbContext,
    IKnowledgeBase catalog,
    IMesocycleRepository repository,
    IUnitOfWork unitOfWork)
    : ICommandHandler<GeneratePlanCommand, PlanResponse>
{
    public async Task<Result<PlanResponse>> Handle(
        GeneratePlanCommand request,
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
        if (generation.IsFailure)
        {
            return Result.Failure<PlanResponse>(generation.Error);
        }

        // Regenerar el plan reemplaza el mesociclo en curso: solo puede haber un activo.
        var active = await repository.GetActiveByUserIdAsync(SingleUser.Id, cancellationToken);
        if (active is not null)
        {
            repository.Remove(active);
        }

        var mesocycle = Mesocycle.Create(SingleUser.Id, generation.Value, DateTimeOffset.UtcNow);
        if (mesocycle.IsFailure)
        {
            return Result.Failure<PlanResponse>(mesocycle.Error);
        }

        repository.Add(mesocycle.Value);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(PlanMappings.ToResponse(generation.Value, catalog));
    }
}