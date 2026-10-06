using Barrapp.Application.Abstractions;

namespace Barrapp.Application.Features.SoloSessions;

/// <summary>
/// Genera una sesión suelta con los parámetros del atleta: tiempo (15/30/45/60 min), energía
/// (baja/media/alta), foco (patrón, skill o «sorpréndeme») y, para el foco de patrón, el grupo.
/// Es también el DTO de entrada del endpoint <c>POST /sessions/suelta</c>.
/// </summary>
/// <param name="TimeMinutes">Tiempo disponible: 15, 30, 45 o 60 minutos.</param>
/// <param name="Energy">Energía declarada: <c>baja</c>, <c>media</c> o <c>alta</c>.</param>
/// <param name="Focus">Foco de la sesión: <c>patron</c>, <c>skill</c> o <c>sorprendeme</c>.</param>
/// <param name="Pattern">Grupo del foco de patrón: <c>push</c>, <c>pull</c> o <c>leg</c>; obligatorio
/// cuando <paramref name="Focus"/> es <c>patron</c> y <c>null</c> en el resto.</param>
public sealed record GenerateSoloSessionCommand(
    int TimeMinutes,
    string Energy,
    string Focus,
    string? Pattern = null) : ICommand<SoloSessionResponse>;
