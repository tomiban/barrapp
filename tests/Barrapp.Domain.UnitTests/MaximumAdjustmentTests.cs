using Barrapp.Domain.Athlete;
using Barrapp.Domain.Knowledge;
using Barrapp.Domain.Planning;
using Barrapp.Domain.Sessions;

namespace Barrapp.Domain.UnitTests;

/// <summary>
/// El ajuste de máximos al cerrar el mesociclo (spec 0001, US-24; ticket #24, D7): para cada
/// ejercicio básico, «nuevo máximo = max(máximo actual, mejor marca real lograda)», nunca baja.
/// La mejor marca de un básico es la repetición más alta registrada en series de ese mismo
/// ejercicio básico (los básicos se miden en repeticiones en el catálogo). Los registros de
/// variantes y regresiones quedan fuera: su dificultad no es comparable con la del básico, y
/// traducirla al ancla sobreestimaría o perjudicaría la prescripción («nunca al fallo»). La
/// regla es pura y determinista: no toca el perfil, solo calcula los valores nuevos.
/// </summary>
public sealed class MaximumAdjustmentTests
{
    private static readonly Guid UserId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    private static readonly DateTimeOffset RecordedAt =
        new(2026, 10, 12, 18, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Compute_raises_an_anchor_to_its_best_registered_mark()
    {
        var profile = Profile(pushUp: 10, pullUp: 5, squat: 20);
        var logs = new[] { Log("push_up", (1, 12), (2, 14), (3, 13)) };

        var result = MaximumAdjustment.Compute(profile, logs, Catalog());

        Assert.Equal(Raised(("push_up", 14), ("pull_up", 5), ("squat", 20)), result);
    }

    [Fact]
    public void Compute_takes_the_highest_mark_across_all_logs_and_sets()
    {
        var profile = Profile(pushUp: 10, pullUp: 5, squat: 20);
        var logs = new[]
        {
            Log("push_up", (1, 10), (2, 12)),
            Log("push_up", (1, 15)),
            Log("push_up", (1, 9)),
        };

        var result = MaximumAdjustment.Compute(profile, logs, Catalog());

        Assert.Equal(15, ForCode(result, "push_up"));
    }

    [Fact]
    public void Compute_never_lowers_a_maximum_below_the_registered_mark()
    {
        var profile = Profile(pushUp: 10, pullUp: 5, squat: 20);
        var logs = new[] { Log("push_up", (1, 8), (2, 9), (3, 0)) };

        var result = MaximumAdjustment.Compute(profile, logs, Catalog());

        Assert.Equal(Raised(("push_up", 10), ("pull_up", 5), ("squat", 20)), result);
    }

    [Fact]
    public void Compute_leaves_the_maximums_untouched_without_logs()
    {
        var profile = Profile(pushUp: 10, pullUp: 5, squat: 20);

        var result = MaximumAdjustment.Compute(profile, [], Catalog());

        Assert.Equal(Raised(("push_up", 10), ("pull_up", 5), ("squat", 20)), result);
    }

    [Fact]
    public void Compute_ignores_logs_of_exercises_that_are_not_a_basic_anchor()
    {
        var profile = Profile(pushUp: 10, pullUp: 5, squat: 20);
        var logs = new[]
        {
            Log("incline-push-up", (1, 25), (2, 25)), // regresión del empuje: más fácil que el ancla
            Log("hollow-body-hold", (1, 40), (2, 45)), // core, en segundos
        };

        var result = MaximumAdjustment.Compute(profile, logs, Catalog());

        Assert.Equal(Raised(("push_up", 10), ("pull_up", 5), ("squat", 20)), result);
    }

    [Fact]
    public void Compute_ignores_logs_of_unknown_exercises()
    {
        var profile = Profile(pushUp: 10, pullUp: 5, squat: 20);
        var logs = new[] { Log("ejercicio-desconocido", (1, 30), (2, 30)) };

        var result = MaximumAdjustment.Compute(profile, logs, Catalog());

        Assert.Equal(Raised(("push_up", 10), ("pull_up", 5), ("squat", 20)), result);
    }

    [Fact]
    public void Compute_raises_each_anchor_independently_from_its_own_logs()
    {
        var profile = Profile(pushUp: 10, pullUp: 5, squat: 20);
        var logs = new[]
        {
            Log("push_up", (1, 15)),
            Log("squat", (1, 22), (2, 24)),
        };

        var result = MaximumAdjustment.Compute(profile, logs, Catalog());

        Assert.Equal(Raised(("push_up", 15), ("pull_up", 5), ("squat", 24)), result);
    }

    [Fact]
    public void Compute_is_deterministic_regardless_of_log_order()
    {
        var profile = Profile(pushUp: 10, pullUp: 5, squat: 20);
        var one = new[] { Log("push_up", (1, 15)), Log("pull_up", (1, 7)) };
        var another = new[] { Log("pull_up", (1, 7)), Log("push_up", (1, 15)) };

        var first = MaximumAdjustment.Compute(profile, one, Catalog());
        var second = MaximumAdjustment.Compute(profile, another, Catalog());

        Assert.Equal(first, second);
    }

    /// <summary>Perfil con los tres básicos y sus máximos actuales.</summary>
    private static AthleteProfile Profile(int pushUp, int pullUp, int squat) => AthleteProfile.Create(
        UserId,
        weightKilograms: 78,
        heightCentimeters: 180,
        armSpanCentimeters: 180,
        inseamCentimeters: 85,
        trainingDays: 3,
        [
            new MaximumInput("push_up", pushUp),
            new MaximumInput("pull_up", pullUp),
            new MaximumInput("squat", squat),
        ]).Value;

    /// <summary>Máximos calculados, en el orden canónico de los básicos.</summary>
    private static IReadOnlyList<MaximumInput> Raised(params (string Code, int Reps)[] maximums) =>
        maximums.Select(maximum => new MaximumInput(maximum.Code, maximum.Reps)).ToList();

    private static int ForCode(IReadOnlyList<MaximumInput> maximums, string code) =>
        maximums.Single(maximum => maximum.ExerciseCode == code).Repetitions;

    private static SessionLog Log(string exerciseId, params (int Set, int Value)[] sets) =>
        SessionLog.Create(
            UserId,
            exerciseId,
            mesocycleId: null,
            sessionDay: 1,
            RecordedAt,
            sets.Select(set => new SessionLogSetInput(set.Set, set.Value, null)).ToList()).Value;

    /// <summary>Catálogo mínimo con los tres básicos, una regresión y el core.</summary>
    private static StubCatalog Catalog() => new(
        Conditioning("push_up", ExerciseGroup.Push, tracksMaximum: true, regressionId: "incline-push-up"),
        Conditioning("incline-push-up", ExerciseGroup.Push, tracksMaximum: false),
        Conditioning("pull_up", ExerciseGroup.Pull, tracksMaximum: true, regressionId: "negative-pull-up"),
        Conditioning("squat", ExerciseGroup.Leg, tracksMaximum: true, regressionId: "box-squat"),
        Conditioning("hollow-body-hold", ExerciseGroup.Core, Metric.Seconds, tracksMaximum: false));

    private static Exercise Conditioning(
        string id,
        ExerciseGroup group,
        Metric metric = Metric.Reps,
        bool tracksMaximum = true,
        string? regressionId = null) =>
        new()
        {
            Id = id,
            Name = id,
            Kind = ExerciseKind.Conditioning,
            Group = group,
            Metric = metric,
            TracksMaximum = tracksMaximum,
            RegressionId = regressionId,
        };

    /// <summary>Catálogo de prueba: solo resuelve ejercicios, sin skills ni programas.</summary>
    private sealed class StubCatalog(params Exercise[] exercises) : IGenerationCatalog
    {
        private readonly Dictionary<string, Exercise> _exercises = exercises.ToDictionary(
            exercise => exercise.Id,
            StringComparer.Ordinal);

        public Skill? FindSkill(string skillId) => null;

        public Exercise? FindExercise(string exerciseId) =>
            _exercises.TryGetValue(exerciseId, out var exercise) ? exercise : null;

        public IReadOnlyList<Exercise> ExercisesByGroup(ExerciseGroup group) => [];
    }
}
