using Barrapp.Application.Abstractions;
using Barrapp.Application.Features.Catalog;
using Barrapp.Domain.Sessions;

namespace Barrapp.Application.Features.SessionLogs;

/// <summary>
/// Proyección compartida de un <see cref="SessionLog"/> a sus DTO. No consulta el catálogo: el
/// nombre y la unidad viajan en la foto del ítem (ADR-0014), de modo que el historial se lee igual
/// aunque la base de conocimiento cambie.
/// </summary>
internal static class SessionLogResponses
{
    /// <summary>Proyecta el registro con todos sus ítems.</summary>
    public static SessionLogResponse From(SessionLog log) => new(
        log.Id,
        CatalogMappings.ToCode(log.Kind),
        log.SessionDate,
        log.MesocycleId,
        log.MicrocycleNumber,
        log.SessionDay,
        log.RecordedAtUtc,
        log.CompletedAtUtc,
        log.Items
            .OrderBy(item => item.Position)
            .Select(item => From(item))
            .ToList());

    /// <summary>Proyecta un solo ítem registrado.</summary>
    public static SessionLogItemResponse From(SessionLogItem item) => new(
        item.Id,
        item.SessionLogId,
        item.Position,
        item.ExerciseId,
        item.ExerciseName,
        CatalogMappings.ToCode(item.Role),
        item.Pattern is null ? null : CatalogMappings.ToCode(item.Pattern.Value),
        CatalogMappings.ToCode(item.Metric),
        new SessionLogObjectiveResponse(
            item.PrescribedSets,
            item.RepsMin,
            item.RepsMax,
            item.HoldSecondsMin,
            item.HoldSecondsMax),
        item.Note,
        ToSets(item));

    /// <summary>
    /// Proyecta un ítem con la marca de pendiente de la cola local (#26), para que la app lo pinte
    /// igual que uno confirmado y solo se lo ofrezca como propio.
    /// </summary>
    public static SessionLogItemResponse Pending(SessionLogItem item) =>
        From(item) with { Pending = true };

    private static IReadOnlyList<SessionLogSetResponse> ToSets(SessionLogItem item) => item.Sets
        .OrderBy(set => set.SetNumber)
        .Select(set => new SessionLogSetResponse(
            set.SetNumber,
            set.Value,
            CatalogMappings.ToCode(item.Metric),
            set.ActualRir,
            set.LoadKg))
        .ToList();
}
