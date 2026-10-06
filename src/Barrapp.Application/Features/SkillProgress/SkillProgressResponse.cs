namespace Barrapp.Application.Features.SkillProgress;

/// <summary>
/// Etapa actual del atleta en un skill, tal y como la consume la app: el slug del skill y el
/// orden de su etapa actual dentro de la escalera.
/// </summary>
/// <param name="SkillId">Slug del skill, siempre presente en el catálogo.</param>
/// <param name="StageOrder">Orden de la etapa actual, consecutiva desde 1.</param>
public sealed record SkillProgressResponse(string SkillId, int StageOrder);
