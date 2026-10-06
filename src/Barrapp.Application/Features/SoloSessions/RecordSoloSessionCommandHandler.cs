using Barrapp.Application.Abstractions;
using Barrapp.Application.Common;
using Barrapp.Domain.Common;

namespace Barrapp.Application.Features.SoloSessions;

/// <summary>
/// Marca la suelta como registrada vía <see cref="SessionSuelta.MarkRecorded"/> (el invariante de
/// estado vive en el dominio) y confirma con la unidad de trabajo. El momento del registro lo fija
/// el servidor, nunca el cliente. No toca registros de sesión, mesociclo ni máximos.
/// </summary>
internal sealed class RecordSoloSessionCommandHandler(
    ISessionSueltaRepository repository,
    IKnowledgeBase catalog,
    IUnitOfWork unitOfWork)
    : ICommandHandler<RecordSoloSessionCommand, SessionSueltaResponse>
{
    public async Task<Result<SessionSueltaResponse>> Handle(
        RecordSoloSessionCommand request,
        CancellationToken cancellationToken)
    {
        var suelta = await repository.GetByIdAsync(request.Id, SingleUser.Id, cancellationToken);
        if (suelta is null)
        {
            return Result.Failure<SessionSueltaResponse>(DomainErrors.SessionSuelta.NotFound);
        }

        var recorded = suelta.MarkRecorded(DateTimeOffset.UtcNow);
        if (recorded.IsFailure)
        {
            return Result.Failure<SessionSueltaResponse>(recorded.Error);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return SueltaResponses.From(suelta, catalog);
    }
}
