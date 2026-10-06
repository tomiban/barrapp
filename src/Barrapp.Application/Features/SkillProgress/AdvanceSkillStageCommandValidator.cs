using Barrapp.Application.Abstractions;
using FluentValidation;

namespace Barrapp.Application.Features.SkillProgress;

/// <summary>
/// Valida la entrada en el pipeline, antes de que llegue al handler: el skill debe existir en el
/// catálogo. Es defensa en profundidad: el dominio vuelve a comprobar el mismo invariante contra
/// <see cref="IKnowledgeBase"/>.
/// </summary>
internal sealed class AdvanceSkillStageCommandValidator : AbstractValidator<AdvanceSkillStageCommand>
{
    public AdvanceSkillStageCommandValidator(IKnowledgeBase knowledgeBase)
    {
        RuleFor(command => command.SkillId)
            .Must(skillId => knowledgeBase.FindSkill(skillId) is not null)
            .WithMessage("El skill indicado no existe en el catálogo.");
    }
}
