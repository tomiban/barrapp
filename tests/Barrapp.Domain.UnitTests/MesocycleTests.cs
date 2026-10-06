using Barrapp.Domain.Athlete;
using Barrapp.Domain.Common;
using Barrapp.Domain.Knowledge;
using Barrapp.Domain.Objectives;
using Barrapp.Domain.Planning;

namespace Barrapp.Domain.UnitTests;

/// <summary>
/// El mesociclo persistido (#27, D7): el agregado que guarda el plan como snapshot cuando se
/// genera, para poder listar los pasados y abrir su detalle. Nace <see cref="MesocycleStatus.Active"/>
/// y conserva el plan íntegro: microciclos, sesiones, filas y la etapa actual del skill. El cierre
/// (<see cref="Mesocycle.Close"/>) deja el <i>seam</i> del ticket #24 preparado; el ajuste de
/// máximos no vive aquí. Se prueba por su interfaz pública.
/// </summary>
public sealed class MesocycleTests
{
    private static readonly Guid UserId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private const string SkillId = "planche";
    private static readonly DateTimeOffset StartedAtUtc = DateTimeOffset.Parse("2026-10-01T08:00:00+00:00");

    [Fact]
    public void Create_builds_an_active_mesocycle_from_the_plan()
    {
        var creation = Mesocycle.Create(UserId, BuildPlan(), StartedAtUtc);

        Assert.True(creation.IsSuccess);
        var mesocycle = creation.Value;
        Assert.NotEqual(Guid.Empty, mesocycle.Id);
        Assert.Equal(UserId, mesocycle.UserId);
        Assert.Equal(SkillId, mesocycle.SkillId);
        Assert.Equal(3, mesocycle.TrainingDays);
        Assert.Equal(MesocycleStatus.Active, mesocycle.Status);
        Assert.Equal(StartedAtUtc, mesocycle.StartedAtUtc);
        Assert.Null(mesocycle.ClosedAtUtc);
    }

    [Fact]
    public void Create_fails_without_an_integrity_plan()
    {
        var creation = Mesocycle.Create(UserId, null!, StartedAtUtc);

        Assert.True(creation.IsFailure);
        Assert.Equal(DomainErrors.Mesocycle.InvalidSnapshot, creation.Error);
    }

    [Fact]
    public void Snapshot_preserves_every_week_session_and_item_of_the_plan()
    {
        var mesocycle = Mesocycle.Create(UserId, BuildPlan(), StartedAtUtc).Value;

        var plan = mesocycle.Snapshot.ToPlan();

        Assert.Equal(Snapshot(BuildPlan()), Snapshot(plan));
        Assert.Equal(SkillId, plan.SkillId);
        Assert.Equal(3, plan.TrainingDays);
        Assert.Equal(4, plan.Microcycles.Count);
        Assert.All(plan.Microcycles, microcycle => Assert.Equal(3, microcycle.Sessions.Count));
    }

    [Fact]
    public void Snapshot_keeps_the_skill_stage_the_plan_was_generated_with()
    {
        var plan = BuildPlan(stageOrder: 2);
        var mesocycle = Mesocycle.Create(UserId, plan, StartedAtUtc).Value;

        var stage = mesocycle.Snapshot.ToPlan().CurrentStage;

        Assert.Equal(2, stage.Order);
        Assert.Equal("planche-tuck", stage.ExerciseId);
        Assert.Equal(Metric.Seconds, stage.Criterion.Metric);
        Assert.Equal(10, stage.Criterion.Target);
        Assert.Equal(3, stage.Criterion.Sets);
    }

    [Fact]
    public void Snapshot_preserves_the_note_of_the_skill_item_for_a_levered_skill()
    {
        var plan = PlanGenerator
            .Generate(BuildProfile(weightKilograms: 100, heightCentimeters: 185), BuildObjective(), 1, Catalog())
            .Value;

        var roundTripped = Mesocycle.Create(UserId, plan, StartedAtUtc).Value.Snapshot.ToPlan();

        var skillItem = roundTripped.Microcycles[0].Sessions[0].Items[0];
        Assert.NotNull(skillItem.Note);
        Assert.Contains("lento", skillItem.Note);
    }

    [Fact]
    public void Close_marks_the_mesocycle_closed_with_a_timestamp()
    {
        var mesocycle = Mesocycle.Create(UserId, BuildPlan(), StartedAtUtc).Value;
        var closedAtUtc = DateTimeOffset.Parse("2026-11-01T08:00:00+00:00");

        var closing = mesocycle.Close(closedAtUtc);

        Assert.True(closing.IsSuccess);
        Assert.Equal(MesocycleStatus.Closed, mesocycle.Status);
        Assert.Equal(closedAtUtc, mesocycle.ClosedAtUtc);
    }

    [Fact]
    public void Close_twice_fails_with_a_conflict()
    {
        var mesocycle = Mesocycle.Create(UserId, BuildPlan(), StartedAtUtc).Value;
        Assert.True(mesocycle.Close(DateTimeOffset.Parse("2026-11-01T08:00:00+00:00")).IsSuccess);

        var secondClose = mesocycle.Close(DateTimeOffset.Parse("2026-12-01T08:00:00+00:00"));

        Assert.True(secondClose.IsFailure);
        Assert.Equal(DomainErrors.Mesocycle.AlreadyClosed, secondClose.Error);
    }

