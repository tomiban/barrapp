using FluentValidation;

namespace Barrapp.Application.Features.SkillProgress;

/// <summary>
/// Valida la forma de la entrada en el pipeline, antes de que llegue al handler: la etapa es al
/// menos 1. Que exista en la escalera del skill lo comprueba
/// <see cref="Barrapp.Domain.SkillProgress.AthleteSkillProgress"/>: el validador no reimplementa
/// esa regla.
/// </summary>
internal sealed class SetSkillProgressCommandValidator : AbstractValidator<SetSkillProgressCommand>
{
    public SetSkillProgressCommandValidator()
    {
        RuleFor(command => command.StageOrder)
            .GreaterThanOrEqualTo(1)
            .WithMessage("La etapa debe ser al menos 1.");
    }
}
