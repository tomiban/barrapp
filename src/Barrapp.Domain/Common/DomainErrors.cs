namespace Barrapp.Domain.Common;

/// <summary>
/// Errores de dominio, agrupados por agregado. Son valores, no excepciones: el flujo
/// esperado se comunica con <see cref="Result"/>.
/// </summary>
public static class DomainErrors
{
    /// <summary>Errores del perfil del atleta.</summary>
    public static class AthleteProfile
    {
        /// <summary>El peso está fuera del rango admitido (30–200 kg).</summary>
        public static readonly Error WeightOutOfRange = Error.Validation(
            "athlete_profile.weight_out_of_range",
            "El peso debe estar entre 30 y 200 kg.");

        /// <summary>La altura está fuera del rango admitido (120–220 cm).</summary>
        public static readonly Error HeightOutOfRange = Error.Validation(
            "athlete_profile.height_out_of_range",
            "La altura debe estar entre 120 y 220 cm.");

        /// <summary>Los días de entrenamiento están fuera del rango admitido (3–5).</summary>
        public static readonly Error TrainingDaysOutOfRange = Error.Validation(
            "athlete_profile.training_days_out_of_range",
            "Los días de entrenamiento deben estar entre 3 y 5.");

        /// <summary>El máximo de un ejercicio no puede ser negativo (0 sí vale).</summary>
        public static readonly Error MaximumMustBeNonNegative = Error.Validation(
            "athlete_profile.maximum_must_be_non_negative",
            "El máximo no puede ser negativo.");

        /// <summary>El código no corresponde a ningún ejercicio básico del catálogo.</summary>
        public static readonly Error UnknownExerciseCode = Error.Validation(
            "athlete_profile.unknown_exercise_code",
            "El ejercicio indicado no es un ejercicio básico.");

        /// <summary>Hay dos máximos para el mismo ejercicio.</summary>
        public static readonly Error DuplicateExerciseMaximum = Error.Validation(
            "athlete_profile.duplicate_exercise_maximum",
            "No puedes repetir el máximo de un mismo ejercicio.");

        /// <summary>Falta el máximo de algún ejercicio básico.</summary>
        public static readonly Error MissingExerciseMaximum = Error.Validation(
            "athlete_profile.missing_exercise_maximum",
            "Debes indicar el máximo de todos los ejercicios básicos.");

        /// <summary>Todavía no hay ningún perfil guardado.</summary>
        public static readonly Error NotFound = Error.NotFound(
            "athlete_profile.not_found",
            "No hay ningún perfil guardado para este atleta.");
    }
}
