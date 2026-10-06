namespace Barrapp.Application.Common;

/// <summary>
/// Vocabulario de día de la semana en el cable (#94): el <see cref="DayOfWeek"/> del dominio
/// viaja como código estable en minúsculas (<c>monday</c>…<c>sunday</c>) y la app lo traduce a su
/// etiqueta en español. Es la única casa de ese mapa, como lo es <c>CatalogMappings</c> para los
/// roles y patrones de sesión.
/// </summary>
internal static class WeekdayCode
{
    private static readonly IReadOnlyDictionary<string, DayOfWeek> ByCode = Enum
        .GetValues<DayOfWeek>()
        .ToDictionary(day => ToCode(day), day => day, StringComparer.Ordinal);

    /// <summary>Código estable en minúsculas del día de la semana.</summary>
    public static string ToCode(DayOfWeek weekday) => weekday.ToString().ToLowerInvariant();

    /// <summary>
    /// Convierte un código de día de la semana al día del dominio. Devuelve <c>null</c> si el código
    /// no es uno de los siete —que es como son, en minúsculas— para que quien valide la entrada lo
    /// pueda rechazar.
    /// </summary>
    public static DayOfWeek? ToWeekday(string? code) =>
        code is not null && ByCode.TryGetValue(code, out var weekday) ? weekday : null;
}
