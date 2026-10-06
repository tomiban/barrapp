using Barrapp.Application.Abstractions;

namespace Barrapp.Application.Features.SessionLogs;

/// <summary>
/// Lista los registros de sesión del atleta, en orden de día y ejercicio. Sin registros todavía,
/// devuelve una lista vacía.
/// </summary>
public sealed record GetSessionLogsQuery : IQuery<IReadOnlyList<SessionLogResponse>>;
