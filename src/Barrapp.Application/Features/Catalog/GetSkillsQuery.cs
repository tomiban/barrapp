using Barrapp.Application.Abstractions;

namespace Barrapp.Application.Features.Catalog;

/// <summary>
/// Devuelve los skills con su escalera de progresión y sus rutinas de patrón. Es solo lectura:
/// la base de conocimiento ya viene validada y en memoria.
/// </summary>
public sealed record GetSkillsQuery : IQuery<IReadOnlyList<SkillResponse>>;
