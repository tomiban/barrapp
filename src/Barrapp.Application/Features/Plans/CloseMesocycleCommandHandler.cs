using Barrapp.Application.Abstractions;
using Barrapp.Application.Common;
using Barrapp.Application.Features.AthleteProfiles;
using Barrapp.Domain.Athlete;
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
/// <remarks>
/// Sin mesociclo activo, el cierre <b>sintetiza</b> el mesociclo que se cierra desde el perfil y el
/// objetivo (FIX-1): la app nunca persiste el plan (solo lo genera on-read con <c>GET /plan</c>),
/// así que sin esta síntesis el cierre sería inalcanzable de extremo a extremo. Hidrata perfil +
/// objetivo + progreso por el mismo camino que <c>GET /plan</c>/<c>POST /plan</c>, corre el motor
/// (<see cref="PlanGenerator.Generate"/>), crea el mesociclo <see cref="MesocycleStatus.Active"/>
/// con arranque un mes atrás (para que la ventana de registros cubra el último mes), lo persiste,
/// lo cierra y sigue el flujo común. Si hay un mesociclo activo, se cierra ese (comportamiento de
/// siempre); la síntesis solo ocurre cuando no lo hay y el plan se puede generar (si falta el
/// perfil o el objetivo, se devuelve <c>Not Found</c>).
/// </remarks>
internal sealed class CloseMesocycleCommandHandler(
    IApplicationDbContext dbContext,
    IKnowledgeBase catalog,
    IMesocycleRepository repository,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
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
        Result<Mesocycle> candidate = active is not null
            ? Result.Success(active)
            : await SynthesizeCandidateAsync(profile, cancellationToken);
        if (candidate.IsFailure)
        {
            return Result.Failure<CloseMesocycleResponse>(candidate.Error);
        }

        var mesocycle = candidate.Value;
        var closedAtUtc = timeProvider.GetUtcNow();
        var closing = mesocycle.Close(closedAtUtc);
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
                && (log.MesocycleId == mesocycle.Id || log.MesocycleId == null))
            .ToListAsync(cancellationToken);

        var logs = candidates
            .Where(log => log.MesocycleId == mesocycle.Id
                || (log.RecordedAtUtc >= mesocycle.StartedAtUtc && log.RecordedAtUtc <= closedAtUtc))
            .ToList();

        var adjustments = MaximumAdjustment.Compute(profile, logs, catalog);
        var applying = profile.UpdateMaximums(adjustments);
        if (applying.IsFailure)
        {
            return Result.Failure<CloseMesocycleResponse>(applying.Error);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(new CloseMesocycleResponse(
            mesocycle.Id,
            mesocycle.SkillId,
            catalog.FindSkill(mesocycle.SkillId)?.Name ?? mesocycle.SkillId,
            mesocycle.TrainingDays,
            mesocycle.StartedAtUtc,
            mesocycle.ClosedAtUtc!.Value,
            MaximumResponses.From(profile.Maximums)));
    }

    /// <summary>
    /// Sintetiza el mesociclo que el cierre va a cerrar cuando no hay ninguno activo (FIX-1):
    /// genera el plan con el perfil y el objetivo guardados (misma hidratación que la lectura) y lo
    /// persiste como activo con arranque un mes atrás, de modo que la ventana de registros cubra el
    /// último mes. Falla con <c>Not Found</c> si falta el objetivo y con el error del motor si el
    /// plan no se puede generar; no persiste nada en ese caso.
    /// </summary>
    private async Task<Result<Mesocycle>> SynthesizeCandidateAsync(
        AthleteProfile profile,
        CancellationToken cancellationToken)
    {
        var objective = await dbContext.Objectives
            .FirstOrDefaultAsync(candidate => candidate.UserId == SingleUser.Id, cancellationToken);
        if (objective is null)
        {
            return Result.Failure<Mesocycle>(DomainErrors.Objective.NotFound);
        }

        var progress = await dbContext.AthleteSkillProgresses
            .FirstOrDefaultAsync(
                candidate => candidate.UserId == SingleUser.Id && candidate.SkillId == objective.SkillId,
                cancellationToken);

        // El mesociclo que se sintetiza aquí está ya vencido: arranca hace cuatro semanas (#94), que es
        // lo que permite que sus sesiones caigan en el pasado y el cierre ajusta los máximos con lo registrado.
        var generation = PlanGenerator.Generate(
            profile,
            objective,
            progress?.StageOrder,
            DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime.AddDays(-28)),
            catalog);
        if (generation.IsFailure)
        {
            return Result.Failure<Mesocycle>(generation.Error);
        }

        var creation = Mesocycle.Create(
            SingleUser.Id,
            generation.Value,
            timeProvider.GetUtcNow().AddDays(-28));
        if (creation.IsFailure)
        {
            return Result.Failure<Mesocycle>(creation.Error);
        }

        repository.Add(creation.Value);
        return Result.Success(creation.Value);
    }
}
