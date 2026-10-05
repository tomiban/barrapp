namespace Barrapp.Domain.Knowledge;

/// <summary>
/// Bloque de una rutina de programa: un circuito de <see cref="Rounds"/> vueltas con sus filas y
/// el descanso entre vueltas.
/// </summary>
public sealed class RoutineBlock
{
    /// <summary>Nombre del bloque para la UI, en español.</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>Vueltas del circuito; debe ser mayor o igual que 1.</summary>
    public int Rounds { get; init; }

    /// <summary>Descanso entre vueltas, en segundos; no negativo.</summary>
    public int RestSeconds { get; init; }

    /// <summary>Notas para la UI, en español; opcional.</summary>
    public string? Notes { get; init; }

    /// <summary>Filas del bloque.</summary>
    public IReadOnlyList<RoutineItem> Items { get; init; } = [];
}
