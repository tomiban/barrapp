using FluentValidation;

namespace Barrapp.Application.Features.SoloSessions;

/// <summary>
/// Valida que el identificador de la suelta a registrar llegue en el pipeline antes del handler.
/// Es defensa en profundidad: el dominio y el repositorio vuelven a comprobar la existencia.
/// </summary>
internal sealed class RecordSoloSessionCommandValidator : AbstractValidator<RecordSoloSessionCommand>
{
    public RecordSoloSessionCommandValidator()
    {
        RuleFor(command => command.Id)
            .NotEmpty()
            .WithMessage("Debes indicar qué sesión suelta quieres registrar.");
    }
}
