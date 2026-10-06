using Barrapp.Application.Abstractions;
using FluentValidation;

namespace Barrapp.Application.Features.SkillProgress;

/// <summary>
/// Valida la entrada en el pipeline, antes de que llegue al handler: la etapa es al menos 1, el
/// skill existe en el catálogo y la etapa pertenece a su escalera. Es defensa en profundidad: el
/// dominio (<see cref="Barrapp.Domain.SkillProgress.AthleteSkillProgress"/>) vuelve a comprobar el
/// mismo invariante contra <see cref="IKnowledgeBase"/>.
/// </summary>
internal sealed class SetSkillProgressCommandValidator : AbstractValidator<SetSkillProgressCommand>
{
    public SetSkillProgressCommandValidator(IKnowledgeBase knowledgeBase)
    {
        RuleFor(command => command.StageOrder)
            .GreaterThanOrEqualTo(1)
            .WithMessage("La etapa debe ser al menos 1.");

        RuleFor(command => command.SkillId)
            .Must(skillId => knowledgeBase.FindSkill(skillId) is not null)
            .WithMessage("El skill indicado no existe en el catálogo.");

        RuleFor(command => command)
            .Custom((command, context) =>
            {
                var skill = knowledgeBase.FindSkill(command.SkillId);
                if (skill is not null && !skill.Stages.Any(stage => stage.Order == command.StageOrder))
                {
                    context.AddFailure(
                        nameof(command.StageOrder),
                        "La etapa indicada no existe en la escalera del skill.");
                }
            });
    }
}
