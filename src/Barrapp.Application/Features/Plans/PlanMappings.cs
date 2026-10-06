using Barrapp.Application.Abstractions;
using Barrapp.Application.Features.Catalog;
using Barrapp.Domain.Planning;

namespace Barrapp.Application.Features.Plans;

/// <summary>
/// Proyecta el <see cref="Plan"/> del dominio al DTO que consume la app, resolviendo el nombre en
/// español de cada ejercicio contra el catálogo. Los roles y patrones viajan como códigos estables
/// en minúsculas.
/// </summary>
internal static class PlanMappings
{
    public static PlanResponse ToResponse(Plan plan, IKnowledgeBase catalog) => new(
        plan.SkillId,
        plan.TrainingDays,
        plan.Microcycles.Select(microcycle => ToMicrocycle(microcycle, catalog)).ToList());

    private static MicrocycleResponse ToMicrocycle(Microcycle microcycle, IKnowledgeBase catalog) => new(
        microcycle.Number,
        microcycle.Sessions.Select(session => ToSession(session, catalog)).ToList());

    private static SessionResponse ToSession(Session session, IKnowledgeBase catalog) => new(
        session.Day,
        session.Items.Select(item => ToItem(item, catalog)).ToList());

    private static SessionItemResponse ToItem(SessionItem item, IKnowledgeBase catalog) => new(
        item.ExerciseId,
        catalog.FindExercise(item.ExerciseId)?.Name ?? item.ExerciseId,
        ToCode(item.Role),
        item.Pattern is null ? null : CatalogMappings.ToCode(item.Pattern.Value),
        item.Sets,
        item.RepsMin,
        item.RepsMax,
        item.HoldSecondsMin,
        item.HoldSecondsMax,
        item.Note);

    private static string ToCode(SessionItemRole role) => role switch
    {
        SessionItemRole.Skill => "skill",
        SessionItemRole.Strength => "strength",
        SessionItemRole.Core => "core",
        _ => throw new ArgumentOutOfRangeException(nameof(role)),
    };
}
