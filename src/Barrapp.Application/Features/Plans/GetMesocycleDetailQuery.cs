using Barrapp.Application.Abstractions;

namespace Barrapp.Application.Features.Plans;

/// <summary>Abre un mesociclo del historial por su identificador (ticket #27).</summary>
/// <param name="MesocycleId">Identificador del mesociclo a abrir.</param>
public sealed record GetMesocycleDetailQuery(Guid MesocycleId) : IQuery<PlanResponse>;