using FluentValidation;

namespace Barrapp.Application.Features.AthleteProfiles;

/// <summary>
/// Valida la entrada del comando en el pipeline, antes de que llegue al handler. Comprueba que
/// peso y altura son medidas positivas; los rangos definitivos (peso 30–200 kg, altura
/// 120–220 cm) llegan en el ticket #4.
/// </summary>
internal sealed class SaveAthleteProfileCommandValidator : AbstractValidator<SaveAthleteProfileCommand>
{
    public SaveAthleteProfileCommandValidator()
    {
        RuleFor(command => command.WeightKilograms)
            .GreaterThan(0)
            .WithMessage("El peso debe ser mayor que cero.");

        RuleFor(command => command.HeightCentimeters)
            .GreaterThan(0)
            .WithMessage("La altura debe ser mayor que cero.");
    }
}
