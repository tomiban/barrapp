using Barrapp.Application.Abstractions;
using Barrapp.Domain.Common;
using Barrapp.Domain.Sessions;

namespace Barrapp.Application.Features.SessionLogs;

/// <summary>
/// Sustituye las series de un ítem registrado (spec 0001, US-36; decisión D5: editar reemplaza los
/// valores de la serie). La identidad de la sesión y la foto del ítem no cambian: solo se pisan los
/// valores. Si el registro o el ítem no existen, devuelve el <c>Not Found</c> correspondiente.
/// </summary>
internal sealed class UpdateSessionLogItemCommandHandler(
    ISessionLogRepository repository,
    IUnitOfWork unitOfWork)
    : ICommandHandler<UpdateSessionLogItemCommand, SessionLogItemResponse>
{
    public async Task<Result<SessionLogItemResponse>> Handle(
        UpdateSessionLogItemCommand request,
        CancellationToken cancellationToken)
    {
        var session = await repository.GetByIdAsync(request.SessionLogId, cancellationToken);
        if (session is null)
        {
            return Result.Failure<SessionLogItemResponse>(DomainErrors.SessionLog.NotFound);
        }

        var updated = session.UpdateItemSets(request.ItemId, request.Sets);
        if (updated.IsFailure)
        {
            return Result.Failure<SessionLogItemResponse>(updated.Error);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return SessionLogResponses.From(updated.Value);
    }
}
