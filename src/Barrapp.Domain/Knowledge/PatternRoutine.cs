namespace Barrapp.Domain.Knowledge;

/// <summary>
/// Rutina de patrón de un skill: un modelo de trabajo (por intensidad y material) con sus filas.
/// </summary>
public sealed class PatternRoutine
{
    /// <summary>Identificador único dentro del skill.</summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>Nombre del modelo para la UI, en español.</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>Intensidad del modelo (2 o 3).</summary>
    public int Intensity { get; init; }

    /// <summary>Material necesario; opcional.</summary>
    public string? Equipment { get; init; }

    /// <summary>Filas de la rutina.</summary>
    public IReadOnlyList<RoutineItem> Items { get; init; } = [];
}
