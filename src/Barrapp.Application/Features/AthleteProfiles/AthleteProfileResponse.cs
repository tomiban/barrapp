namespace Barrapp.Application.Features.AthleteProfiles;

/// <summary>Máximo de un ejercicio básico tal y como lo intercambia el API.</summary>
/// <param name="ExerciseCode">Código estable del ejercicio básico.</param>
/// <param name="Repetitions">Repeticiones máximas estrictas; 0 indica regresión.</param>
public sealed record MaximumResponse(string ExerciseCode, int Repetitions);

/// <summary>
/// Perfil del atleta tal y como lo consume la app: peso en kg, altura en cm, días de
/// entrenamiento por semana y el máximo de cada ejercicio básico.
/// </summary>
/// <param name="WeightKilograms">Peso del atleta en kilogramos.</param>
/// <param name="HeightCentimeters">Altura del atleta en centímetros.</param>
/// <param name="TrainingDays">Días de entrenamiento por semana (3–5).</param>
/// <param name="Maximums">Máximo por cada ejercicio básico; 0 vale (regresión).</param>
public sealed record AthleteProfileResponse(
    double WeightKilograms,
    double HeightCentimeters,
    int TrainingDays,
    IReadOnlyList<MaximumResponse> Maximums);
