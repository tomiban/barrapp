using Barrapp.Application.Abstractions;
using Barrapp.Domain.Athlete;

namespace Barrapp.Application.Features.AthleteProfiles;

/// <summary>
/// Crea el perfil del atleta si no existe o lo actualiza si ya existe. Es también el DTO de
/// entrada del endpoint: peso en kg, altura, envergadura y entrepierna en cm, días de
/// entrenamiento por semana y el máximo de cada ejercicio básico.
/// </summary>
/// <param name="WeightKilograms">Peso del atleta en kilogramos.</param>
/// <param name="HeightCentimeters">Altura del atleta en centímetros.</param>
/// <param name="ArmSpanCentimeters">Envergadura del atleta en centímetros.</param>
/// <param name="InseamCentimeters">Entrepierna del atleta en centímetros.</param>
/// <param name="TrainingDays">Días de entrenamiento por semana (3–5).</param>
/// <param name="Maximums">Máximo por cada ejercicio básico; 0 vale (regresión).</param>
public sealed record SaveAthleteProfileCommand(
    double WeightKilograms,
    double HeightCentimeters,
    double ArmSpanCentimeters,
    double InseamCentimeters,
    int TrainingDays,
    IReadOnlyList<MaximumInput> Maximums) : ICommand<AthleteProfileResponse>;
