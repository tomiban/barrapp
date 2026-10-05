using Barrapp.Domain.Athlete;
using FluentValidation;

namespace Barrapp.Application.Features.AthleteProfiles;

/// <summary>
/// Valida la entrada del comando en el pipeline, antes de que llegue al handler. Comprueba que
/// el peso (30–200 kg), la altura (120–220 cm) y los días de entrenamiento (3–5) están dentro de
/// rango, y que los máximos cubren cada ejercicio básico con repeticiones ≥ 0. Los límites y el
/// catálogo salen del dominio para no duplicarlos.
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

        RuleForEach(command => command.Maximums)
            .ChildRules(maximum =>
            {
                maximum.RuleFor(input => input.ExerciseCode)
                    .Must(BasicExercises.IsKnownCode)
                    .WithMessage("El ejercicio indicado no es un ejercicio básico.");

                maximum.RuleFor(input => input.Repetitions)
                    .GreaterThanOrEqualTo(0)
                    .WithMessage("El máximo no puede ser negativo.");
            });

        RuleFor(command => command.Maximums)
            .Must(HasNoDuplicateExercises)
            .WithMessage("No puedes repetir el máximo de un mismo ejercicio.")
            .When(command => AllCodesAreKnown(command.Maximums));

        RuleFor(command => command.Maximums)
            .Must(CoversEveryBasicExercise)
            .WithMessage("Debes indicar el máximo de todos los ejercicios básicos.")
            .When(command => command.Maximums is null
                || (AllCodesAreKnown(command.Maximums) && HasNoDuplicateExercises(command.Maximums)));
    }

    private static bool AllCodesAreKnown(IReadOnlyList<MaximumInput>? maximums) =>
        maximums is not null && maximums.All(input => BasicExercises.IsKnownCode(input.ExerciseCode));

    private static bool HasNoDuplicateExercises(IReadOnlyList<MaximumInput>? maximums) =>
        maximums is null
        || maximums.Select(input => input.ExerciseCode).Distinct(StringComparer.Ordinal).Count()
            == maximums.Count;

    private static bool CoversEveryBasicExercise(IReadOnlyList<MaximumInput>? maximums)
    {
        if (maximums is null)
        {
            return false;
        }

        var codes = maximums.Select(input => input.ExerciseCode).ToHashSet(StringComparer.Ordinal);
        return BasicExercises.All.All(exercise => codes.Contains(exercise.Code));
    }
}
