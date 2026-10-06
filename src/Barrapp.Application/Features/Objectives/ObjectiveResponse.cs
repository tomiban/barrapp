namespace Barrapp.Application.Features.Objectives;

/// <summary>
/// Objetivo del mesociclo tal y como lo consume la app: el slug del skill elegido.
/// </summary>
/// <param name="SkillId">Slug del skill objetivo, siempre presente en el catálogo.</param>
public sealed record ObjectiveResponse(string SkillId);
