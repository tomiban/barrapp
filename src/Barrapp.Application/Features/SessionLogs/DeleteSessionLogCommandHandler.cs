using Barrapp.Application.Abstractions;
using Barrapp.Domain.Common;
using MediatR;

namespace Barrapp.Application.Features.SessionLogs;

/// <summary>
/// Carga el registro, lo marca para eliminar y confirma con la unidad de trabajo. Si el registro
/// no existe, devuelve <see cref="DomainErrors.SessionLog.NotFound"/>. Las series (objetos valor
/// owned) se eliminan en cascada con su registro.
/// </summary>
internal sealed class DeleteSessionLogCommandHandler(
    ISessionLogRepository repository,
    IUnitOfWork unitOfWork)
    : ICommandHandler<DeleteSessionLogCommand, Unit>
{
    public async Task<Result<Unit>> Handle(
        DeleteSessionLogCommand request,
        CancellationToken cancellationToken)
    {
        var sessionLog = await repository.GetByIdAsync(request.Id, cancellationToken);
        if (sessionLog is null)
        {
            return Result.Failure<Unit>(DomainErrors.SessionLog.NotFound);
        }

        repository.Remove(sessionLog);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(Unit.Value);
    }
}