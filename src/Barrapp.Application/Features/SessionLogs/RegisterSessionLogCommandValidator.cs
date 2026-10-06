using Barrapp.Application.Abstractions;
using FluentValidation;

namespace Barrapp.Application.Features.SessionLogs;

/// <summary>
/// Valida la forma de la entrada en el pipeline, antes de que llegue al handler: el día de la
/// sesión es al menos 1, el ejercicio existe en el catálogo y hay al menos una serie. Las reglas
/// por serie (número ≥ 1, valor ≥ 0, esfuerzo 0–10 y consecutividad) viven solo en el dominio
/// (<see cref="Barrapp.Domain.Sessions.SessionLog"/>), como con los máximos del perfil.
/// </summary>
internal sealed class RegisterSessionLogCommandValidator : AbstractValidator<RegisterSessionLogCommand>
{
    public RegisterSessionLogCommandValidator(IKnowledgeBase knowledgeBase)
    {
        RuleFor(command => command.SessionDay)
            .GreaterThanOrEqualTo(1)
            .WithMessage("El día de la sesión debe ser mayor o igual que 1.");

        RuleFor(command => command.ExerciseId)
            .NotEmpty()
            .WithMessage("Debes indicar el ejercicio registrado.");

        RuleFor(command => command.ExerciseId)
            .Must(exerciseId => knowledgeBase.FindExercise(exerciseId) is not null)
            .WithMessage("El ejercicio indicado no existe en el catálogo.");

        RuleFor(command => command.Sets)
            .NotEmpty()
            .WithMessage("Debes registrar al menos una serie.");
    }
}
