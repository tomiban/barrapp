namespace Barrapp.Application.Features.Ping;

/// <summary>
/// Respuesta del endpoint <c>GET /ping</c>.
/// </summary>
/// <param name="Message">Confirmación fija del API.</param>
/// <param name="ServerTimeUtc">Hora del servidor, útil para comprobar la conexión desde la app.</param>
public sealed record PingResponse(string Message, DateTimeOffset ServerTimeUtc);
