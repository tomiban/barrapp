using Barrapp.Domain.Knowledge;

namespace Barrapp.Application.Features.Catalog;

/// <summary>
/// Mapeos compartidos del catálogo: proyecta los tipos del dominio a los DTOs que consume la app.
/// Centraliza la proyección de una fila de rutina y los mapas a minúsculas para que los handlers
/// de catálogo no los repitan.
/// </summary>
internal static class CatalogMappings
{
    /// <summary>Proyecta una fila de rutina a su DTO.</summary>
    public static RoutineItemResponse ToResponse(RoutineItem item) => new(
        item.ExerciseId,
        item.Sets,
        item.RepsMin,
        item.RepsMax,
        item.HoldSecondsMin,
        item.HoldSecondsMax,
        item.RestSeconds,
        item.Tempo,
        item.SupersetGroup,
        item.Notes);

    /// <summary>Convierte un patrón a su código en minúsculas (<c>push</c>, <c>pull</c>…).</summary>
    public static string ToCode(ExerciseGroup group) => group.ToString().ToLowerInvariant();

    /// <summary>Convierte una métrica a su código en minúsculas (<c>reps</c> o <c>seconds</c>).</summary>
    public static string ToCode(Metric metric) => metric.ToString().ToLowerInvariant();

    /// <summary>Convierte un tipo de programa a su código en minúsculas (<c>circuit</c> o <c>strength</c>).</summary>
    public static string ToCode(RoutineProgramType type) => type.ToString().ToLowerInvariant();
}
