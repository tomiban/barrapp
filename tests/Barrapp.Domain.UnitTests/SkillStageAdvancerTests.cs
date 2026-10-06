using Barrapp.Domain.Common;
using Barrapp.Domain.Knowledge;
using Barrapp.Domain.Sessions;
using Barrapp.Domain.SkillProgress;

namespace Barrapp.Domain.UnitTests;

/// <summary>
/// El avance de etapa del skill (spec 0001, US-19; decisión D4): el atleta sube de etapa al
/// cumplir el criterio de la etapa actual en dos sesiones consecutivas. Una sesión cumple el
/// criterio cuando el ejercicio de la etapa alcanza el objetivo en las series exigidas; solo
/// cuentan las sesiones en las que se practicó ese ejercicio, y las dos más recientes deben
/// cumplirlo para avanzar.
/// </summary>
public sealed class SkillStageAdvancerTests
{
    private static readonly Guid UserId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    private const string StageExercise = "handstand-wall-support";
    private const string OtherExercise = "push_up";

    private static readonly DateTimeOffset DayOne = new(2026, 10, 5, 18, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset DayTwo = new(2026, 10, 7, 18, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset DayThree = new(2026, 10, 9, 18, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Evaluate_advances_when_the_two_most_recent_sessions_meet_the_criterion()
    {
        var skill = Skill(seconds: 30, sets: 3);

        var logs = new[]
        {
            Log(sessionDay: 1, DayOne, (1, 30), (2, 30), (3, 30)),
            Log(sessionDay: 2, DayTwo, (1, 30), (2, 30), (3, 30)),
        };

        var result = SkillStageAdvancer.Evaluate(skill, currentStageOrder: 1, logs);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value.Advanced);
        Assert.Equal(2, result.Value.StageOrder);
    }

    [Fact]
    public void Evaluate_does_not_advance_when_the_last_session_falls_short_of_the_target()
    {
        var skill = Skill(seconds: 30, sets: 3);

        var logs = new[]
        {
            Log(sessionDay: 1, DayOne, (1, 30), (2, 30), (3, 30)),
            Log(sessionDay: 2, DayTwo, (1, 30), (2, 30), (3, 20)),
        };

        var result = SkillStageAdvancer.Evaluate(skill, currentStageOrder: 1, logs);

        Assert.True(result.IsSuccess);
        Assert.False(result.Value.Advanced);
        Assert.Equal(1, result.Value.StageOrder);
    }

    [Fact]
    public void Evaluate_does_not_advance_with_a_single_meeting_session()
    {
        var skill = Skill(seconds: 30, sets: 3);
        var logs = new[]
        {
            Log(sessionDay: 1, DayOne, (1, 30), (2, 30), (3, 30)),
        };

        var result = SkillStageAdvancer.Evaluate(skill, currentStageOrder: 1, logs);

        Assert.True(result.IsSuccess);
        Assert.False(result.Value.Advanced);
        Assert.Equal(1, result.Value.StageOrder);
    }

    [Fact]
    public void Evaluate_does_not_advance_when_an_intermediate_session_breaks_the_streak()
    {
        var skill = Skill(seconds: 30, sets: 3);

        var logs = new[]
        {
            Log(sessionDay: 1, DayOne, (1, 30), (2, 30), (3, 30)),
            Log(sessionDay: 2, DayTwo, (1, 30), (2, 30), (3, 20)),
            Log(sessionDay: 3, DayThree, (1, 30), (2, 30), (3, 30)),
        };

        var result = SkillStageAdvancer.Evaluate(skill, currentStageOrder: 1, logs);

        Assert.True(result.IsSuccess);
        Assert.False(result.Value.Advanced);
        Assert.Equal(1, result.Value.StageOrder);
    }

    [Fact]
    public void Evaluate_does_not_advance_when_the_required_sets_were_not_all_registered()
    {
        var skill = Skill(seconds: 30, sets: 3);

        var logs = new[]
        {
            Log(sessionDay: 1, DayOne, (1, 30), (2, 30), (3, 30)),
            Log(sessionDay: 2, DayTwo, (1, 30), (2, 30)),
        };

        var result = SkillStageAdvancer.Evaluate(skill, currentStageOrder: 1, logs);

        Assert.True(result.IsSuccess);
        Assert.False(result.Value.Advanced);
        Assert.Equal(1, result.Value.StageOrder);
    }

    [Fact]
    public void Evaluate_does_not_advance_past_the_last_stage()
    {
        var skill = Skill(seconds: 10, sets: 3, totalStages: 2);

        var logs = new[]
        {
            Log(sessionDay: 1, DayOne, (1, 10), (2, 10), (3, 10)),
            Log(sessionDay: 2, DayTwo, (1, 10), (2, 10), (3, 10)),
        };

        var result = SkillStageAdvancer.Evaluate(skill, currentStageOrder: 2, logs);

        Assert.True(result.IsSuccess);
        Assert.False(result.Value.Advanced);
        Assert.Equal(2, result.Value.StageOrder);
    }

    [Fact]
    public void Evaluate_counts_only_consecutive_sessions_that_practiced_the_stage_exercise()
    {
        var skill = Skill(seconds: 30, sets: 3);

        var logs = new[]
        {
            Log(sessionDay: 1, DayOne, (1, 30), (2, 30), (3, 30)),
            Log(sessionDay: 2, DayTwo, exerciseId: OtherExercise, (1, 10), (2, 10)),
            Log(sessionDay: 3, DayThree, (1, 30), (2, 30), (3, 30)),
        };

        var result = SkillStageAdvancer.Evaluate(skill, currentStageOrder: 1, logs);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value.Advanced);
        Assert.Equal(2, result.Value.StageOrder);
    }

    [Fact]
    public void Evaluate_uses_the_last_registration_of_a_session_day()
    {
        var skill = Skill(seconds: 30, sets: 3);

        var logs = new[]
        {
            Log(sessionDay: 1, DayOne, (1, 30), (2, 30), (3, 20)),
            Log(sessionDay: 1, DayTwo, (1, 30), (2, 30), (3, 30)),
            Log(sessionDay: 2, DayThree, (1, 30), (2, 30), (3, 30)),
        };

        var result = SkillStageAdvancer.Evaluate(skill, currentStageOrder: 1, logs);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value.Advanced);
        Assert.Equal(2, result.Value.StageOrder);
    }

