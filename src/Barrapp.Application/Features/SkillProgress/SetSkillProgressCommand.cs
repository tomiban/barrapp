using Barrapp.Application.Abstractions;

namespace Barrapp.Application.Features.SkillProgress;

/// <summary>
/// Fija la etapa actual del atleta en un skill: crea la progresión si no existe o cambia su etapa
/// si ya existe. Es también el DTO de entrada del endpoint.
/// </summary>
/// <param name="SkillId">Slug del skill; debe existir en el catálogo.</param>
/// <param name="StageOrder">Orden de la etapa dentro de la escalera del skill; debe existir.</param>
public sealed record SetSkillProgressCommand(string SkillId, int StageOrder)
    : ICommand<SkillProgressResponse>;
