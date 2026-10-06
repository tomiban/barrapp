using Barrapp.Application.Abstractions;
using FluentValidation;

namespace Barrapp.Application.Features.SessionLogs;

/// <summary>
/// Valida la forma de la entrada en el pipeline, antes de que llegue al handler: al menos una
/// serie. Las reglas por serie (número ≥ 1, valor ≥ 0, esfuerzo 0–10 y consecutividad) viven solo
/// en el dominio (<see cref="Barrapp.Domain.Sessions.SessionLog"/>) y el identificador lo garantiza
/// el enrutado (<c>PUT /session-logs/{{id:guid}}</c>), como en el resto de la API.
/// </summary>
internal sealed class UpdateSessionLogCommandValidator : AbstractValidator<UpdateSessionLogCommand>
{
    public UpdateSessionLogCommandValidator()
    {
        RuleFor(command => command.Sets)
            .NotEmpty()
            .WithMessage("Debes registrar al menos una serie.");
    }
}
