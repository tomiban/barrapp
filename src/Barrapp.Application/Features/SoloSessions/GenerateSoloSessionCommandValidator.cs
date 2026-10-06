using Barrapp.Application.Abstractions;
using Barrapp.Application.Common;
using Barrapp.Domain.Planning;
using FluentValidation;

namespace Barrapp.Application.Features.SoloSessions;

/// <summary>
/// Valida la entrada en el pipeline, antes de llegar al handler: el tiempo es uno de los cuatro
/// admitidos, la energía y el foco son de su vocabulario y, cuando el foco es de patrón, el grupo es
/// obligatorio y uno de los tres patrones. Es defensa en profundidad: el dominio vuelve a comprobar
/// el invariante del foco (<see cref="DomainErrors.SessionSuelta"/>).
/// </summary>
internal sealed class GenerateSoloSessionCommandValidator : AbstractValidator<GenerateSoloSessionCommand>
{
    public GenerateSoloSessionCommandValidator()
    {
        RuleFor(command => command.TimeMinutes)
            .Must(time => time is 15 or 30 or 45 or 60)
            .WithMessage("El tiempo debe ser 15, 30, 45 o 60 minutos.");

        RuleFor(command => command.Energy)
            .Must(energy => energy is "baja" or "media" or "alta")
            .WithMessage("La energía debe ser baja, media o alta.");

        RuleFor(command => command.Focus)
            .Must(focus => focus is "patron" or "skill" or "sorprendeme")
            .WithMessage("El foco debe ser patrón, skill o sorpréndeme.");

        RuleFor(command => command.Pattern)
            .Must(pattern => pattern is "push" or "pull" or "leg")
            .When(command => command.Focus == "patron")
            .WithMessage("Debes elegir un patrón (empuje, tirón o pierna).");

        RuleFor(command => command.Pattern)
            .Null()
            .When(command => command.Focus != "patron")
            .WithMessage("El patrón solo se indica cuando el foco es de patrón.");
    }
}
