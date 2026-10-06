using Barrapp.Domain.Athlete;
using Barrapp.Domain.Common;
using Barrapp.Domain.Knowledge;
using Barrapp.Domain.Objectives;
using Barrapp.Domain.SkillProgress;

namespace Barrapp.Domain.Planning;

/// <summary>
/// Motor determinista de la sesión suelta: a partir del perfil, el objetivo y unos parámetros de
/// tiempo, energía y foco compone una sesión puntual contra el catálogo. Es puro: mismas entradas,
/// misma sesión.
/// </summary>
/// <remarks>
/// <para>
/// La costura pública del motor es <see cref="Generate"/> (ver `docs/architecture.md`,
/// «Interfaz de dominio»: <c>GenerarSesionSuelta(perfil, objetivo, parámetros) → Sesión</c>).
/// <see cref="ResolveFocus"/> es su compañera: materializa el foco resuelto (sobre todo para
/// «sorpréndeme») para que la respuesta del API pueda decir qué ha tocado, sin revelar internos.
/// </para>
/// <para>
/// Reglas de composición (#28): el <b>tiempo</b> fija cuántos huecos de trabajo entran —15 min → 1,
/// 30 → 2, 45 → 3, 60 → 4— y si el core de cierre aparece (solo desde 30 min). La <b>energía</b>
/// decide las series (baja → 2, media → 3, alta → 4) y la reserva (RIR 4, 3 y 2: a menos energía,
/// más repeticiones en reserva, sesión más leve). El <b>foco</b> filtra el catálogo: un patrón
/// trabaja la fuerza general de ese grupo con el ancla derivada del máximo del atleta y variantes
/// del mismo grupo; el skill abre con la etapa actual de su escalera (sin tocar su marca, solo
/// ajusta las series por energía) y sigue con sus rutinas de patrón; «sorpréndeme» elige entre
/// skill y patrones con una semilla fija derivada de las entradas, así que sigue siendo
/// determinista.
/// </para>
/// <para>
/// La sesión suelta se apoya en el mismo <see cref="StrengthLoad"/> del plan para nunca prescribir
/// al fallo. Reutiliza <see cref="Session"/> con un día fijo (1): la suelta está fuera del
/// mesociclo y el día no aplica.
/// </para>
/// </remarks>
public static class SoloSessionGenerator
{
    /// <summary>Ejercicio de core por defecto, mismo ancla que el plan.</summary>
    private const string CoreExerciseId = "hollow-body-hold";

    private const int CoreHoldSecondsMin = 20;
    private const int CoreHoldSecondsMax = 30;

    /// <summary>
    /// Genera la sesión suelta para <paramref name="profile"/> y <paramref name="objective"/>
    /// según <paramref name="parameters"/>. Falla con <see cref="DomainErrors.Objective.UnknownSkill"/>
    /// si el skill objetivo no está en el catálogo, con <see cref="DomainErrors.SessionSuelta"/> si
    /// el foco de patrón no trae un grupo válido, con <see cref="DomainErrors.Plan.UnknownStage"/>
    /// si el foco de skill pide una etapa que no está en la escalera y con un error de catálogo si
    /// falta un ejercicio o un máximo obligatorios.
    /// </summary>
    /// <param name="stageOrder">
    /// Etapa actual del atleta en el skill objetivo; <c>null</c> si aún no tiene progresión guardada,
    /// en cuyo caso se practica la primera etapa. Solo se usa cuando la composición es de skill.
    /// </param>
    public static Result<Session> Generate(
        AthleteProfile profile,
        Objective objective,
        int? stageOrder,
        SoloSessionParameters parameters,
        IGenerationCatalog catalog)
    {
        if (catalog.FindSkill(objective.SkillId) is null)
        {
            return Result.Failure<Session>(DomainErrors.Objective.UnknownSkill);
        }

        var composition = ResolveFocus(parameters, objective);
        if (composition.IsFailure)
        {
            return Result.Failure<Session>(composition.Error);
        }

        var items = new List<SessionItem>();

        if (composition.Value.Kind == SoloSessionCompositionKind.Skill)
        {
            var skillBlock = BuildSkillBlock(stageOrder, parameters.Energy, objective.SkillId, catalog);
            if (skillBlock.IsFailure)
            {
                return Result.Failure<Session>(skillBlock.Error);
            }

            items.AddRange(skillBlock.Value);
        }
        else
        {
            var patternBlock = BuildPatternBlock(profile, composition.Value.Pattern!.Value, parameters, catalog);
            if (patternBlock.IsFailure)
            {
                return Result.Failure<Session>(patternBlock.Error);
            }

            items.AddRange(patternBlock.Value);
        }

        if (parameters.Time >= SoloSessionTime.Minutes30)
        {
            var core = BuildCore(parameters.Energy, catalog);
            if (core.IsFailure)
            {
                return Result.Failure<Session>(core.Error);
            }

            items.Add(core.Value);
        }

        // La suelta es una sesión fuera del mesociclo: no ocupa un día del calendario, así que ni el día
        // de la semana ni la fecha aplican (#94).
        return Result.Success(new Session(day: 1, weekday: null, date: null, items));
    }

