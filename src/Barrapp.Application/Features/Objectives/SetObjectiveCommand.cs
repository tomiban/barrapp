using Barrapp.Application.Abstractions;

namespace Barrapp.Application.Features.Objectives;

/// <summary>
/// Fija el skill objetivo del mesociclo: crea el objetivo si no existe o cambia su skill si ya
/// existe. Es también el DTO de entrada del endpoint: el slug del skill elegido.
/// </summary>
/// <param name="SkillId">Slug del skill objetivo; debe existir en el catálogo.</param>
public sealed record SetObjectiveCommand(string SkillId) : ICommand<ObjectiveResponse>;
