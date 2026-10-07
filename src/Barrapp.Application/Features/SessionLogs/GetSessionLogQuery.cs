using Barrapp.Application.Abstractions;

namespace Barrapp.Application.Features.SessionLogs;

/// <summary>
/// Abre un registro de sesión por su identificador: la cabecera con su clave de sesión determinista,
/// su marca de completada y sus ítems con la foto y las series. Sin registro, devuelve
/// <c>Not Found</c> a través del handler.
/// </summary>
/// <param name="SessionLogId">Identificador del registro de sesión.</param>
public sealed record GetSessionLogQuery(Guid SessionLogId) : IQuery<SessionLogResponse>;
