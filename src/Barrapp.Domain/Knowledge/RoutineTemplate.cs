namespace Barrapp.Domain.Knowledge;

/// <summary>
/// Rutina concreta de un programa general: un modelo con su intensidad, duración estimada y sus
/// bloques.
/// </summary>
public sealed class RoutineTemplate
{
    /// <summary>Identificador único dentro del programa.</summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>Nombre de la rutina para la UI, en español.</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>Intensidad del modelo (2 o 3).</summary>
    public int Intensity { get; init; }

    /// <summary>Duración estimada en minutos; opcional.</summary>
    public int? DurationMinutes { get; init; }

    /// <summary>Bloques de la rutina; no puede estar vacía.</summary>
    public IReadOnlyList<RoutineBlock> Blocks { get; init; } = [];
}
