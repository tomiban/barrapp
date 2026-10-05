namespace Barrapp.Domain.Athlete;

/// <summary>
/// Catálogo de ejercicios básicos del MVP (M1): uno por patrón. Es la fuente única de la
/// lista; la validación y la UI la leen de aquí, no la duplican.
/// </summary>
public static class BasicExercises
{
    /// <summary>Flexión (empuje).</summary>
    public static readonly BasicExercise PushUp = new("push_up", "Flexión", ExercisePattern.Push);

    /// <summary>Dominada (tirón).</summary>
    public static readonly BasicExercise PullUp = new("pull_up", "Dominada", ExercisePattern.Pull);

    /// <summary>Sentadilla (pierna).</summary>
    public static readonly BasicExercise Squat = new("squat", "Sentadilla", ExercisePattern.Legs);

    /// <summary>Todos los ejercicios básicos, ordenados por patrón (empuje, tirón, pierna).</summary>
    public static readonly IReadOnlyList<BasicExercise> All = [PushUp, PullUp, Squat];

    /// <summary>Devuelve el ejercicio básico con ese código, o <c>null</c> si no existe.</summary>
    public static BasicExercise? FindByCode(string? code) =>
        All.FirstOrDefault(exercise => string.Equals(exercise.Code, code, StringComparison.Ordinal));

    /// <summary>Indica si el código corresponde a un ejercicio básico del catálogo.</summary>
    public static bool IsKnownCode(string? code) => FindByCode(code) is not null;
}
