using Barrapp.Application.Abstractions;
using Barrapp.Application.Common;
using Barrapp.Application.Features.AthleteProfiles;
using Barrapp.Domain.Common;
using Barrapp.Domain.Planning;
using Microsoft.EntityFrameworkCore;

namespace Barrapp.Application.Features.Plans;

/// <summary>
/// Cierra el mesociclo activo, ajusta los máximos y confirma todo en una sola transacción
/// (ticket #24, D7). La regla de ajuste vive en <see cref="MaximumAdjustment"/> y es pura; este
/// caso de uso le añade el efecto de cierre: selecciona los registros que pertenecen al mesociclo
/// —los etiquetados con su id y los registrados durante su vigencia—, aplica los máximos nuevos
/// al perfil y persiste el estado cerrado.
/// </summary>
internal sealed class CloseMesocycleCommandHandler(
    IApplicationDbContext dbContext,
    IKnowledgeBase catalog,
    IMesocycleRepository repository,
    IUnitOfWork unitOfWork)
    : ICommandHandler<CloseMesocycleCommand, CloseMesocycleResponse>
{
    public async Task<Result<CloseMesocycleResponse>> Handle(
        CloseMesocycleCommand request,
        CancellationToken cancellationToken)
    {
        var profile = await dbContext.AthleteProfiles
            .Include(candidate => candidate.Maximums)
            .FirstOrDefaultAsync(candidate => candidate.UserId == SingleUser.Id, cancellationToken);

        if (profile is null)
        {
            return Result.Failure<CloseMesocycleResponse>(DomainErrors.AthleteProfile.NotFound);
        }

        var active = await repository.GetActiveByUserIdAsync(SingleUser.Id, cancellationToken);
        if (active is null)
        {
            return Result.Failure<CloseMesocycleResponse>(DomainErrors.Mesocycle.NotActive);
        }

        var closedAtUtc = DateTimeOffset.UtcNow;
        var closing = active.Close(closedAtUtc);
        if (closing.IsFailure)
        {
            return Result.Failure<CloseMesocycleResponse>(closing.Error);
        }

        // Los registros del mesociclo: los etiquetados con su id y, para cubrir los clientes que
        // no lo etiquetan (#26), los registrados durante la vigencia del mesociclo. La ventana se
        // filtra en memoria: SQLite no compara DateTimeOffset en SQL (misma convención que el
        // historial, que ordena en memoria).
        var candidates = await dbContext.SessionLogs
            .AsNoTracking()
            .Where(log => log.UserId == SingleUser.Id
                && (log.MesocycleId == active.Id || log.MesocycleId == null))
            .ToListAsync(cancellationToken);

        var logs = candidates
            .Where(log => log.MesocycleId == active.Id
                || (log.RecordedAtUtc >= active.StartedAtUtc && log.RecordedAtUtc <= closedAtUtc))
            .ToList();

        var adjustments = MaximumAdjustment.Compute(profile, logs, catalog);
        var applying = profile.UpdateMaximums(adjustments);
        if (applying.IsFailure)
        {
            return Result.Failure<CloseMesocycleResponse>(applying.Error);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(new CloseMesocycleResponse(
            active.Id,
            active.SkillId,
            catalog.FindSkill(active.SkillId)?.Name ?? active.SkillId,
            active.TrainingDays,
            active.StartedAtUtc,
            active.ClosedAtUtc!.Value,
            MaximumResponses.From(profile.Maximums)));
    }
}