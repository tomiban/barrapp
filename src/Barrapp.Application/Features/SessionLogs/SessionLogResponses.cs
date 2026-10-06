using Barrapp.Application.Abstractions;
using Barrapp.Application.Features.Catalog;
using Barrapp.Domain.Knowledge;
using Barrapp.Domain.Sessions;

namespace Barrapp.Application.Features.SessionLogs;

/// <summary>
/// Proyección compartida de un <see cref="SessionLog"/> a su DTO. El nombre y la unidad del
/// ejercicio pueden venir ya resueltos (command) o resolverse contra el catálogo al proyectar
/// (query), como hace el plan.
/// </summary>
internal static class SessionLogResponses
{
    /// <summary>Proyecta un registro resolviendo el ejercicio contra el catálogo.</summary>
    public static SessionLogResponse From(SessionLog log, IKnowledgeBase knowledgeBase)
    {
        var exercise = knowledgeBase.FindExercise(log.ExerciseId);

        return new SessionLogResponse(
            log.Id,
            log.ExerciseId,
            exercise?.Name ?? log.ExerciseId,
            exercise is null ? string.Empty : CatalogMappings.ToCode(exercise.Metric),
            log.MesocycleId,
            log.SessionDay,
            log.RecordedAtUtc,
            ToSets(log));
    }

    /// <summary>Proyecta un registro con el ejercicio ya resuelto.</summary>
    public static SessionLogResponse From(SessionLog log, string exerciseName, Metric metric) => new(
        log.Id,
        log.ExerciseId,
        exerciseName,
        CatalogMappings.ToCode(metric),
        log.MesocycleId,
        log.SessionDay,
        log.RecordedAtUtc,
        ToSets(log));

    private static IReadOnlyList<SessionLogSetResponse> ToSets(SessionLog log) => log.Sets
        .Select(set => new SessionLogSetResponse(set.SetNumber, set.Value, set.Effort))
        .ToList();
}
