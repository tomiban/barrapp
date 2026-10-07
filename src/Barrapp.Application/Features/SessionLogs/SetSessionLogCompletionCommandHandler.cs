using Barrapp.Application.Abstractions;
using Barrapp.Domain.Common;
using Barrapp.Domain.Sessions;

namespace Barrapp.Application.Features.SessionLogs;

/// <summary>
/// Marca una sesión registrada como completada o la des-completa (spec 0001, US-23). Ambas
/// operaciones son idempotentes —un reintento offline no pisa la marca ni pierde los ítems— y el
/// momento lo fija el servidor, nunca el cliente.
/// </summary>
internal sealed class SetSessionLogCompletionCommandHandler(
    ISessionLogRepository repository,
    IUnitOfWork unitOfWork)
    : ICommandHandler<SetSessionLogCompletionCommand, SessionLogResponse>
{
    public async Task<Result<SessionLogResponse>> Handle(
        SetSessionLogCompletionCommand request,
        CancellationToken cancellationToken)
    {
        var session = await repository.GetByIdAsync(request.SessionLogId, cancellationToken);
        if (session is null)
        {
            return Result.Failure<SessionLogResponse>(DomainErrors.SessionLog.NotFound);
        }

        var completion = request.Completed
            ? session.Complete(DateTimeOffset.UtcNow)
            : session.Uncomplete();
        if (completion.IsFailure)
        {
            return Result.Failure<SessionLogResponse>(completion.Error);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return SessionLogResponses.From(session);
    }
}
