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
        /// <summary>El peso debe ser un número positivo.</summary>
        public static readonly Error WeightMustBePositive = Error.Validation(
            "athlete_profile.weight_must_be_positive",
            "El peso debe ser mayor que cero.");

        /// <summary>La altura debe ser un número positivo.</summary>
        public static readonly Error HeightMustBePositive = Error.Validation(
            "athlete_profile.height_must_be_positive",
            "La altura debe ser mayor que cero.");

        /// <summary>Todavía no hay ningún perfil guardado.</summary>
        public static readonly Error NotFound = Error.NotFound(
            "athlete_profile.not_found",
            "No hay ningún perfil guardado para este atleta.");
    }
}
