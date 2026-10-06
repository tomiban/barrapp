namespace Barrapp.Domain.SkillProgress;

/// <summary>
/// Resultado de la evaluación del avance de etapa del skill: la etapa en la que queda el atleta
/// tras la evaluación y si la evaluación lo sube (criterio cumplido en dos sesiones consecutivas).
/// Si no avanza, <see cref="StageOrder"/> es la etapa actual de salida, sin cambios.
/// </summary>
/// <param name="StageOrder">Etapa en la que queda el atleta tras la evaluación.</param>
/// <param name="Advanced"><c>true</c> si la evaluación sube de etapa; <c>false</c> si no.</param>
public sealed record StageAdvanceEvaluation(int StageOrder, bool Advanced);
