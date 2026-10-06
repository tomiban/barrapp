using Barrapp.Application.Abstractions;
using Barrapp.Application.Common;
using Barrapp.Application.Features.Catalog;
using Barrapp.Application.Features.Plans;
using Barrapp.Domain.Common;
using Barrapp.Domain.Knowledge;
using Barrapp.Domain.Planning;
using Microsoft.EntityFrameworkCore;

namespace Barrapp.Application.Features.SoloSessions;

/// <summary>
/// Carga el perfil (con sus máximos), el objetivo y la etapa actual del atleta en ese skill, y
/// delega la composición en el motor de dominio. La regla de programación vive entera en
/// <see cref="SoloSessionGenerator"/>; el handler solo orquesta y proyecta a DTO. Igual que el
/// plan (<see cref="Barrapp.Application.Features.Plans.GetPlanQueryHandler"/>), es la excepción
/// pragmática a «las queries proyectan directo a DTO»: el motor necesita el agregado del atleta
/// como entrada.
/// </summary>
/// <remarks>
/// La sesión suelta no se persiste en este ticket (#28): el historial y el aislamiento del
/// mesociclo llegan con #29.
/// </remarks>
internal sealed class GenerateSoloSessionCommandHandler(IApplicationDbContext dbContext, IKnowledgeBase catalog)
    : ICommandHandler<GenerateSoloSessionCommand, SoloSessionResponse>
{
    public async Task<Result<SoloSessionResponse>> Handle(
        GenerateSoloSessionCommand request,
        CancellationToken cancellationToken)
    {
        var profile = await dbContext.AthleteProfiles
            .Include(candidate => candidate.Maximums)
            .FirstOrDefaultAsync(candidate => candidate.UserId == SingleUser.Id, cancellationToken);

        if (profile is null)
        {
            return Result.Failure<SoloSessionResponse>(DomainErrors.AthleteProfile.NotFound);
        }

        var objective = await dbContext.Objectives
            .FirstOrDefaultAsync(candidate => candidate.UserId == SingleUser.Id, cancellationToken);

        if (objective is null)
        {
            return Result.Failure<SoloSessionResponse>(DomainErrors.Objective.NotFound);
        }

        var progress = await dbContext.AthleteSkillProgresses
            .FirstOrDefaultAsync(
                candidate => candidate.UserId == SingleUser.Id && candidate.SkillId == objective.SkillId,
                cancellationToken);

        var parameters = ToParameters(request);
        var generation = SoloSessionGenerator.Generate(profile, objective, progress?.StageOrder, parameters, catalog);
        if (generation.IsFailure)
        {
            return Result.Failure<SoloSessionResponse>(generation.Error);
        }

        // El foco resuelto (sobre todo para «sorpréndeme») va en la respuesta: el motor ya lo ha
        // validado, así que el segundo pase es puro y nunca vuelve a fallar.
        var composition = SoloSessionGenerator.ResolveFocus(parameters, objective).Value;

        return Result.Success(ToResponse(request, composition, generation.Value));
    }

    private static SoloSessionParameters ToParameters(GenerateSoloSessionCommand command) =>
        new(
            command.TimeMinutes switch
            {
                15 => SoloSessionTime.Minutes15,
                30 => SoloSessionTime.Minutes30,
                45 => SoloSessionTime.Minutes45,
                60 => SoloSessionTime.Minutes60,
                _ => throw new ArgumentOutOfRangeException(nameof(command), "Tiempo fuera del vocabulario."),
            },
            command.Energy switch
            {
                "baja" => SoloSessionEnergy.Low,
                "media" => SoloSessionEnergy.Medium,
                "alta" => SoloSessionEnergy.High,
                _ => throw new ArgumentOutOfRangeException(nameof(command), "Energía fuera del vocabulario."),
            },
            command.Focus switch
            {
                "patron" => SoloSessionFocus.Pattern,
                "skill" => SoloSessionFocus.Skill,
                "sorprendeme" => SoloSessionFocus.Surprise,
                _ => throw new ArgumentOutOfRangeException(nameof(command), "Foco fuera del vocabulario."),
            },
            command.Pattern switch
            {
                "push" => ExerciseGroup.Push,
                "pull" => ExerciseGroup.Pull,
                "leg" => ExerciseGroup.Leg,
                _ => null,
            });

    private SoloSessionResponse ToResponse(
        GenerateSoloSessionCommand request,
        SoloSessionComposition composition,
        Session session) =>
        new(
            request.Focus,
            composition.Pattern is null ? null : CatalogMappings.ToCode(composition.Pattern.Value),
            composition.SkillId,
            composition.SkillId is null
                ? null
                : catalog.FindSkill(composition.SkillId)?.Name ?? composition.SkillId,
            request.TimeMinutes,
            request.Energy,
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