    /// <summary>
    /// Resuelve el foco de la sesión suelta: qué compone el motor. Para <see cref="SoloSessionFocus.Pattern"/>
    /// valida que el grupo sea un patrón (empuje, tirón o pierna); para «sorpréndeme» elige de forma
    /// determinista a partir de una semilla derivada de las entradas (tiempo, energía y skill del
    /// objetivo), por lo que el mismo atleta con los mismos parámetros siempre obtiene la misma
    /// elección pero el motor reparte entre skill y patrones.
    /// </summary>
    public static Result<SoloSessionComposition> ResolveFocus(
        SoloSessionParameters parameters,
        Objective objective)
    {
        switch (parameters.Focus)
        {
            case SoloSessionFocus.Skill:
                return Result.Success(new SoloSessionComposition(
                    SoloSessionCompositionKind.Skill,
                    Pattern: null,
                    objective.SkillId));

            case SoloSessionFocus.Pattern:
                var pattern = parameters.Pattern;
                if (pattern is null)
                {
                    return Result.Failure<SoloSessionComposition>(DomainErrors.SessionSuelta.PatternRequired);
                }

                if (!IsStrengthPattern(pattern.Value))
                {
                    return Result.Failure<SoloSessionComposition>(DomainErrors.SessionSuelta.UnsupportedPattern);
                }

                return Result.Success(new SoloSessionComposition(
                    SoloSessionCompositionKind.Pattern,
                    pattern,
                    SkillId: null));

            case SoloSessionFocus.Surprise:
                var pick = SurprisePick(parameters, objective.SkillId);
                return pick == 0
                    ? Result.Success(new SoloSessionComposition(
                        SoloSessionCompositionKind.Skill,
                        Pattern: null,
                        objective.SkillId))
                    : Result.Success(new SoloSessionComposition(
                        SoloSessionCompositionKind.Pattern,
                        GroupForSurprisePick(pick),
                        SkillId: null));

            default:
                throw new ArgumentOutOfRangeException(nameof(parameters));
        }
    }

    /// <summary>Bloque de skill: la etapa actual de la escalera, sin tocar su marca, con las series
    /// ajustadas por la energía.</summary>
    private static Result<IReadOnlyList<SessionItem>> BuildSkillBlock(
        int? stageOrder,
        SoloSessionEnergy energy,
        string skillId,
        IGenerationCatalog catalog)
    {
        var skill = catalog.FindSkill(skillId);
        if (skill is null)
        {
            return Result.Failure<IReadOnlyList<SessionItem>>(DomainErrors.Objective.UnknownSkill);
        }

        var stage = skill.Stages.FirstOrDefault(candidate =>
            candidate.Order == (stageOrder ?? AthleteSkillProgress.InitialStageOrder));
        if (stage is null)
        {
            return Result.Failure<IReadOnlyList<SessionItem>>(DomainErrors.Plan.UnknownStage);
        }

        var items = new List<SessionItem>
        {
            BuildSkillItem(stage, energy),
        };

        // La fuerza de acompañamiento viene de la primera rutina de patrón del skill (orden estable
        // del catálogo), traducida a filas de sesión con su grupo y su prescripción propia.
        var routine = skill.PatternRoutines.FirstOrDefault();
        if (routine is not null)
        {
            items.AddRange(routine.Items.Select(item => ToStrengthItem(item, catalog)).Where(item => item is not null)!);
        }

        return Result.Success<IReadOnlyList<SessionItem>>(items);
    }

