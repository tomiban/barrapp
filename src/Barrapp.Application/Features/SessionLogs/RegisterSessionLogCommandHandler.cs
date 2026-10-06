using Barrapp.Application.Abstractions;
using Barrapp.Application.Common;
using Barrapp.Domain.Common;
using Barrapp.Domain.Sessions;

namespace Barrapp.Application.Features.SessionLogs;

/// <summary>
/// Construye el registro con <see cref="SessionLog.Create"/> (los invariantes viven en el dominio),
/// lo marca para insertar y confirma con la unidad de trabajo. El momento del registro lo fija el
/// servidor, nunca el cliente.
/// </summary>
/// <remarks>
/// Idempotencia de la outbox offline (ticket #26): si el alta trae <see cref="RegisterSessionLogCommand.ClientId"/>
/// y el atleta ya tiene una fila con ese id —un envío cuya respuesta se perdió y se reintenta—, no
/// se inserta otra: se actualizan las series del registro original en su sitio
/// (<c>last-write-wins</c>, se conserva la identidad de la fila, incluida su fecha de registro) y
/// se devuelve ese mismo registro. El índice único filtrado <c>(UserId, ClientId)</c> de la base es
/// el respaldo de la invariante.
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
        var exercise = knowledgeBase.FindExercise(request.ExerciseId);
        if (exercise is null)
        {
            return Result.Failure<SessionLogResponse>(DomainErrors.SessionLog.UnknownExercise);
        }

        if (request.ClientId is { } clientId)
        {
            var existing = await repository.GetByClientIdAsync(
                SingleUser.Id,
                clientId,
                cancellationToken);
            if (existing is not null)
            {
                var updated = existing.Update(request.Sets);
                if (updated.IsFailure)
                {
                    return Result.Failure<SessionLogResponse>(updated.Error);
                }

                await unitOfWork.SaveChangesAsync(cancellationToken);
                return SessionLogResponses.From(existing, exercise.Name, exercise.Metric);
            }
        }

        var creation = SessionLog.Create(
            SingleUser.Id,
            request.ExerciseId,
            request.MesocycleId,
            request.SessionDay,
            DateTimeOffset.UtcNow,
            request.Sets,
            request.ClientId);
        if (creation.IsFailure)
        {
            return Result.Failure<SessionLogResponse>(creation.Error);
        }

        var sessionLog = creation.Value;
        repository.Add(sessionLog);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return SessionLogResponses.From(sessionLog, exercise.Name, exercise.Metric);
    }
}
