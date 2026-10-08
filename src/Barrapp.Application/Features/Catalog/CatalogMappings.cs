using Barrapp.Domain.Knowledge;
using Barrapp.Domain.Planning;
using Barrapp.Domain.Sessions;

namespace Barrapp.Application.Features.Catalog;

/// <summary>
/// Mapeos compartidos del catálogo: proyecta los tipos del dominio a los DTOs que consume la app.
/// Centraliza la proyección de una fila de rutina, los mapas a minúsculas y los códigos estables de
/// rol de sesión (<see cref="ToCode(SessionItemRole)"/>) para que los handlers de catálogo, plan y
/// sesión suelta no los repitan (única casa del vocabulario de cable).
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

    /// <summary>
    /// Convierte el origen de una sesión registrada a su código en minúsculas (<c>mesocycle</c> o
    /// <c>suelta</c>). Es el vocabulario de cable que comparten el registro de sesión y la app.
    /// </summary>
    public static string ToCode(SessionLogKind kind) => kind switch
    {
        SessionLogKind.Mesocycle => "mesocycle",
        SessionLogKind.Suelta => "suelta",
        _ => throw new ArgumentOutOfRangeException(nameof(kind)),
    };

    /// <summary>Convierte un tipo de programa a su código en minúsculas (<c>circuit</c> o <c>strength</c>).</summary>
    public static string ToCode(RoutineProgramType type) => type.ToString().ToLowerInvariant();

    /// <summary>
    /// Convierte el rol de una fila de sesión a su código estable en minúsculas
    /// (<c>skill</c>, <c>strength</c> o <c>core</c>). Es el vocabulario de cable compartido por el
    /// plan, la sesión suelta y su historial; la propia máquina de roles no se expone al cliente.
    /// </summary>
    public static string ToCode(SessionItemRole role) => role switch
    {
        SessionItemRole.Skill => "skill",
        SessionItemRole.Strength => "strength",
        SessionItemRole.Core => "core",
        _ => throw new ArgumentOutOfRangeException(nameof(role)),
    };
}
