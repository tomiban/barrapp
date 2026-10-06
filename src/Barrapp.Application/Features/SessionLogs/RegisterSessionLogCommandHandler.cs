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

        var creation = SessionLog.Create(
            SingleUser.Id,
            request.ExerciseId,
            request.MesocycleId,
            request.SessionDay,
            DateTimeOffset.UtcNow,
            request.Sets);
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
