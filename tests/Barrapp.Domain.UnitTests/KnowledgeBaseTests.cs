using Barrapp.Domain.Common;
using Barrapp.Domain.Knowledge;

namespace Barrapp.Domain.UnitTests;

/// <summary>
/// Reglas de validación del catálogo de conocimiento (esquema del ticket #63). Se construyen los
/// objetos directamente y se comprueba el <see cref="Result{TValue}"/> que devuelve
/// <see cref="KnowledgeBase.Create"/>; el cargador de Persistence reutiliza esta misma puerta.
/// </summary>
public sealed class KnowledgeBaseTests
{
    [Fact]
    public void Create_accepts_an_empty_catalog()
    {
        var result = KnowledgeBase.Create([], [], []);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void Create_accepts_a_valid_catalog()
    {
        var result = Create();

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value.Exercises);
        Assert.Single(result.Value.Skills);
        Assert.Single(result.Value.Programs);
    }

    [Fact]
    public void Create_rejects_duplicate_exercise_ids()
    {
        var result = Create(exercises: [Conditioning(), Conditioning()]);

        AssertRejected(result, "knowledge.duplicate_exercise_id");
    }

    [Fact]
    public void Create_rejects_duplicate_skill_ids()
    {
        var result = Create(skills: [ValidSkill(), ValidSkill()]);

        AssertRejected(result, "knowledge.duplicate_skill_id");
    }

    [Fact]
    public void Create_rejects_duplicate_program_ids()
    {
        var result = Create(programs: [ValidProgram(), ValidProgram()]);

        AssertRejected(result, "knowledge.duplicate_program_id");
    }

    [Fact]
    public void Create_rejects_duplicate_routine_ids_within_a_skill()
    {
        var skill = ValidSkill(patternRoutines: [PatternRoutine("r1"), PatternRoutine("r1")]);

        var result = Create(skills: [skill]);

        AssertRejected(result, "knowledge.duplicate_routine_id");
    }

    [Fact]
    public void Create_rejects_a_dangling_regression_reference()
    {
        var result = Create(exercises: [Conditioning(regressionId: "does-not-exist")]);

        AssertRejected(result, "knowledge.unknown_exercise_reference");
    }

    [Fact]
    public void Create_rejects_a_maximum_exercise_without_a_regression()
    {
        var result = Create(exercises: [Conditioning(tracksMaximum: true)]);

        AssertRejected(result, "knowledge.basic_exercise_requires_regression");
    }

