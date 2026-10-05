using Barrapp.Application.Abstractions;

namespace Barrapp.Application.Features.AthleteProfiles;

/// <summary>
/// Crea el perfil del atleta si no existe o lo actualiza si ya existe. Es también el DTO de
/// entrada del endpoint: peso en kg, altura en cm y días de entrenamiento por semana.
/// </summary>
/// <param name="WeightKilograms">Peso del atleta en kilogramos.</param>
/// <param name="HeightCentimeters">Altura del atleta en centímetros.</param>
/// <param name="TrainingDays">Días de entrenamiento por semana (3–5).</param>
public sealed record SaveAthleteProfileCommand(
    double WeightKilograms,
    double HeightCentimeters,
    int TrainingDays) : ICommand<AthleteProfileResponse>;
