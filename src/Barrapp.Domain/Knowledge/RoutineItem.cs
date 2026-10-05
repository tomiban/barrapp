namespace Barrapp.Domain.Knowledge;

/// <summary>
/// Fila de una rutina (de skill o de programa general): un ejercicio con sus series, su rango de
/// repeticiones o de segundos, el descanso y, opcionalmente, cadencia, superserie y notas.
/// </summary>
/// <remarks>
/// Compartido por <see cref="PatternRoutine"/> y <see cref="RoutineBlock"/>. Debe declarar
/// <b>al menos un rango</b> completo (<see cref="RepsMin"/>/<see cref="RepsMax"/> o
/// <see cref="HoldSecondsMin"/>/<see cref="HoldSecondsMax"/>).
/// </remarks>
public sealed class RoutineItem
{
    /// <summary>Ejercicio de la fila; debe existir en el catálogo.</summary>
    public string ExerciseId { get; init; } = string.Empty;

    /// <summary>Número de series; debe ser mayor que 0.</summary>
    public int Sets { get; init; }

    /// <summary>Repeticiones mínimas del rango; opcional.</summary>
    public int? RepsMin { get; init; }

    /// <summary>Repeticiones máximas del rango; opcional.</summary>
    public int? RepsMax { get; init; }

    /// <summary>Segundos mantenidos mínimos del rango; opcional.</summary>
    public int? HoldSecondsMin { get; init; }

    /// <summary>Segundos mantenidos máximos del rango; opcional.</summary>
    public int? HoldSecondsMax { get; init; }

    /// <summary>Descanso tras la serie (o tras el grupo de superserie), en segundos; no negativo.</summary>
    public int RestSeconds { get; init; }

    /// <summary>Cadencia opcional (positiva : isométrica : excéntrica).</summary>
    public string? Tempo { get; init; }

    /// <summary>
    /// Entero que agrupa filas <b>consecutivas</b> en una superserie. Un mismo valor no puede
    /// reabrirse tras un grupo distinto.
    /// </summary>
    public int? SupersetGroup { get; init; }

    /// <summary>Notas para la UI, en español; opcional.</summary>
    public string? Notes { get; init; }
}
