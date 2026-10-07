using Barrapp.Application.Abstractions;
using FluentValidation;

namespace Barrapp.Application.Features.SessionLogs;

/// <summary>
/// Valida la forma de la entrada en el pipeline, antes de que llegue al handler: al menos una
/// serie. Las reglas por serie (número ≥ 1, valor ≥ 0, RIR real 0–10, lastre ≥ 0 y consecutividad)
/// viven solo en el dominio (<see cref="Barrapp.Domain.Sessions.SessionLogItem"/>) y los
/// identificadores los garantiza el enrutado (<c>PUT /session-logs/{{id:guid}}/items/{{itemId:guid}}</c>),
/// como en el resto de la API.
/// </summary>
internal sealed class UpdateSessionLogItemCommandValidator
    : AbstractValidator<UpdateSessionLogItemCommand>
{
    public UpdateSessionLogItemCommandValidator()
    {
        RuleFor(command => command.Sets)
            .NotEmpty()
            .WithMessage("Debes registrar al menos una serie.");
    }
}
