namespace Barrapp.Domain.Athlete;

/// <summary>
/// Entrada para fijar el máximo de un ejercicio básico. El agregado
/// <see cref="AthleteProfile"/> la valida y construye el <see cref="Maximum"/>.
/// </summary>
/// <param name="ExerciseCode">Código estable del ejercicio básico.</param>
/// <param name="Repetitions">Repeticiones máximas; 0 vale (regresión).</param>
public sealed record MaximumInput(string ExerciseCode, int Repetitions);
