using Barrapp.Application.Abstractions;
using Barrapp.Application.Features.Catalog;
using Barrapp.Application.Features.Plans;
using Barrapp.Domain.Knowledge;
using Barrapp.Domain.Planning;
using Barrapp.Domain.Sessions;

namespace Barrapp.Application.Features.SoloSessions;

/// <summary>
/// Traducción del vocabulario de la sesión suelta a los códigos del API (y viceversa). Es la única
/// casa de los códigos de cable <c>baja/media/alta</c> (energía) y <c>patron/skill/sorprendeme</c>
/// (foco): tanto la generación (código → dominio) como la respuesta y el historial (dominio →
/// código) pasan por aquí, sin switches duplicados.
/// </summary>
internal static class SueltaCodes
{
    public static string ToEnergy(SoloSessionEnergy energy) => energy switch
    {
        SoloSessionEnergy.Low => "baja",
        SoloSessionEnergy.Medium => "media",
        SoloSessionEnergy.High => "alta",
        _ => throw new ArgumentOutOfRangeException(nameof(energy)),
    };

    public static string ToFocus(SoloSessionFocus focus) => focus switch
    {
        SoloSessionFocus.Pattern => "patron",
        SoloSessionFocus.Skill => "skill",
        SoloSessionFocus.Surprise => "sorprendeme",
        _ => throw new ArgumentOutOfRangeException(nameof(focus)),
    };

    public static string ToStatus(SessionSueltaStatus status) => status switch
    {
        SessionSueltaStatus.Generated => "generada",
        SessionSueltaStatus.Recorded => "registrada",
        _ => throw new ArgumentOutOfRangeException(nameof(status)),
    };

    public static SoloSessionEnergy FromEnergy(string energy) => energy switch
    {
        "baja" => SoloSessionEnergy.Low,
        "media" => SoloSessionEnergy.Medium,
        "alta" => SoloSessionEnergy.High,
        _ => throw new ArgumentOutOfRangeException(nameof(energy), "Energía fuera del vocabulario."),
    };

    public static SoloSessionFocus FromFocus(string focus) => focus switch
    {
        "patron" => SoloSessionFocus.Pattern,
        "skill" => SoloSessionFocus.Skill,
        "sorprendeme" => SoloSessionFocus.Surprise,
        _ => throw new ArgumentOutOfRangeException(nameof(focus), "Foco fuera del vocabulario."),
    };
}

/// <summary>
/// Proyección compartida de una <see cref="SessionSuelta"/> a su DTO de historial, resolviendo el
/// nombre del skill y de cada ejercicio contra el catálogo igual que hace el plan.
/// </summary>
internal static class SueltaResponses
{
    public static SessionSueltaResponse From(SessionSuelta suelta, IKnowledgeBase catalog)
    {
        return new SessionSueltaResponse(
            suelta.Id,
            suelta.TimeMinutes,
            SueltaCodes.ToEnergy(suelta.Energy),
            SueltaCodes.ToFocus(suelta.Focus),
            suelta.Pattern is null ? null : CatalogMappings.ToCode(suelta.Pattern.Value),
            suelta.SkillId,
            suelta.SkillId is null ? null : catalog.FindSkill(suelta.SkillId)?.Name ?? suelta.SkillId,
            SueltaCodes.ToStatus(suelta.Status),
            suelta.CreatedAtUtc,
            suelta.RecordedAtUtc,
            suelta.Items.OrderBy(item => item.Position).Select(item => ToItem(item, catalog)).ToList());
    }

    private static SessionItemResponse ToItem(SessionSueltaItem item, IKnowledgeBase catalog) => new(
        item.ExerciseId,
        catalog.FindExercise(item.ExerciseId)?.Name ?? item.ExerciseId,
        CatalogMappings.ToCode(item.Role),
        item.Pattern is null ? null : CatalogMappings.ToCode(item.Pattern.Value),
        item.Sets,
        item.RepsMin,
        item.RepsMax,
        item.HoldSecondsMin,
        item.HoldSecondsMax,
        item.Note);
}
