namespace Barrapp.Application.Features.SkillProgress;

/// <summary>
/// Resultado del avance de etapa de un skill: la etapa en la que queda el atleta tras evaluar los
/// registros y si el avance se produjo (criterio cumplido en dos sesiones consecutivas). Si no
/// avanza, <see cref="StageOrder"/> es la etapa actual, sin cambios.
/// </summary>
/// <param name="SkillId">Slug del skill evaluado.</param>
/// <param name="StageOrder">Etapa en la que queda el atleta tras la evaluación.</param>
/// <param name="Advanced"><c>true</c> si se subió de etapa; <c>false</c> si no.</param>
public sealed record SkillStageAdvanceResponse(string SkillId, int StageOrder, bool Advanced);