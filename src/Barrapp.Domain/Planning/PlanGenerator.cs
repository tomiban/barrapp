using Barrapp.Domain.Athlete;
using Barrapp.Domain.Common;
using Barrapp.Domain.Knowledge;
using Barrapp.Domain.Objectives;

namespace Barrapp.Domain.Planning;

/// <summary>
/// Motor determinista del mesociclo: a partir del perfil y el objetivo construye un plan de cuatro
/// semanas con sesiones full-body (bloque de skill → fuerza por patrón → core). Es puro: mismas
/// entradas, mismo plan.
/// </summary>
/// <remarks>
/// Ticket #10 implementa el reparto de 3 días/semana; las frecuencias 4 y 5 devuelven
/// <see cref="DomainErrors.Plan.UnsupportedFrequency"/>. La carga de fuerza se deriva del máximo
/// del atleta (#11) con <see cref="StrengthLoad"/>, dejando repeticiones en reserva. La onda
/// semanal de RIR (#12) sube el volumen en las semanas 2 y 3; el deload de la semana 4 (#13) se
/// apoyará en este mismo punto de entrada.
/// </remarks>
public static class PlanGenerator
{
    /// <summary>Semanas de un mesociclo.</summary>
    private const int MicrocycleCount = 4;

    /// <summary>Frecuencia soportada hoy: reparto full-body de 3 días.</summary>
    private const int SupportedTrainingDays = 3;

    /// <summary>Ejercicio de core por defecto.</summary>
    private const string CoreExerciseId = "hollow-body-hold";

    private const int StrengthSets = 3;
    private const int CoreSets = 3;
    private const int CoreHoldSecondsMin = 20;
    private const int CoreHoldSecondsMax = 30;

    /// <summary>
    /// Genera el mesociclo para <paramref name="profile"/> y <paramref name="objective"/> contra el
    /// catálogo. Falla con <see cref="DomainErrors.Plan.UnsupportedFrequency"/> si la frecuencia no
    /// es 3 días y con un error de catálogo si falta un ejercicio o una etapa obligatorios.
    /// </summary>
    public static Result<Plan> Generate(
        AthleteProfile profile,
        Objective objective,
        IGenerationCatalog catalog)
    {
        if (profile.TrainingDays != SupportedTrainingDays)
        {
            return Result.Failure<Plan>(DomainErrors.Plan.UnsupportedFrequency);
        }

        var skill = catalog.FindSkill(objective.SkillId);
        if (skill is null)
        {
            return Result.Failure<Plan>(DomainErrors.Objective.UnknownSkill);
        }

        var firstStage = skill.Stages.FirstOrDefault(stage => stage.Order == 1);
        if (firstStage is null)
        {
            return Result.Failure<Plan>(DomainErrors.Plan.MissingSkillStage);
        }

        var strength = ResolveStrengthSlots(catalog, profile);
        if (strength.IsFailure)
        {
            return Result.Failure<Plan>(strength.Error);
        }

        if (catalog.FindExercise(CoreExerciseId) is null)
        {
            return Result.Failure<Plan>(DomainErrors.Plan.UnknownExercise(CoreExerciseId));
        }

        var microcycles = new List<Microcycle>(MicrocycleCount);
        for (var number = 1; number <= MicrocycleCount; number++)
        {
            var repsInReserve = RirWave.RepsInReserve(number);
            var sessions = new List<Session>(profile.TrainingDays);
            for (var day = 1; day <= profile.TrainingDays; day++)
            {
                sessions.Add(new Session(day, BuildItems(firstStage, strength.Value, repsInReserve)));
            }

            microcycles.Add(new Microcycle(number, sessions));
        }

        return new Plan(skill.Id, profile.TrainingDays, microcycles);
    }

    private static Result<IReadOnlyList<StrengthSlot>> ResolveStrengthSlots(
        IGenerationCatalog catalog,
        AthleteProfile profile)
    {
        var slots = new List<StrengthSlot>(BasicExercises.All.Count);
        foreach (var basic in BasicExercises.All)
        {
            var exercise = catalog.FindExercise(basic.Code);
            if (exercise is null)
            {
                return Result.Failure<IReadOnlyList<StrengthSlot>>(
                    DomainErrors.Plan.UnknownExercise(basic.Code));
            }

            var maximum = profile.MaximumFor(basic.Code);
            if (maximum is null)
            {
                return Result.Failure<IReadOnlyList<StrengthSlot>>(
                    DomainErrors.AthleteProfile.MissingExerciseMaximum);
            }

            slots.Add(new StrengthSlot(ToGroup(basic.Pattern), exercise.Id, maximum.Value));
        }

        return Result.Success<IReadOnlyList<StrengthSlot>>(slots);
    }

    private static IReadOnlyList<SessionItem> BuildItems(
        SkillStage stage,
        IReadOnlyList<StrengthSlot> strength,
        int repsInReserve)
    {
        var items = new List<SessionItem>(strength.Count + 2)
        {
            BuildSkillItem(stage),
        };

        foreach (var slot in strength)
        {
            // La onda por microciclo (#12) fija cuántas repeticiones se dejan en reserva; el RIR
            // baja de 3 a 1 en las tres primeras semanas.
            var reps = StrengthLoad.Derive(slot.MaximumRepetitions, repsInReserve);

            items.Add(new SessionItem(
                slot.ExerciseId,
                SessionItemRole.Strength,
                slot.Pattern,
                StrengthSets,
                reps.Min,
                reps.Max,
                null,
                null));
        }

        items.Add(new SessionItem(
            CoreExerciseId,
            SessionItemRole.Core,
            null,
            CoreSets,
            null,
            null,
            CoreHoldSecondsMin,
            CoreHoldSecondsMax));

        return items;
    }

    private static SessionItem BuildSkillItem(SkillStage stage)
    {
        var isHold = stage.Criterion.Metric == Metric.Seconds;

        return new SessionItem(
            stage.ExerciseId,
            SessionItemRole.Skill,
            null,
            stage.Criterion.Sets,
            isHold ? null : stage.Criterion.Target,
            isHold ? null : stage.Criterion.Target,
            isHold ? stage.Criterion.Target : null,
            isHold ? stage.Criterion.Target : null);
    }

    // Puente entre el patrón anclado a los máximos (Athlete) y el grupo del catálogo. Ojo con el
    // nombre: `ExercisePattern.Legs` (plural) se corresponde con `ExerciseGroup.Leg` (singular).
    private static ExerciseGroup ToGroup(ExercisePattern pattern) => pattern switch
    {
        ExercisePattern.Push => ExerciseGroup.Push,
        ExercisePattern.Pull => ExerciseGroup.Pull,
        ExercisePattern.Legs => ExerciseGroup.Leg,
        _ => throw new ArgumentOutOfRangeException(nameof(pattern)),
    };

    /// <summary>
    /// Patrón, ejercicio ancla y máximo del atleta que ocupan un hueco de fuerza en la sesión.
    /// </summary>
    private readonly record struct StrengthSlot(
        ExerciseGroup Pattern,
        string ExerciseId,
        int MaximumRepetitions);
}