    [Fact]
    public void Close_twice_keeps_the_first_close_timestamp()
    {
        var mesocycle = Mesocycle.Create(UserId, BuildPlan(), StartedAtUtc).Value;
        var closedAtUtc = DateTimeOffset.Parse("2026-11-01T08:00:00+00:00");
        mesocycle.Close(closedAtUtc);
        mesocycle.Close(DateTimeOffset.Parse("2026-12-01T08:00:00+00:00"));

        Assert.Equal(closedAtUtc, mesocycle.ClosedAtUtc);
    }

    /// <summary>Proyección canónica del plan para comparar dos planes sin depender de Equals.</summary>
    private static string Snapshot(Plan plan) =>
        string.Join(
            "|",
            plan.Microcycles.SelectMany(microcycle =>
                microcycle.Sessions.SelectMany(session =>
                    session.Items.Select(item =>
                        $"{microcycle.Number}:{session.Day}:{item.ExerciseId}:{item.Role}:{item.Pattern}:"
                        + $"{item.Sets}:{item.RepsMin}-{item.RepsMax}:{item.HoldSecondsMin}-{item.HoldSecondsMax}:{item.Note}"))));

    private static Plan BuildPlan(int stageOrder = 1)
    {
        var catalog = Catalog();
        return PlanGenerator.Generate(BuildProfile(), BuildObjective(), stageOrder, catalog).Value;
    }

    private static AthleteProfile BuildProfile(
        int trainingDays = 3,
        double weightKilograms = 78,
        double heightCentimeters = 180) =>
        AthleteProfile.Create(
            UserId,
            weightKilograms,
            heightCentimeters,
            armSpanCentimeters: 180,
            inseamCentimeters: 85,
            trainingDays,
            [
                new MaximumInput("push_up", 10),
                new MaximumInput("pull_up", 5),
                new MaximumInput("squat", 20),
            ]).Value;

    private static Objective BuildObjective() => Objective.Create(UserId, SkillId, Catalog()).Value;

    private static KnowledgeBase Catalog()
    {
        var exercises = new List<Exercise>
        {
            Conditioning("push_up", ExerciseGroup.Push, regressionId: "incline-push-up"),
            Conditioning("incline-push-up", ExerciseGroup.Push),
            Conditioning("pull_up", ExerciseGroup.Pull, regressionId: "negative-pull-up"),
            Conditioning("negative-pull-up", ExerciseGroup.Pull),
            Conditioning("squat", ExerciseGroup.Leg, regressionId: "box-squat"),
            Conditioning("box-squat", ExerciseGroup.Leg),
            Conditioning("hollow-body-hold", ExerciseGroup.Core, Metric.Seconds),
            SkillMovement("planche-lean"),
            SkillMovement("planche-tuck"),
            SkillMovement("planche-advanced-tuck"),
            SkillMovement("planche-full"),
        };

        var planche = new Skill
        {
            Id = SkillId,
            Name = "Planche",
            Group = ExerciseGroup.Push,
            Lever = true,
            Stages =
            [
                Stage(1, "planche-lean", Metric.Seconds, target: 20, sets: 3),
                Stage(2, "planche-tuck", Metric.Seconds, target: 10, sets: 3),
                Stage(3, "planche-advanced-tuck", Metric.Seconds, target: 8, sets: 3),
                Stage(4, "planche-full", Metric.Seconds, target: 3, sets: 3),
            ],
            PatternRoutines =
            [
                new PatternRoutine
                {
                    Id = "r1",
                    Name = "Modelo R1",
                    Intensity = 2,
                    Items =
                    [
                        new RoutineItem
                        {
                            ExerciseId = "push_up",
                            Sets = 3,
                            RepsMin = 5,
                            RepsMax = 8,
                            RestSeconds = 120,
                        },
                    ],
                },
            ],
        };

        var result = KnowledgeBase.Create(exercises, [planche], []);
        Assert.True(result.IsSuccess);
        return result.Value;
    }

    private static Exercise Conditioning(
        string id,
        ExerciseGroup group,
        Metric metric = Metric.Reps,
        string? regressionId = null) =>
        new()
        {
            Id = id,
            Name = id,
            Kind = ExerciseKind.Conditioning,
            Group = group,
            Metric = metric,
            RegressionId = regressionId,
        };

    private static Exercise SkillMovement(string id, Metric metric = Metric.Seconds) =>
        new()
        {
            Id = id,
            Name = id,
            Kind = ExerciseKind.Skill,
            Group = null,
            Metric = metric,
            SkillId = SkillId,
        };

    private static SkillStage Stage(int order, string exerciseId, Metric metric, int target, int sets) =>
        new()
        {
            Order = order,
            Name = $"Etapa {order}",
            ExerciseId = exerciseId,
            Criterion = new StageCriterion { Metric = metric, Target = target, Sets = sets },
        };
}