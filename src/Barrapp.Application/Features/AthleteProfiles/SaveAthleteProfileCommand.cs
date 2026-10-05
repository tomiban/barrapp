using Barrapp.Application.Abstractions;

namespace Barrapp.Application.Features.AthleteProfiles;

/// <summary>
/// Crea el perfil del atleta si no existe o lo actualiza si ya existe. Es también el DTO de
/// entrada del endpoint: peso en kg y altura en cm.
/// </summary>
/// <param name="WeightKilograms">Peso del atleta en kilogramos.</param>
/// <param name="HeightCentimeters">Altura del atleta en centímetros.</param>
public sealed record SaveAthleteProfileCommand(double WeightKilograms, double HeightCentimeters)
    : ICommand<AthleteProfileResponse>;
