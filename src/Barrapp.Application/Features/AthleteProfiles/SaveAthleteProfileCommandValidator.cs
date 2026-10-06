using Barrapp.Application.Common;
using Barrapp.Domain.Athlete;
using FluentValidation;

namespace Barrapp.Application.Features.AthleteProfiles;

/// <summary>
/// Valida la forma de la entrada en el pipeline, antes de que llegue al handler: que el peso
/// (30–200 kg), la altura (120–220 cm), la envergadura (100–250 cm), la entrepierna
/// (50–130 cm) y los días de entrenamiento (3–5) estén dentro de rango.
/// Los límites salen del dominio para no duplicarlos. La semántica de los máximos (código
/// conocido, sin repetidos, cobertura de todos los ejercicios básicos y repeticiones ≥ 0) y de los
/// días de la semana vive solo en <see cref="AthleteProfile"/>: el validador no la reimplementa.
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

        RuleFor(command => command.ArmSpanCentimeters)
            .InclusiveBetween(AthleteProfile.MinArmSpanCentimeters, AthleteProfile.MaxArmSpanCentimeters)
            .WithMessage("La envergadura debe estar entre {From} y {To} cm.");

        RuleFor(command => command.InseamCentimeters)
            .InclusiveBetween(AthleteProfile.MinInseamCentimeters, AthleteProfile.MaxInseamCentimeters)
            .WithMessage("La entrepierna debe estar entre {From} y {To} cm.");

        RuleFor(command => command.TrainingDays)
            .InclusiveBetween(AthleteProfile.MinTrainingDays, AthleteProfile.MaxTrainingDays)
            .WithMessage("Los días de entrenamiento deben estar entre {From} y {To}.");

        // Los días de la semana elegidos (#94) tienen que ser tantos como la frecuencia y sin
        // repetir: el número de días de entrenamiento es cuántos días de la semana elige el atleta.
        RuleFor(command => command)
            .Must(command =>
                command.TrainingWeekdays is null
                || command.TrainingWeekdays.Count == command.TrainingDays)
            .WithName("TrainingWeekdays")
            .WithMessage("Elige tantos días de la semana como días de entrenamiento.");

        RuleFor(command => command)
            .Must(command =>
                command.TrainingWeekdays is null
                || command.TrainingWeekdays.Distinct().Count() == command.TrainingWeekdays.Count)
            .WithName("TrainingWeekdays")
            .WithMessage("No puedes repetir un día de la semana.");

        RuleFor(command => command)
            .Must(command =>
                command.TrainingWeekdays is null
                || command.TrainingWeekdays.All(code => WeekdayCode.ToWeekday(code) is not null))
            .WithName("TrainingWeekdays")
            .WithMessage("Cada día de la semana debe ser un día válido (de monday a sunday).");
    }
}
