using Barrapp.Application.Abstractions;
using Barrapp.Application.Common;
using Barrapp.Domain.Common;
using Barrapp.Domain.Objectives;

namespace Barrapp.Application.Features.Objectives;

/// <summary>
/// Cargar el objetivo → crearlo o actualizarlo → guardar. El invariante (el skill existe en el
/// catálogo) vive en <see cref="Objective"/> y se comprueba contra <see cref="IKnowledgeBase"/>.
/// </summary>
internal sealed class SetObjectiveCommandHandler(
    IObjectiveRepository repository,
    IKnowledgeBase knowledgeBase,
    IUnitOfWork unitOfWork)
    : ICommandHandler<SetObjectiveCommand, ObjectiveResponse>
{
    public async Task<Result<ObjectiveResponse>> Handle(
        SetObjectiveCommand request,
        CancellationToken cancellationToken)
    {
        var objective = await repository.GetByUserIdAsync(SingleUser.Id, cancellationToken);

        if (objective is null)
        {
            var creation = Objective.Create(SingleUser.Id, request.SkillId, knowledgeBase);
            if (creation.IsFailure)
            {
                return Result.Failure<ObjectiveResponse>(creation.Error);
            }

            objective = creation.Value;
            repository.Add(objective);
        }
        else
        {
            var update = objective.Update(request.SkillId, knowledgeBase);
            if (update.IsFailure)
            {
                return Result.Failure<ObjectiveResponse>(update.Error);
            }
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new ObjectiveResponse(objective.SkillId);
    }
}