    [Fact]
    public void Evaluate_returns_the_current_stage_without_logs()
    {
        var skill = Skill(seconds: 30, sets: 3);

        var result = SkillStageAdvancer.Evaluate(skill, currentStageOrder: 1, []);

        Assert.True(result.IsSuccess);
        Assert.False(result.Value.Advanced);
        Assert.Equal(1, result.Value.StageOrder);
    }

    [Fact]
    public void Evaluate_fails_for_a_stage_that_is_not_in_the_ladder()
    {
        var skill = Skill(seconds: 30, sets: 3);

        var result = SkillStageAdvancer.Evaluate(skill, currentStageOrder: 9, []);

        Assert.True(result.IsFailure);
        Assert.Equal(DomainErrors.SkillProgress.UnknownStage, result.Error);
    }

    [Fact]
    public void Evaluate_fails_for_a_null_skill()
    {
        var result = SkillStageAdvancer.Evaluate(skill: null!, currentStageOrder: 1, []);

        Assert.True(result.IsFailure);
        Assert.Equal(DomainErrors.SkillProgress.UnknownSkill, result.Error);
    }

    private static Skill Skill(int seconds, int sets, int totalStages = 3) => new()
    {
        Id = "handstand",
        Name = "Pino",
        Stages = Enumerable.Range(1, totalStages)
            .Select(order => new SkillStage
            {
                Order = order,
                Name = $"Etapa {order}",
                ExerciseId = StageExercise,
                Criterion = new StageCriterion
                {
                    Metric = Metric.Seconds,
                    Target = seconds,
                    Sets = sets,
                },
            })
            .ToList(),
    };

    private static SessionLog Log(
        int sessionDay,
        DateTimeOffset recordedAt,
        params (int Set, int Value)[] sets) =>
        Log(sessionDay, recordedAt, StageExercise, sets);

    private static SessionLog Log(
        int sessionDay,
        DateTimeOffset recordedAt,
        string exerciseId,
        params (int Set, int Value)[] sets) =>
        SessionLog.Create(
            UserId,
            exerciseId,
            mesocycleId: null,
            sessionDay,
            recordedAt,
            sets.Select(set => new SessionLogSetInput(set.Set, set.Value, null)).ToList()).Value;
}