using Barrapp.Application.Abstractions;

namespace Barrapp.Application.Features.Plans;

/// <summary>
/// Cierra el mesociclo en curso del atleta (spec 0001, US-24; ticket #24, D7): lo pasa a
/// <see cref="Barrapp.Domain.Planning.MesocycleStatus.Closed"/> (entra en el historial), ajusta
/// los máximos del perfil con las sesiones registradas del mesociclo y devuelve el estado del
/// cierre con los máximos ya ajustados. Falla con <c>Not Found</c> si no hay perfil y con
/// <c>Conflict</c> si no hay mesociclo en curso.
/// </summary>
public sealed record CloseMesocycleCommand : ICommand<CloseMesocycleResponse>;
