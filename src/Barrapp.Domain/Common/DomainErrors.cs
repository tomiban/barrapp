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

        /// <summary>Todavía no hay ningún perfil guardado.</summary>
        public static readonly Error NotFound = Error.NotFound(
            "athlete_profile.not_found",
            "No hay ningún perfil guardado para este atleta.");
    }
}
