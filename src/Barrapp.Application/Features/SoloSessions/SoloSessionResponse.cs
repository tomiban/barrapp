using Barrapp.Application.Abstractions;
using Barrapp.Application.Common;
using Barrapp.Application.Features.Catalog;
using Barrapp.Application.Features.Plans;
using Barrapp.Domain.Common;
using Barrapp.Domain.Knowledge;
using Barrapp.Domain.Planning;

namespace Barrapp.Application.Features.SoloSessions;

/// <summary>
/// Sesión suelta tal y como la consume la app: los parámetros con los que se pidió —con el foco ya
/// resuelto, para que «sorpréndeme» diga qué ha tocado— y las filas de la sesión generada.
/// </summary>
/// <param name="Focus">Foco con el que se pidió: <c>pattern</c>, <c>skill</c> o <c>surprise</c>.</param>
/// <param name="Pattern">Patrón resuelto (<c>push</c>, <c>pull</c> o <c>leg</c>) o <c>null</c> cuando el
/// foco es de skill.</param>
/// <param name="SkillId">Slug del skill resuelto (el objetivo) o <c>null</c> cuando el foco es de patrón.</param>
/// <param name="SkillName">Nombre del skill resuelto para la UI, en español; <c>null</c> con foco de patrón.</param>
/// <param name="TimeMinutes">Tiempo disponible con el que se generó.</param>
/// <param name="Energy">Energía con la que se generó: <c>baja</c>, <c>media</c> o <c>alta</c>.</param>
/// <param name="Items">Filas de la sesión, en orden de ejecución.</param>
public sealed record SoloSessionResponse(
    string Focus,
    string? Pattern,
    string? SkillId,
    string? SkillName,
    int TimeMinutes,
    string Energy,
    IReadOnlyList<SessionItemResponse> Items);
