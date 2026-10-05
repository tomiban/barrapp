using Barrapp.Application.Abstractions;
using Barrapp.Application.Common;
using Barrapp.Domain.Common;
using Barrapp.Domain.SkillProgress;

namespace Barrapp.Application.Features.SkillProgress;

/// <summary>
/// Cargar la progresión del skill → crearla o actualizarla → guardar. El invariante (el skill y la
/// etapa existen en el catálogo) vive en <see cref="AthleteSkillProgress"/> y se comprueba contra
/// <see cref="IKnowledgeBase"/>.
/// </summary>
internal sealed class SetSkillProgressCommandHandler(
    IAthleteSkillProgressRepository repository,
    IKnowledgeBase knowledgeBase,
    IUnitOfWork unitOfWork)
    : ICommandHandler<SetSkillProgressCommand, SkillProgressResponse>
{
    public async Task<Result<SkillProgressResponse>> Handle(
        SetSkillProgressCommand request,
        CancellationToken cancellationToken)
    {
        var progress = await repository.GetAsync(SingleUser.Id, request.SkillId, cancellationToken);

        if (progress is null)
        {
            var creation = AthleteSkillProgress.Create(
                SingleUser.Id,
                request.SkillId,
                request.StageOrder,
                knowledgeBase);

            if (creation.IsFailure)
            {
                return Result.Failure<SkillProgressResponse>(creation.Error);
            }

            progress = creation.Value;
            repository.Add(progress);
        }
        else
        {
            var update = progress.Update(request.StageOrder, knowledgeBase);
            if (update.IsFailure)
            {
                return Result.Failure<SkillProgressResponse>(update.Error);
            }
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new SkillProgressResponse(progress.SkillId, progress.StageOrder);
    }
}
