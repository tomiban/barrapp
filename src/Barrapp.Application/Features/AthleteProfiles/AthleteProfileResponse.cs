namespace Barrapp.Application.Features.AthleteProfiles;

/// <summary>
/// Perfil del atleta tal y como lo consume la app: peso en kg, altura en cm y días de
/// entrenamiento por semana.
/// </summary>
/// <param name="WeightKilograms">Peso del atleta en kilogramos.</param>
/// <param name="HeightCentimeters">Altura del atleta en centímetros.</param>
/// <param name="TrainingDays">Días de entrenamiento por semana (3–5).</param>
public sealed record AthleteProfileResponse(
    double WeightKilograms,
    double HeightCentimeters,
    int TrainingDays);
