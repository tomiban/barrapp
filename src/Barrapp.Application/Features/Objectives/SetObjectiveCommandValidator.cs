using FluentValidation;

namespace Barrapp.Application.Features.Objectives;

/// <summary>
/// Valida la forma de la entrada en el pipeline, antes de que llegue al handler: el skill
/// objetivo es obligatorio. Que exista en el catálogo lo comprueba
/// <see cref="Barrapp.Domain.Objectives.Objective"/>: el validador no reimplementa esa regla.
/// </summary>
internal sealed class SetObjectiveCommandValidator : AbstractValidator<SetObjectiveCommand>
{
    public SetObjectiveCommandValidator()
    {
        RuleFor(command => command.SkillId)
            .NotEmpty()
            .WithMessage("Debes elegir un skill objetivo.");
    }
}
