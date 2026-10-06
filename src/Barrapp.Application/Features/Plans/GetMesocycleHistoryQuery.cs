using Barrapp.Application.Abstractions;

namespace Barrapp.Application.Features.Plans;

/// <summary>Lista el historial de mesociclos cerrados, más reciente primero (ticket #27).</summary>
public sealed record GetMesocycleHistoryQuery : IQuery<IReadOnlyList<MesocycleSummaryResponse>>;