    private static SessionItem BuildSkillItem(SkillStage stage, SoloSessionEnergy energy)
    {
        var isHold = stage.Criterion.Metric == Metric.Seconds;
        var sets = Math.Max(1, stage.Criterion.Sets + SkillSetAdjustment(energy));

        return new SessionItem(
            stage.ExerciseId,
            SessionItemRole.Skill,
            null,
            sets,
            isHold ? null : stage.Criterion.Target,
            isHold ? null : stage.Criterion.Target,
            isHold ? stage.Criterion.Target : null,
            isHold ? stage.Criterion.Target : null);
    }

    private static SessionItem? ToStrengthItem(RoutineItem item, IGenerationCatalog catalog) =>
        catalog.FindExercise(item.ExerciseId) is { } exercise
            ? new SessionItem(
                item.ExerciseId,
                SessionItemRole.Strength,
                exercise.Group,
                item.Sets,
                item.RepsMin,
                item.RepsMax,
                item.HoldSecondsMin,
                item.HoldSecondsMax,
                item.Notes)
            : null;

    /// <summary>Bloque de fuerza de un patrón: el ancla derivada del máximo del atleta y las
    /// variantes generales del mismo grupo que alcancen el tiempo disponible.</summary>
    private static Result<IReadOnlyList<SessionItem>> BuildPatternBlock(
        AthleteProfile profile,
        ExerciseGroup group,
        SoloSessionParameters parameters,
        IGenerationCatalog catalog)
    {
        var anchor = AnchorCode(group);
        if (catalog.FindExercise(anchor) is null)
        {
            return Result.Failure<IReadOnlyList<SessionItem>>(DomainErrors.Plan.UnknownExercise(anchor));
        }

        var maximum = profile.MaximumFor(anchor);
        if (maximum is null)
        {
            return Result.Failure<IReadOnlyList<SessionItem>>(DomainErrors.AthleteProfile.MissingExerciseMaximum);
        }

        var sets = SetsFor(parameters.Energy);
        var repsInReserve = RepsInReserveFor(parameters.Energy);
        var slots = SlotsFor(parameters.Time);

        var items = new List<SessionItem>
        {
            BuildAnchorItem(anchor, group, sets, maximum.Value, repsInReserve),
        };

        // Variantes del patrón en orden estable del catálogo, de acondicionamiento general y por
        // repeticiones; su carga se deriva del máximo del ancla, nunca al fallo.
        var extras = catalog.ExercisesByGroup(group)
            .Where(exercise =>
                exercise.Id != anchor
                && exercise.SkillId is null
                && exercise.Metric == Metric.Reps)
            .Take(slots - 1);

        foreach (var extra in extras)
        {
            var range = StrengthLoad.Derive(maximum.Value, repsInReserve);
            items.Add(new SessionItem(
                extra.Id,
                SessionItemRole.Strength,
                group,
                sets,
                range.Min,
                range.Max,
                null,
                null));
        }

        return Result.Success<IReadOnlyList<SessionItem>>(items);
    }

    private static SessionItem BuildAnchorItem(
        string anchor,
        ExerciseGroup group,
        int sets,
        int maximum,
        int repsInReserve)
    {
        var range = StrengthLoad.Derive(maximum, repsInReserve);

        return new SessionItem(
            anchor,
            SessionItemRole.Strength,
            group,
            sets,
            range.Min,
            range.Max,
            null,
            null);
    }

