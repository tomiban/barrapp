namespace Barrapp.Domain.Knowledge;

/// <summary>
/// Etapa de la escalera de progresión de un skill: su orden (consecutivo desde 1), el ejercicio
/// que la representa, el criterio para superarla y notas para la UI.
/// </summary>
public sealed class SkillStage
{
    /// <summary>Posición en la escalera; consecutiva desde 1.</summary>
    public int Order { get; init; }

    /// <summary>Nombre de la etapa para la UI, en español.</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>Ejercicio que representa la etapa; debe existir en el catálogo.</summary>
    public string ExerciseId { get; init; } = string.Empty;

    /// <summary>Criterio para superar la etapa.</summary>
    public StageCriterion Criterion { get; init; } = new();

    /// <summary>Notas para la UI, en español.</summary>
    public string Notes { get; init; } = string.Empty;
}
