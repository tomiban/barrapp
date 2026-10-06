using Barrapp.Application.Abstractions;

namespace Barrapp.Application.Features.SessionLogs;

/// <summary>
/// Lista los registros de sesión del atleta con sus ítems y su foto (ADR-0014), de la fecha más
/// reciente a la más antigua. Sin registros todavía, devuelve una lista vacía.
/// </summary>
public sealed record GetSessionLogsQuery : IQuery<IReadOnlyList<SessionLogResponse>>;
