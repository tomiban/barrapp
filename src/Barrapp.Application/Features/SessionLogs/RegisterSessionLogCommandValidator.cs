using Barrapp.Application.Abstractions;
using Barrapp.Domain.Sessions;
using FluentValidation;

namespace Barrapp.Application.Features.SessionLogs;

/// <summary>
/// Valida la forma de la entrada en el pipeline, antes de que llegue al handler: la cabecera lleva
/// su clave de sesión según su tipo, el ejercicio existe en el catálogo y hay al menos una serie.
/// Las reglas por serie (número ≥ 1, valor ≥ 0, RIR real 0–10, lastre ≥ 0 y consecutividad) viven
/// solo en el dominio (<see cref="SessionLogItem"/>), como con los máximos del perfil.
/// </summary>
internal sealed class RegisterSessionLogCommandValidator : AbstractValidator<RegisterSessionLogCommand>
{
    public RegisterSessionLogCommandValidator(IKnowledgeBase knowledgeBase)
    {
        When(command => command.Session.Kind == SessionLogKind.Mesocycle, () =>
        {
            RuleFor(command => command.Session.MesocycleId)
                .NotEmpty()
                .WithMessage("Una sesión del mesociclo necesita su mesociclo.");

            RuleFor(command => command.Session.MicrocycleNumber)
                .InclusiveBetween(1, 4)
                .WithMessage("El microciclo debe estar entre 1 y 4.");

            RuleFor(command => command.Session.SessionDay)
                .GreaterThanOrEqualTo(1)
                .WithMessage("El día de la sesión debe ser mayor o igual que 1.");
        });

        When(command => command.Session.Kind == SessionLogKind.Suelta, () =>
        {
            RuleFor(command => command.Session.MesocycleId)
                .Null()
                .WithMessage("Una sesión suelta no lleva mesociclo.");

            RuleFor(command => command.Session.MicrocycleNumber)
                .Null()
                .WithMessage("Una sesión suelta no lleva microciclo.");

            RuleFor(command => command.Session.SessionDay)
                .Null()
                .WithMessage("Una sesión suelta no lleva día de sesión.");
        });

        RuleFor(command => command.Item)
            .NotNull()
            .WithMessage("Debes indicar el ejercicio registrado.");

        RuleFor(command => command.Item.ExerciseId)
            .NotEmpty()
            .WithMessage("Debes indicar el ejercicio registrado.");

        RuleFor(command => command.Item.ExerciseId)
            .Must(exerciseId => knowledgeBase.FindExercise(exerciseId) is not null)
            .WithMessage("El ejercicio indicado no existe en el catálogo.");

        RuleFor(command => command.Item.PrescribedSets)
            .GreaterThanOrEqualTo(1)
            .WithMessage("El objetivo del ejercicio debe declarar al menos una serie.");

        RuleFor(command => command.Item.Sets)
            .NotEmpty()
            .WithMessage("Debes registrar al menos una serie.");
    }
}
