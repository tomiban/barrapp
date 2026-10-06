using Barrapp.Domain.Athlete;
using Barrapp.Domain.Common;
using Barrapp.Domain.Knowledge;
using Barrapp.Domain.Objectives;
using Barrapp.Domain.Planning;

namespace Barrapp.Domain.UnitTests;

/// <summary>
/// Reglas del motor de generación (#10): da un mesociclo de cuatro semanas con reparto full-body
/// de 3 días, cada patrón recibe trabajo al menos dos veces por semana, cada sesión empieza por el
/// bloque de skill y el resultado es determinista. Solo se prueba por su interfaz pública.
/// </summary>
public sealed class PlanGeneratorTests
{
    private static readonly Guid UserId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private const string SkillId = "planche";

    [Fact]
    public void Generate_returns_a_four_week_plan_with_three_sessions_per_week()
    {
        var result = PlanGenerator.Generate(BuildProfile(3), BuildObjective(), Catalog());

        Assert.True(result.IsSuccess);
        Assert.Equal(SkillId, result.Value.SkillId);
        Assert.Equal(3, result.Value.TrainingDays);
        Assert.Equal(4, result.Value.Microcycles.Count);
        Assert.All(result.Value.Microcycles, microcycle => Assert.Equal(3, microcycle.Sessions.Count));
    }

    [Fact]
    public void Generate_gives_every_strength_pattern_work_at_least_twice_per_week()
    {
        var plan = PlanGenerator.Generate(BuildProfile(3), BuildObjective(), Catalog()).Value;

        foreach (var microcycle in plan.Microcycles)
        {
            var workByPattern = microcycle.Sessions
                .SelectMany(session => session.Items)
                .Where(item => item.Role == SessionItemRole.Strength)
                .GroupBy(item => item.Pattern!.Value)
                .ToDictionary(group => group.Key, group => group.Count());

            Assert.True(workByPattern.GetValueOrDefault(ExerciseGroup.Push) >= 2);
            Assert.True(workByPattern.GetValueOrDefault(ExerciseGroup.Pull) >= 2);
            Assert.True(workByPattern.GetValueOrDefault(ExerciseGroup.Leg) >= 2);
        }
    }

    [Fact]
    public void Generate_builds_each_session_as_skill_then_strength_by_pattern_then_core()
    {
        var plan = PlanGenerator.Generate(BuildProfile(3), BuildObjective(), Catalog()).Value;

        foreach (var session in plan.Microcycles.SelectMany(microcycle => microcycle.Sessions))
        {
            Assert.Equal(
                new[]
                {
                    SessionItemRole.Skill,
                    SessionItemRole.Strength,
                    SessionItemRole.Strength,
                    SessionItemRole.Strength,
                    SessionItemRole.Core,
                },
                session.Items.Select(item => item.Role));

            Assert.Equal(
                new[] { "push_up", "pull_up", "squat" },
                session.Items
                    .Where(item => item.Role == SessionItemRole.Strength)
                    .Select(item => item.ExerciseId));

            Assert.Equal("hollow-body-hold", session.Items[^1].ExerciseId);
        }
    }

    [Fact]
    public void Generate_starts_each_session_with_the_objective_skill_block()
    {
        var catalog = Catalog();
        var firstStage = catalog.FindSkill(SkillId)!.Stages.Single(stage => stage.Order == 1);

        var plan = PlanGenerator.Generate(BuildProfile(3), BuildObjective(), catalog).Value;

        foreach (var session in plan.Microcycles.SelectMany(microcycle => microcycle.Sessions))
        {
            var firstItem = session.Items[0];

            Assert.Equal(SessionItemRole.Skill, firstItem.Role);
            Assert.Equal(firstStage.ExerciseId, firstItem.ExerciseId);
            Assert.Equal(firstStage.Criterion.Sets, firstItem.Sets);
            Assert.Equal(firstStage.Criterion.Target, firstItem.HoldSecondsMax);
        }
    }

    [Fact]
    public void Generate_is_deterministic()
    {
        var first = PlanGenerator.Generate(BuildProfile(3), BuildObjective(), Catalog()).Value;
        var second = PlanGenerator.Generate(BuildProfile(3), BuildObjective(), Catalog()).Value;

        Assert.Equal(Snapshot(first), Snapshot(second));
    }

    [Theory]
    [InlineData(4)]
    [InlineData(5)]
    public void Generate_fails_for_a_frequency_other_than_three_days(int trainingDays)
    {
        var result = PlanGenerator.Generate(BuildProfile(trainingDays), BuildObjective(), Catalog());

        Assert.True(result.IsFailure);
        Assert.Equal(DomainErrors.Plan.UnsupportedFrequency, result.Error);
        Assert.Contains("3 días", result.Error.Description);
    }

    private static string Snapshot(Plan plan) =>
        string.Join(
            "|",
            plan.Microcycles.SelectMany(microcycle =>
                microcycle.Sessions.SelectMany(session =>
                    session.Items.Select(item =>
                        $"{microcycle.Number}:{session.Day}:{item.ExerciseId}:{item.Role}:{item.Pattern}:"
                        + $"{item.Sets}:{item.RepsMin}-{item.RepsMax}:{item.HoldSecondsMin}-{item.HoldSecondsMax}"))));

    private static AthleteProfile BuildProfile(int trainingDays) =>
        AthleteProfile.Create(
            UserId,
            78,
            180,
            180,
            85,
            trainingDays,
            [
                new MaximumInput("push_up", 10),
                new MaximumInput("pull_up", 5),
                new MaximumInput("squat", 20),
            ]).Value;

    private static Objective BuildObjective() =>
        Objective.Create(UserId, SkillId, Catalog()).Value;

    private static KnowledgeBase Catalog()
    {
        var exercises = new List<Exercise>
        {
            Conditioning("push_up", ExerciseGroup.Push),
            Conditioning("pull_up", ExerciseGroup.Pull),
            Conditioning("squat", ExerciseGroup.Leg),
            Conditioning("hollow-body-hold", ExerciseGroup.Core, Metric.Seconds),
            SkillMovement("planche-lean"),
            SkillMovement("planche-tuck"),
            SkillMovement("planche-advanced-tuck"),
            SkillMovement("planche-full"),
        };

        var skill = new Skill
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

        var result = KnowledgeBase.Create(exercises, [skill], []);
        Assert.True(result.IsSuccess);
        return result.Value;
    }

    private static Exercise Conditioning(
        string id,
        ExerciseGroup group,
        Metric metric = Metric.Reps) =>
        new()
        {
            Id = id,
            Name = id,
            Kind = ExerciseKind.Conditioning,
            Group = group,
            Metric = metric,
        };

    private static Exercise SkillMovement(string id) =>
        new()
        {
            Id = id,
            Name = id,
            Kind = ExerciseKind.Skill,
            Group = null,
            Metric = Metric.Seconds,
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
