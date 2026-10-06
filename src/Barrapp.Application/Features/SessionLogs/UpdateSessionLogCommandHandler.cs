using Barrapp.Application.Abstractions;
using Barrapp.Domain.Common;
using Barrapp.Domain.Sessions;

namespace Barrapp.Application.Features.SessionLogs;

/// <summary>
/// Carga el registro, sustituye sus series con <see cref="SessionLog.Update"/> (los invariantes
/// viven en el dominio) y confirma con la unidad de trabajo. Si el registro no existe, devuelve
/// <see cref="DomainErrors.SessionLog.NotFound"/>. El nombre y la unidad del ejercicio se resuelven
/// contra el catálogo, como hace el <c>GET</c>.
/// </summary>
internal sealed class UpdateSessionLogCommandHandler(
    ISessionLogRepository repository,
    IKnowledgeBase knowledgeBase,
    IUnitOfWork unitOfWork)
    : ICommandHandler<UpdateSessionLogCommand, SessionLogResponse>
{
    public async Task<Result<SessionLogResponse>> Handle(
        UpdateSessionLogCommand request,
        CancellationToken cancellationToken)
    {
        var sessionLog = await repository.GetByIdAsync(request.Id, cancellationToken);
        if (sessionLog is null)
        {
            return Result.Failure<SessionLogResponse>(DomainErrors.SessionLog.NotFound);
        }

        var update = sessionLog.Update(request.Sets);
        if (update.IsFailure)
        {
            return Result.Failure<SessionLogResponse>(update.Error);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return SessionLogResponses.From(sessionLog, knowledgeBase);
    }
}
