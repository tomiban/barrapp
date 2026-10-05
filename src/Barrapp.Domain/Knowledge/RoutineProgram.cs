namespace Barrapp.Domain.Knowledge;

/// <summary>
/// Programa general de acondicionamiento: agrupa rutinas de circuito (por tiempo) o de fuerza
/// (por repeticiones).
/// </summary>
public sealed class RoutineProgram
{
    /// <summary>Slug estable y único entre programas.</summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>Nombre para la UI, en español.</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>Tipo de programa: circuito o fuerza.</summary>
    public RoutineProgramType Type { get; init; }

    /// <summary>Descripción para la UI, en español; opcional.</summary>
    public string? Description { get; init; }

    /// <summary>Rutinas del programa; al menos una.</summary>
    public IReadOnlyList<RoutineTemplate> Routines { get; init; } = [];
}
