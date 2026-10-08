using Barrapp.Application.Abstractions;
using Barrapp.Domain.Common;
using Barrapp.Domain.Sessions;
using MediatR;

namespace Barrapp.Application.Features.SessionLogs;

/// <summary>
/// Borra un ítem registrado (spec 0001, US-36). El borrado <b>des-completa</b> la sesión: lo que
/// dependía de ella —la <i>adherencia</i>— se recalcula sobre lo que queda. Si era el último ítem,
/// la sesión se borra también: una cabecera sin ítems no es un registro y no debe contar para nada.
/// </summary>
internal sealed class DeleteSessionLogItemCommandHandler(
    ISessionLogRepository repository,
    IUnitOfWork unitOfWork)
    : ICommandHandler<DeleteSessionLogItemCommand, Unit>
{
    public async Task<Result<Unit>> Handle(
        DeleteSessionLogItemCommand request,
        CancellationToken cancellationToken)
    {
        var session = await repository.GetByIdAsync(request.SessionLogId, cancellationToken);
        if (session is null)
        {
            return Result.Failure<Unit>(DomainErrors.SessionLog.NotFound);
        }

        var removed = session.RemoveItem(request.ItemId);
        if (removed.IsFailure)
        {
            return Result.Failure<Unit>(removed.Error);
        }

        if (session.Items.Count == 0)
        {
            repository.Remove(session);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(Unit.Value);
    }
}
