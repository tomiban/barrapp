using Barrapp.Application.Abstractions;

namespace Barrapp.Application.Features.SkillProgress;

/// <summary>
/// Devuelve la etapa actual del atleta en todos los skills del catálogo. Un skill sin progreso
/// guardado se lee en la etapa 1.
/// </summary>
public sealed record GetSkillProgressQuery : IQuery<IReadOnlyList<SkillProgressResponse>>;
