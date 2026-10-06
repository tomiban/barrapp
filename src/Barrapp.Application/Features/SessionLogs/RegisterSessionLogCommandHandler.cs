using Barrapp.Application.Abstractions;
using Barrapp.Application.Common;
using Barrapp.Domain.Common;
using Barrapp.Domain.Knowledge;
using Barrapp.Domain.Sessions;

namespace Barrapp.Application.Features.SessionLogs;

/// <summary>
/// Registra lo ejecutado en un ejercicio de una sesión, serie a serie (spec 0001, US-34). Resuelve
/// o crea la sesión por su <b>clave de sesión determinista</b> (ADR-0014) y guarda el ítem con su
/// foto: el nombre y la unidad los deriva el servidor del catálogo y el resto de la foto viene del
/// plan que el atleta tenía delante, de modo que el historial sobreviva a los cambios de la base de
/// conocimiento.
/// </summary>
/// <remarks>
/// Idempotencia de la outbox offline (ADR-0003): si el alta trae
/// <see cref="RegisterSessionLogCommand.ClientId"/> y ya hay un ítem con ese id —un envío cuya
/// respuesta se perdió y se reintenta—, no se inserta otro: se actualizan sus series y su foto en su
/// sitio (última escritura gana, se conserva la identidad del ítem y de la sesión) y se devuelve esa
/// misma sesión. El índice único filtrado de <c>ClientId</c> es el respaldo de la invariante.
/// </remarks>
internal sealed class RegisterSessionLogCommandHandler(
    ISessionLogRepository repository,
    IKnowledgeBase knowledgeBase,
    IUnitOfWork unitOfWork)
    : ICommandHandler<RegisterSessionLogCommand, SessionLogResponse>
{
    public async Task<Result<SessionLogResponse>> Handle(
        RegisterSessionLogCommand request,
        CancellationToken cancellationToken)
    {
        var key = ToKey(request.Session);
        if (key is null)
        {
            return Result.Failure<SessionLogResponse>(DomainErrors.SessionLog.MesocycleKeyRequired);
        }

        // Idempotencia offline: el reintento del mismo clientId actualiza el ítem original, en la
        // sesión en la que se registró aunque la cabecera haya cambiado de clave.
        if (request.ClientId is { } clientId)
        {
            var alreadyRegistered = await repository.GetItemByClientIdAsync(clientId, cancellationToken);
            if (alreadyRegistered is not null)
            {
                var replaySession = await repository.GetByIdAsync(alreadyRegistered.SessionLogId, cancellationToken);
                if (replaySession is not null)
                {
                    return await UpsertAsync(replaySession, request, cancellationToken);
                }
            }
        }

        var existing = await repository.GetByKeyAsync(key, cancellationToken);
        if (existing is not null)
        {
            return await UpsertAsync(existing, request, cancellationToken);
        }

        var creation = SessionLog.Create(
            SingleUser.Id,
            request.Session.Kind,
            request.Session.SessionDate,
            request.Session.MesocycleId,
            request.Session.MicrocycleNumber,
            request.Session.SessionDay,
            DateTimeOffset.UtcNow);
        if (creation.IsFailure)
        {
            return Result.Failure<SessionLogResponse>(creation.Error);
        }

        var sessionLog = creation.Value;
        var registered = sessionLog.UpsertItem(BuildInput(request));
        if (registered.IsFailure)
        {
            return Result.Failure<SessionLogResponse>(registered.Error);
        }

        repository.Add(sessionLog);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return SessionLogResponses.From(sessionLog);
    }

    /// <summary>
    /// Registra el ítem en la sesión ya existente y confirma la escritura. La sesión se resuelve por
    /// su clave, así que volver a registrar el mismo ejercicio la actualiza en lugar de duplicarlo.
    /// </summary>
    private async Task<Result<SessionLogResponse>> UpsertAsync(
        SessionLog session,
        RegisterSessionLogCommand request,
        CancellationToken cancellationToken)
    {
        var registered = session.UpsertItem(BuildInput(request));
        if (registered.IsFailure)
        {
            return Result.Failure<SessionLogResponse>(registered.Error);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return SessionLogResponses.From(session);
    }

    /// <summary>
    /// La clave de sesión determinista de la cabecera (ADR-0014): de una sesión de mesociclo,
    /// mesociclo, microciclo y día; de una suelta, solo el tipo y la fecha.
    /// </summary>
    private static SessionLogKey? ToKey(SessionLogKeyInput header) =>
        header.Kind == SessionLogKind.Mesocycle
            && header.MesocycleId is { } mesocycleId
            && header.MicrocycleNumber is { } microcycleNumber
            && header.SessionDay is { } sessionDay
                ? SessionLogKey.ForMesocycle(
                    SingleUser.Id,
                    mesocycleId,
                    microcycleNumber,
                    sessionDay,
                    header.SessionDate)
                : header.Kind == SessionLogKind.Suelta
                    ? SessionLogKey.ForSuelta(SingleUser.Id, header.SessionDate)
                    : null;

    /// <summary>
    /// Monta el ítem con su foto: el <b>nombre</b> y la <b>unidad</b> los deriva el servidor del tipo
    /// ejercicio (nunca el cliente) y el resto de la foto —papel y objetivo— llega del plan.
    /// </summary>
    private SessionLogItemInput BuildInput(RegisterSessionLogCommand request)
    {
        var exercise = knowledgeBase.FindExercise(request.Item.ExerciseId);

        return new SessionLogItemInput(
            request.Item.ExerciseId,
            exercise?.Name ?? request.Item.ExerciseId,
            request.Item.Role,
            request.Item.Pattern,
            exercise?.Metric ?? Metric.Reps,
            request.Item.PrescribedSets,
            request.Item.RepsMin,
            request.Item.RepsMax,
            request.Item.HoldSecondsMin,
            request.Item.HoldSecondsMax,
            request.Item.Note,
            request.Item.Sets,
            request.ClientId);
    }
}
