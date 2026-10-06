using Barrapp.Application.Abstractions;
using Barrapp.Application.Common;
using Barrapp.Application.Features.Catalog;
using Barrapp.Application.Features.Plans;
using Barrapp.Domain.Common;
using Barrapp.Domain.Knowledge;
using Barrapp.Domain.Planning;
using Barrapp.Domain.Sessions;
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
/// Desde #29 la suelta generada queda guardada en el historial (<see cref="SessionSuelta"/>) en su
/// propia tabla, etiquetada como tal y aislada de los flujos de <see cref="SessionLog"/>: no altera
/// el mesociclo ni los máximos. La respuesta sigue siendo la sesión compuesta, como en #28.
/// </remarks>
internal sealed class GenerateSoloSessionCommandHandler(
    IApplicationDbContext dbContext,
    IKnowledgeBase catalog,
    ISessionSueltaRepository repository,
    IUnitOfWork unitOfWork)
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

        // El foco resuelto (sobre todo para «sorpréndeme») va en la respuesta. El motor ya lo ha
        // validado dentro de Generate, así que este segundo pase es puro y no debería fallar; se
        // guarda igualmente el resultado por si el dominio cambia la regla (FIX-6).
        var composition = SoloSessionGenerator.ResolveFocus(parameters, objective);
        if (composition.IsFailure)
        {
            return Result.Failure<SoloSessionResponse>(composition.Error);
        }

        var history = SessionSuelta.Create(
            SingleUser.Id,
            request.TimeMinutes,
            SueltaCodes.FromEnergy(request.Energy),
            SueltaCodes.FromFocus(request.Focus),
            composition.Value.Pattern,
            composition.Value.SkillId,
            DateTimeOffset.UtcNow,
            generation.Value.Items.Select(item => new SessionSueltaItemInput(
                item.ExerciseId,
                item.Role,
                item.Pattern,
                item.Sets,
                item.RepsMin,
                item.RepsMax,
                item.HoldSecondsMin,
                item.HoldSecondsMax,
                item.Note)).ToList());
        if (history.IsFailure)
        {
            return Result.Failure<SoloSessionResponse>(history.Error);
        }

        repository.Add(history.Value);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(ToResponse(request, composition.Value, generation.Value));
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
            SueltaCodes.FromEnergy(command.Energy),
            SueltaCodes.FromFocus(command.Focus),
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
        CatalogMappings.ToCode(item.Role),
        item.Pattern is null ? null : CatalogMappings.ToCode(item.Pattern.Value),
        item.Sets,
        item.RepsMin,
        item.RepsMax,
        item.HoldSecondsMin,
        item.HoldSecondsMax,
        item.Note);
}
