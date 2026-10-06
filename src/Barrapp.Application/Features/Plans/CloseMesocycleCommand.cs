using Barrapp.Application.Abstractions;

namespace Barrapp.Application.Features.Plans;

/// <summary>
/// Cierra el mesociclo en curso del atleta (spec 0001, US-24; ticket #24, D7): lo pasa a
/// <see cref="Barrapp.Domain.Planning.MesocycleStatus.Closed"/> (entra en el historial), ajusta
/// los máximos del perfil con las sesiones registradas del mesociclo y devuelve el estado del
/// cierre con los máximos ya ajustados. Sin un mesociclo activo, el cierre sintetiza el mesociclo
/// que se cierra desde el perfil y el objetivo (FIX-1): la app solo genera on-read con
/// <c>GET /plan</c> y aun así el cierre funciona de extremo a extremo. Falla con <c>Not Found</c>
/// si no hay perfil, si al sintetizar no hay objetivo guardado, o con el error del motor si el
/// plan no se puede generar.
/// </summary>
public sealed record CloseMesocycleCommand : ICommand<CloseMesocycleResponse>;
