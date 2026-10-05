using Barrapp.Domain.Athlete;
using FluentValidation;

namespace Barrapp.Application.Features.AthleteProfiles;

/// <summary>
/// Valida la entrada del comando en el pipeline, antes de que llegue al handler. Comprueba que
/// el peso (30–200 kg), la altura (120–220 cm) y los días de entrenamiento (3–5) están dentro de
/// rango; los límites salen de <see cref="AthleteProfile"/> para no duplicarlos.
/// </summary>
internal sealed class SaveAthleteProfileCommandValidator : AbstractValidator<SaveAthleteProfileCommand>
{
    public SaveAthleteProfileCommandValidator()
    {
        RuleFor(command => command.WeightKilograms)
            .InclusiveBetween(AthleteProfile.MinWeightKilograms, AthleteProfile.MaxWeightKilograms)
            .WithMessage("El peso debe estar entre {From} y {To} kg.");

        RuleFor(command => command.HeightCentimeters)
            .InclusiveBetween(AthleteProfile.MinHeightCentimeters, AthleteProfile.MaxHeightCentimeters)
            .WithMessage("La altura debe estar entre {From} y {To} cm.");

        RuleFor(command => command.TrainingDays)
            .InclusiveBetween(AthleteProfile.MinTrainingDays, AthleteProfile.MaxTrainingDays)
            .WithMessage("Los días de entrenamiento deben estar entre {From} y {To}.");
    }
}
