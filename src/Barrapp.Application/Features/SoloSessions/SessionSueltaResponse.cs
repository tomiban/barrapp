using Barrapp.Application.Abstractions;
using Barrapp.Application.Features.Plans;

namespace Barrapp.Application.Features.SoloSessions;

/// <summary>
/// Entrada del historial de sesiones sueltas (#29): los parámetros con los que se pidió, el foco ya
/// resuelto por el motor y el estado de la suelta (generada o registrada). Cada suelta queda
/// etiquetada como tal y no participa en el avance de etapa ni en el ajuste de máximos.
/// </summary>
/// <param name="Id">Identificador de la suelta en el historial.</param>
/// <param name="TimeMinutes">Tiempo disponible con el que se generó.</param>
/// <param name="Energy">Energía con la que se generó: <c>baja</c>, <c>media</c> o <c>alta</c>.</param>
/// <param name="Focus">Foco con el que se pidió: <c>patron</c>, <c>skill</c> o <c>sorprendeme</c>.</param>
/// <param name="Pattern">Patrón resuelto (<c>push</c>, <c>pull</c> o <c>leg</c>) o <c>null</c> en skill.</param>
/// <param name="SkillId">Slug del skill resuelto o <c>null</c> en patrón.</param>
/// <param name="SkillName">Nombre del skill resuelto para la UI, en español; <c>null</c> en patrón.</param>
/// <param name="Status">Estado de la suelta: <c>generada</c> o <c>registrada</c>.</param>
/// <param name="CreatedAtUtc">Momento en el que se generó (UTC).</param>
/// <param name="RecordedAtUtc">Momento en el que se marcó como registrada; <c>null</c> si sigue generada.</param>
/// <param name="Items">Filas de la sesión guardada, en orden de ejecución.</param>
public sealed record SessionSueltaResponse(
    Guid Id,
    int TimeMinutes,
    string Energy,
    string Focus,
    string? Pattern,
    string? SkillId,
    string? SkillName,
    string Status,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? RecordedAtUtc,
    IReadOnlyList<SessionItemResponse> Items);