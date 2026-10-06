namespace Barrapp.Domain.Knowledge;

/// <summary>
/// Criterio para superar una etapa de la escalera: la métrica, el objetivo a alcanzar y el
/// número de series en que hay que lograrlo.
/// </summary>
public sealed class StageCriterion
{
    /// <summary>Métrica del criterio (repeticiones o segundos).</summary>
    public Metric Metric { get; init; }

    /// <summary>Objetivo a alcanzar; debe ser mayor que 0.</summary>
    public int Target { get; init; }

    /// <summary>Series en las que hay que alcanzar el objetivo; debe ser mayor que 0.</summary>
    public int Sets { get; init; }
}