    [Fact]
    public void Create_accepts_a_maximum_exercise_with_a_resolvable_regression()
    {
        var result = Create(
            exercises:
            [
                Conditioning(tracksMaximum: true, regressionId: "incline-push-up"),
                Conditioning(id: "incline-push-up"),
            ]);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void Create_rejects_a_dangling_skill_reference_on_a_conditioning_exercise()
    {
        var result = Create(exercises: [Conditioning(skillId: "does-not-exist")]);

        AssertRejected(result, "knowledge.unknown_skill_reference");
    }

    [Fact]
    public void Create_rejects_a_dangling_stage_exercise_reference()
    {
        var skill = ValidSkill(stages: [Stage(1, "does-not-exist"), Stage(2), Stage(3), Stage(4)]);

        var result = Create(skills: [skill]);

        AssertRejected(result, "knowledge.unknown_exercise_reference");
    }

    [Fact]
    public void Create_rejects_a_dangling_item_exercise_reference()
    {
        var skill = ValidSkill(patternRoutines: [PatternRoutine(items: [Item("does-not-exist")])]);

        var result = Create(skills: [skill]);

        AssertRejected(result, "knowledge.unknown_exercise_reference");
    }

    [Fact]
    public void Create_rejects_a_conditioning_exercise_without_a_group()
    {
        var result = Create(exercises: [Conditioning(group: null)]);

        AssertRejected(result, "knowledge.conditioning_requires_group");
    }

    [Fact]
    public void Create_rejects_a_skill_movement_with_a_group()
    {
        var movement = SkillMovement("planche-lean", "planche", group: ExerciseGroup.Push);

        var result = Create(exercises: [movement]);

        AssertRejected(result, "knowledge.skill_exercise_has_group");
    }

    [Fact]
    public void Create_rejects_a_skill_movement_without_a_skill_id()
    {
        var movement = SkillMovement("planche-lean", skillId: "");

        var result = Create(exercises: [movement]);

        AssertRejected(result, "knowledge.skill_exercise_requires_skill_id");
    }

    [Fact]
    public void Create_rejects_a_skill_that_trains_cardio()
    {
        var result = Create(skills: [ValidSkill(group: ExerciseGroup.Cardio)]);

        AssertRejected(result, "knowledge.skill_group_out_of_range");
    }

    [Fact]
    public void Create_rejects_a_skill_with_fewer_than_four_stages()
    {
        var skill = ValidSkill(stages: [Stage(1), Stage(2), Stage(3)]);

        var result = Create(skills: [skill]);

        AssertRejected(result, "knowledge.skill_stages_out_of_range");
    }

    [Fact]
    public void Create_rejects_a_skill_with_more_than_six_stages()
    {
        var skill = ValidSkill(
            stages: [Stage(1), Stage(2), Stage(3), Stage(4), Stage(5), Stage(6), Stage(7)]);

        var result = Create(skills: [skill]);

        AssertRejected(result, "knowledge.skill_stages_out_of_range");
    }

    [Fact]
    public void Create_rejects_non_consecutive_stage_orders()
    {
        var skill = ValidSkill(stages: [Stage(1), Stage(2), Stage(4), Stage(5)]);

        var result = Create(skills: [skill]);

        AssertRejected(result, "knowledge.stage_order_not_consecutive");
    }

    [Fact]
    public void Create_rejects_a_skill_without_pattern_routines()
    {
        var result = Create(skills: [ValidSkill(patternRoutines: [])]);

        AssertRejected(result, "knowledge.empty_pattern_routines");
    }

    [Fact]
    public void Create_rejects_a_criterion_with_a_non_positive_target()
    {
        var skill = ValidSkill(stages: [Stage(1, target: 0), Stage(2), Stage(3), Stage(4)]);

        var result = Create(skills: [skill]);

        AssertRejected(result, "knowledge.criterion_target_must_be_positive");
    }

    [Fact]
    public void Create_rejects_a_criterion_with_non_positive_sets()
    {
        var skill = ValidSkill(stages: [Stage(1, sets: 0), Stage(2), Stage(3), Stage(4)]);

        var result = Create(skills: [skill]);

        AssertRejected(result, "knowledge.criterion_sets_must_be_positive");
    }

    [Fact]
    public void Create_rejects_a_routine_item_without_a_range()
    {
        var program = ProgramWithItems(Item(repsMin: null, repsMax: null));

        var result = Create(programs: [program]);

        AssertRejected(result, "knowledge.routine_item_requires_range");
    }

    [Fact]
    public void Create_rejects_a_routine_item_with_a_reps_min_greater_than_max()
    {
        var program = ProgramWithItems(Item(repsMin: 8, repsMax: 5));

        var result = Create(programs: [program]);

        AssertRejected(result, "knowledge.routine_range_min_greater_than_max");
    }

    [Fact]
    public void Create_rejects_a_routine_item_with_a_hold_seconds_min_greater_than_max()
    {
        var program = ProgramWithItems(
            Item(repsMin: null, repsMax: null, holdSecondsMin: 15, holdSecondsMax: 5));

        var result = Create(programs: [program]);

        AssertRejected(result, "knowledge.routine_range_min_greater_than_max");
    }

    [Fact]
    public void Create_rejects_a_routine_item_with_non_positive_sets()
    {
        var program = ProgramWithItems(Item(sets: 0));

        var result = Create(programs: [program]);

        AssertRejected(result, "knowledge.routine_item_sets_must_be_positive");
    }

    [Fact]
    public void Create_rejects_a_routine_item_with_a_negative_rest()
    {
        var program = ProgramWithItems(Item(restSeconds: -1));

        var result = Create(programs: [program]);

        AssertRejected(result, "knowledge.rest_seconds_must_be_non_negative");
    }

    [Fact]
    public void Create_rejects_non_consecutive_superset_groups()
    {
        var program = ProgramWithItems(
            Item(supersetGroup: 1),
            Item(supersetGroup: 2),
            Item(supersetGroup: 1));

        var result = Create(programs: [program]);

        AssertRejected(result, "knowledge.superset_group_not_consecutive");
    }

    [Fact]
    public void Create_accepts_consecutive_superset_groups()
    {
        var program = ProgramWithItems(
            Item(supersetGroup: 1),
            Item(supersetGroup: 1),
            Item(supersetGroup: 2),
            Item(supersetGroup: null));

        var result = Create(programs: [program]);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void Create_rejects_a_program_without_routines()
    {
        var result = Create(programs: [ValidProgram(routines: [])]);

        AssertRejected(result, "knowledge.program_requires_routines");
    }

    [Fact]
    public void Create_rejects_a_block_with_fewer_than_one_round()
    {
        var program = ValidProgram(routines: [Template(blocks: [Block(rounds: 0)])]);

        var result = Create(programs: [program]);

        AssertRejected(result, "knowledge.block_rounds_must_be_positive");
    }

    private static void AssertRejected(Result<KnowledgeBase> result, string expectedCode)
    {
        Assert.True(result.IsFailure);
        Assert.Equal(expectedCode, result.Error.Code);
    }

    private static Result<KnowledgeBase> Create(
        IReadOnlyList<Exercise>? exercises = null,
        IReadOnlyList<Skill>? skills = null,
        IReadOnlyList<RoutineProgram>? programs = null) =>
        KnowledgeBase.Create(
            exercises ?? [Conditioning()],
            skills ?? [ValidSkill()],
            programs ?? [ValidProgram()]);

    private static Exercise Conditioning(
        string id = "push_up",
        ExerciseGroup? group = ExerciseGroup.Push,
        string? regressionId = null,
        string? skillId = null,
        bool tracksMaximum = false) =>
        new()
        {
            Id = id,
            Name = $"Ejercicio {id}",
            Kind = ExerciseKind.Conditioning,
            Group = group,
            Metric = Metric.Reps,
            TracksMaximum = tracksMaximum,
            RegressionId = regressionId,
            SkillId = skillId,
        };

    private static Exercise SkillMovement(
        string id,
        string skillId,
        ExerciseGroup? group = null) =>
        new()
        {
            Id = id,
            Name = $"Movimiento {id}",
            Kind = ExerciseKind.Skill,
            Group = group,
            Metric = Metric.Seconds,
            TracksMaximum = false,
            RegressionId = null,
            SkillId = skillId,
        };

    private static SkillStage Stage(
        int order,
        string exerciseId = "push_up",
        int target = 10,
        int sets = 3) =>
        new()
        {
            Order = order,
            Name = $"Etapa {order}",
            ExerciseId = exerciseId,
            Criterion = new StageCriterion { Metric = Metric.Seconds, Target = target, Sets = sets },
            Notes = string.Empty,
        };

    private static RoutineItem Item(
        string exerciseId = "push_up",
        int sets = 3,
        int? repsMin = 5,
        int? repsMax = 8,
        int? holdSecondsMin = null,
        int? holdSecondsMax = null,
        int restSeconds = 60,
        int? supersetGroup = null) =>
        new()
        {
            ExerciseId = exerciseId,
            Sets = sets,
            RepsMin = repsMin,
            RepsMax = repsMax,
            HoldSecondsMin = holdSecondsMin,
            HoldSecondsMax = holdSecondsMax,
            RestSeconds = restSeconds,
            SupersetGroup = supersetGroup,
        };

    private static PatternRoutine PatternRoutine(
        string id = "r1",
        IReadOnlyList<RoutineItem>? items = null) =>
        new()
        {
            Id = id,
            Name = $"Modelo {id}",
            Intensity = 2,
            Equipment = null,
            Items = items ?? [Item()],
        };

    private static Skill ValidSkill(
        string id = "planche",
        ExerciseGroup group = ExerciseGroup.Push,
        IReadOnlyList<SkillStage>? stages = null,
        IReadOnlyList<PatternRoutine>? patternRoutines = null) =>
        new()
        {
            Id = id,
            Name = $"Skill {id}",
            Group = group,
            Lever = true,
            Stages = stages ?? [Stage(1), Stage(2), Stage(3), Stage(4)],
            PatternRoutines = patternRoutines ?? [PatternRoutine()],
        };

    private static RoutineBlock Block(
        int rounds = 3,
        int restSeconds = 0,
        IReadOnlyList<RoutineItem>? items = null) =>
        new()
        {
            Name = "SET 1",
            Rounds = rounds,
            RestSeconds = restSeconds,
            Notes = null,
            Items = items ?? [Item()],
        };

    private static RoutineTemplate Template(
        string id = "rutina-1",
        IReadOnlyList<RoutineBlock>? blocks = null) =>
        new()
        {
            Id = id,
            Name = $"Rutina {id}",
            Intensity = 2,
            DurationMinutes = 10,
            Blocks = blocks ?? [Block()],
        };

    private static RoutineProgram ValidProgram(
        string id = "ponte-en-forma",
        IReadOnlyList<RoutineTemplate>? routines = null) =>
        new()
        {
            Id = id,
            Name = $"Programa {id}",
            Type = RoutineProgramType.Circuit,
            Description = null,
            Routines = routines ?? [Template()],
        };

    private static RoutineProgram ProgramWithItems(params RoutineItem[] items) =>
        ValidProgram(routines: [Template(blocks: [Block(items: items)])]);
}
