using Barrapp.Application.Abstractions;

namespace Barrapp.Application.Features.SoloSessions;

/// <summary>
/// Lista las sesiones sueltas del atleta, de la más reciente a la más antigua. Sin sueltas todavía,
/// devuelve una lista vacía.
/// </summary>
public sealed record GetSueltaHistoryQuery : IQuery<IReadOnlyList<SessionSueltaResponse>>;