using Barrapp.Domain.Athlete;
using Barrapp.Domain.Common;
using Barrapp.Domain.Knowledge;
using Barrapp.Domain.Objectives;
using Barrapp.Domain.SkillProgress;

namespace Barrapp.Domain.Planning;

/// <summary>
/// Motor determinista del mesociclo: a partir del perfil y el objetivo construye un plan de cuatro
/// semanas con sesiones full-body (bloque de skill → fuerza por patrón → core). Es puro: mismas
/// entradas, mismo plan.
/// </summary>
/// <remarks>
/// Ticket #10 implementa el reparto de 3 días/semana; las frecuencias 4 y 5 devuelven
/// <see cref="DomainErrors.Plan.UnsupportedFrequency"/>. La carga de fuerza se deriva del máximo
/// del atleta (#11) con <see cref="StrengthLoad"/>, dejando repeticiones en reserva. El bloque de
/// skill practica la etapa actual del atleta (#14) y, en los skills apalancados, ajusta ±1 serie
/// según la <see cref="AthleteLever"/> y añade su nota de ritmo esperado (#68); el criterio de
/// etapa no cambia. La onda semanal de RIR (#12) sube el volumen en las semanas 2 y 3; el deload de
/// la semana 4 (#13) se apoyará en este mismo punto de entrada. Con un máximo de 0 en algún patrón,
/// el hueco de fuerza se cubre con la regresión del ejercicio (#17).
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
    /// catálogo, practicando en el bloque de skill la etapa actual del atleta. Falla con
    /// <see cref="DomainErrors.Plan.UnsupportedFrequency"/> si la frecuencia no es 3 días, con
    /// <see cref="DomainErrors.Plan.UnknownStage"/> si el skill no tiene esa etapa y con un error de
    /// catálogo si falta un ejercicio o un máximo obligatorios.
    /// </summary>
    /// <param name="stageOrder">
    /// Etapa actual del atleta en el skill objetivo; <c>null</c> si aún no tiene progresión guardada,
    /// en cuyo caso practica la primera etapa.
    /// </param>
    public static Result<Plan> Generate(
        AthleteProfile profile,
        Objective objective,
        int? stageOrder,
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

        var currentStage = skill.Stages.FirstOrDefault(
            stage => stage.Order == (stageOrder ?? AthleteSkillProgress.InitialStageOrder));
        if (currentStage is null)
        {
            return Result.Failure<Plan>(DomainErrors.Plan.UnknownStage);
        }

        // Solo los skills apalancados ajustan su bloque por palanca (ADR-0011); el criterio de la
        // etapa no se toca, sigue siendo dato de la escalera.
        var lever = skill.Lever ? AthleteLever.Classify(profile) : null;

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
                sessions.Add(new Session(day, BuildItems(currentStage, strength.Value, lever, repsInReserve)));
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

            slots.Add(ResolveStrengthSlot(catalog, exercise, ToGroup(basic.Pattern), maximum.Value));
        }

        return Result.Success<IReadOnlyList<StrengthSlot>>(slots);
    }

    // D3 (#17): un máximo de 0 no admite prescripción sobre el ancla (no hay margen ni para una
    // repetición), así que el hueco de fuerza de ese patrón pasa a la regresión del ejercicio,
    // prescrita sobre una base de trabajo asumida y modesta (`StrengthLoad.RegressionWorkableReps`).
    // La onda de RIR sigue aplicando sobre esa base, de modo que la regresión nunca se prescribe al
    // fallo ni con 0 repeticiones. El catálogo valida que toda regresión referenciada exista y se
    // resuelva; si no se pudiera resolver, se conserva el marcador neutro previo al #17.
    private static StrengthSlot ResolveStrengthSlot(
        IGenerationCatalog catalog,
        Exercise exercise,
        ExerciseGroup pattern,
        int maximum)
    {
        if (maximum == 0
            && exercise.RegressionId is not null
            && catalog.FindExercise(exercise.RegressionId) is { } regression)
        {
            return new StrengthSlot(pattern, regression.Id, StrengthLoad.RegressionWorkableReps);
        }

        return new StrengthSlot(pattern, exercise.Id, maximum);
    }

    private static IReadOnlyList<SessionItem> BuildItems(
        SkillStage stage,
        IReadOnlyList<StrengthSlot> strength,
        AthleteLever? lever,
        int repsInReserve)
    {
        var items = new List<SessionItem>(strength.Count + 2)
        {
            BuildSkillItem(stage, lever),
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

    private static SessionItem BuildSkillItem(SkillStage stage, AthleteLever? lever)
    {
        var isHold = stage.Criterion.Metric == Metric.Seconds;
        var sets = Math.Max(1, stage.Criterion.Sets + (lever?.SetAdjustment ?? 0));

        return new SessionItem(
            stage.ExerciseId,
            SessionItemRole.Skill,
            null,
            sets,
            isHold ? null : stage.Criterion.Target,
            isHold ? null : stage.Criterion.Target,
            isHold ? stage.Criterion.Target : null,
            isHold ? stage.Criterion.Target : null,
            lever?.Note);
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
