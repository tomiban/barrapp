namespace Barrapp.Application.Features.AthleteProfiles;

/// <summary>
/// Perfil del atleta tal y como lo consume la app: peso en kg y altura en cm.
/// </summary>
/// <param name="WeightKilograms">Peso del atleta en kilogramos.</param>
/// <param name="HeightCentimeters">Altura del atleta en centímetros.</param>
public sealed record AthleteProfileResponse(double WeightKilograms, double HeightCentimeters);