    private static Result<SessionItem> BuildCore(SoloSessionEnergy energy, IGenerationCatalog catalog)
    {
        if (catalog.FindExercise(CoreExerciseId) is null)
        {
            return Result.Failure<SessionItem>(DomainErrors.Plan.UnknownExercise(CoreExerciseId));
        }

        return Result.Success(new SessionItem(
            CoreExerciseId,
            SessionItemRole.Core,
            null,
            SetsFor(energy),
            null,
            null,
            CoreHoldSecondsMin,
            CoreHoldSecondsMax));
    }

    private static bool IsStrengthPattern(ExerciseGroup group) => group switch
    {
        ExerciseGroup.Push or ExerciseGroup.Pull or ExerciseGroup.Leg => true,
        _ => false,
    };

    /// <summary>Ancla del patrón: el ejercicio básico con máximo guardado del atleta.</summary>
    private static string AnchorCode(ExerciseGroup group) => group switch
    {
        ExerciseGroup.Push => BasicExercises.PushUp.Code,
        ExerciseGroup.Pull => BasicExercises.PullUp.Code,
        ExerciseGroup.Leg => BasicExercises.Squat.Code,
        _ => throw new ArgumentOutOfRangeException(nameof(group)),
    };

    private static int SlotsFor(SoloSessionTime time) => time switch
    {
        SoloSessionTime.Minutes15 => 1,
        SoloSessionTime.Minutes30 => 2,
        SoloSessionTime.Minutes45 => 3,
        SoloSessionTime.Minutes60 => 4,
        _ => throw new ArgumentOutOfRangeException(nameof(time)),
    };

    private static int SetsFor(SoloSessionEnergy energy) => energy switch
    {
        SoloSessionEnergy.Low => 2,
        SoloSessionEnergy.Medium => 3,
        SoloSessionEnergy.High => 4,
        _ => throw new ArgumentOutOfRangeException(nameof(energy)),
    };

    private static int RepsInReserveFor(SoloSessionEnergy energy) => energy switch
    {
        SoloSessionEnergy.Low => 4,
        SoloSessionEnergy.Medium => 3,
        SoloSessionEnergy.High => 2,
        _ => throw new ArgumentOutOfRangeException(nameof(energy)),
    };

    /// <summary>Ajuste de series del bloque de skill por energía (mínimo 1); la marca de la etapa
    /// nunca cambia.</summary>
    private static int SkillSetAdjustment(SoloSessionEnergy energy) => energy switch
    {
        SoloSessionEnergy.Low => -1,
        SoloSessionEnergy.Medium => 0,
        SoloSessionEnergy.High => 1,
        _ => throw new ArgumentOutOfRangeException(nameof(energy)),
    };

    /// <summary>Semilla estable de «sorpréndeme» a partir de las entradas: tiempo, energía y skill
    /// del objetivo. Sin dependencia del reloj, así el motor sigue siendo puro y determinista.</summary>
    private static int SeedFor(SoloSessionTime time, SoloSessionEnergy energy, string skillId)
    {
        unchecked
        {
            var hash = 17;
            hash = (hash * 31) + (int)time;
            hash = (hash * 31) + (int)energy;
            foreach (var character in skillId)
            {
                hash = (hash * 31) + character;
            }

            return hash;
        }
    }

    /// <summary>Elección de «sorpréndeme»: 0 → skill objetivo, 1 → empuje, 2 → tirón, 3 → pierna.</summary>
    private static int SurprisePick(SoloSessionParameters parameters, string skillId) =>
        (int)((uint)SeedFor(parameters.Time, parameters.Energy, skillId) % 4);

    private static ExerciseGroup GroupForSurprisePick(int pick) => pick switch
    {
        1 => ExerciseGroup.Push,
        2 => ExerciseGroup.Pull,
        3 => ExerciseGroup.Leg,
        _ => throw new ArgumentOutOfRangeException(nameof(pick)),
    };
}
