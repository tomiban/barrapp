using Barrapp.Domain.Athlete;
using Barrapp.Domain.Common;
using Barrapp.Domain.Knowledge;
using Barrapp.Domain.Objectives;
using Barrapp.Domain.Planning;

namespace Barrapp.Domain.UnitTests;

/// <summary>
/// Motor de la sesión suelta (#28): compone una sesión puntual a partir de tiempo, energía y foco.
/// El tiempo fija cuánto trabajo entra (15→1 hueco, 30→2, 45→3, 60→4), la energía las series y la
/// reserva (baja → 2 series y RIR 4; media → 3 y RIR 3; alta → 4 y RIR 2). El foco filtra el
/// catálogo: un patrón centra la sesión en ese grupo (ancla con el máximo del atleta y variantes
/// del grupo), el skill abre con la etapa actual de su escalera y sus rutinas de patrón, y
/// «sorpréndeme» elige de forma determinista entre skill y patrones a partir de una semilla de las
/// entradas. Solo se prueba por su interfaz pública.
/// </summary>
public sealed class SoloSessionGeneratorTests
{
    private static readonly Guid UserId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private const string PushSkillId = "planche";
    private const string LegSkillId = "pistol-squat";

    [Fact]
    public void Generate_with_pattern_focus_builds_the_session_around_that_pattern()
    {
        var result = SoloSessionGenerator.Generate(
            BuildProfile(),
            BuildObjective(),
            stageOrder: null,
            Parameters(time: SoloSessionTime.Minutes30, energy: SoloSessionEnergy.Medium, focus: SoloSessionFocus.Pattern, pattern: ExerciseGroup.Push),
            Catalog());

        Assert.True(result.IsSuccess);
        var items = result.Value.Items;

        // 30 min → 2 huecos de fuerza + core de cierre.
        Assert.Equal(
            new[] { SessionItemRole.Strength, SessionItemRole.Strength, SessionItemRole.Core },
            items.Select(item => item.Role));

        Assert.All(
            items.Where(item => item.Role == SessionItemRole.Strength),
            item =>
            {
                Assert.Equal(ExerciseGroup.Push, item.Pattern);
                Assert.NotEqual(ExerciseGroup.Pull, item.Pattern);
                Assert.NotEqual(ExerciseGroup.Leg, item.Pattern);
            });

        Assert.Equal("push_up", items[0].ExerciseId);
    }

    [Theory]
    [InlineData(SoloSessionTime.Minutes15, 1)]
    [InlineData(SoloSessionTime.Minutes30, 3)]
    [InlineData(SoloSessionTime.Minutes45, 4)]
    [InlineData(SoloSessionTime.Minutes60, 5)]
    public void Generate_scales_the_work_with_time(
        SoloSessionTime time,
        int expectedItemCount)
    {
        var session = SoloSessionGenerator.Generate(
            BuildProfile(),
            BuildObjective(),
            stageOrder: null,
            Parameters(time, SoloSessionEnergy.Medium, SoloSessionFocus.Pattern, ExerciseGroup.Push),
            Catalog()).Value;

        // Más tiempo -> más ejercicios de fuerza (huecos) y el core aparece desde 30 min.
        Assert.Equal(expectedItemCount, session.Items.Count);
    }

    [Theory]
    [InlineData(SoloSessionEnergy.Low, 2, 6)]
    [InlineData(SoloSessionEnergy.Medium, 3, 7)]
    [InlineData(SoloSessionEnergy.High, 4, 8)]
    public void Generate_scales_sets_and_reserve_with_energy(
        SoloSessionEnergy energy,
        int expectedSets,
        int expectedRepsMax)
    {
        // Máximo de flexiones 10: RIR 4 deja el tope en 6, RIR 3 en 7 y RIR 2 en 8.
        var profile = BuildProfile(pushUpMaximum: 10);

        var session = SoloSessionGenerator.Generate(
            profile,
            BuildObjective(),
            stageOrder: null,
            Parameters(SoloSessionTime.Minutes30, energy, SoloSessionFocus.Pattern, ExerciseGroup.Push),
            Catalog()).Value;

        Assert.All(
            session.Items.Where(item => item.Role == SessionItemRole.Strength),
            item =>
            {
                Assert.Equal(expectedSets, item.Sets);
                Assert.Equal(expectedRepsMax, item.RepsMax);
            });
    }

    [Fact]
    public void Generate_with_skill_focus_leads_with_the_current_stage_and_its_pattern_routine()
    {
        var catalog = Catalog();
        var stage = catalog.FindSkill(PushSkillId)!.Stages.Single(candidate => candidate.Order == 2);

        var session = SoloSessionGenerator.Generate(
            BuildProfile(),
            BuildObjective(),
            stageOrder: 2,
            Parameters(SoloSessionTime.Minutes30, SoloSessionEnergy.Medium, SoloSessionFocus.Skill, pattern: null),
            catalog).Value;

        // La escalera aparece al inicio, sin tocar la marca del criterio (solo ajusta series por energía).
        var skillItem = session.Items[0];
        Assert.Equal(SessionItemRole.Skill, skillItem.Role);
        Assert.Equal(stage.ExerciseId, skillItem.ExerciseId);
        Assert.Equal(stage.Criterion.Target, skillItem.HoldSecondsMax);
        Assert.Equal(stage.Criterion.Sets, skillItem.Sets);

        // La fuerza viene de la primera rutina de patrón del skill, no de los anclas del perfil.
        var strengthItems = session.Items.Where(item => item.Role == SessionItemRole.Strength).ToList();
        Assert.NotEmpty(strengthItems);
        Assert.Equal("push_up", strengthItems[0].ExerciseId);
        Assert.Equal(3, strengthItems[0].Sets);
        Assert.Equal(5, strengthItems[0].RepsMin);
        Assert.Equal(8, strengthItems[0].RepsMax);
    }

    [Fact]
    public void Generate_with_skill_focus_fails_when_the_stage_is_not_in_the_ladder()
    {
        var result = SoloSessionGenerator.Generate(
            BuildProfile(),
            BuildObjective(),
            stageOrder: 99,
            Parameters(SoloSessionTime.Minutes30, SoloSessionEnergy.Medium, SoloSessionFocus.Skill, pattern: null),
            Catalog());

        Assert.True(result.IsFailure);
        Assert.Equal(DomainErrors.Plan.UnknownStage, result.Error);
    }

    [Fact]
    public void Generate_filters_the_catalog_by_the_focus_pattern()
    {
        var session = SoloSessionGenerator.Generate(
            BuildProfile(),
            BuildObjective(),
            stageOrder: null,
            Parameters(SoloSessionTime.Minutes60, SoloSessionEnergy.High, SoloSessionFocus.Pattern, ExerciseGroup.Push),
            Catalog()).Value;

        // Todo el trabajo de fuerza es de empuje; ni tirón ni pierna.
        Assert.All(
            session.Items.Where(item => item.Role == SessionItemRole.Strength),
            item => Assert.Equal(ExerciseGroup.Push, item.Pattern));
    }

    [Theory]
    [InlineData(6)]
    [InlineData(10)]
    public void Generate_never_prescribes_strength_reps_to_failure(int pushUpMaximum)
    {
        var profile = BuildProfile(pushUpMaximum: pushUpMaximum);
        foreach (var energy in new[] { SoloSessionEnergy.Low, SoloSessionEnergy.Medium, SoloSessionEnergy.High })
        {
            var session = SoloSessionGenerator.Generate(
                profile,
                BuildObjective(),
                stageOrder: null,
                Parameters(SoloSessionTime.Minutes45, energy, SoloSessionFocus.Pattern, ExerciseGroup.Push),
                Catalog()).Value;

            Assert.All(
                session.Items.Where(item => item.Role == SessionItemRole.Strength),
                item =>
                {
                    Assert.True(item.RepsMin >= 1, $"{item.ExerciseId} no puede prescribir 0 repeticiones");
                    Assert.True(
                        item.RepsMax < pushUpMaximum,
                        $"{item.ExerciseId}: {item.RepsMax} reps llegan al fallo con un máximo de {pushUpMaximum}");
                });
        }
    }

    [Fact]
    public void Generate_with_surprise_focus_is_deterministic()
    {
        var first = SoloSessionGenerator.Generate(
            BuildProfile(),
            BuildObjective(),
            stageOrder: null,
            Parameters(SoloSessionTime.Minutes45, SoloSessionEnergy.High, SoloSessionFocus.Surprise, pattern: null),
            Catalog()).Value;
        var second = SoloSessionGenerator.Generate(
            BuildProfile(),
            BuildObjective(),
            stageOrder: null,
            Parameters(SoloSessionTime.Minutes45, SoloSessionEnergy.High, SoloSessionFocus.Surprise, pattern: null),
            Catalog()).Value;

        Assert.Equal(Snapshot(first), Snapshot(second));
    }

    [Fact]
    public void ResolveFocus_for_surprise_is_deterministic_and_spans_compositions()
    {
        // «Sorpréndeme» varía con las entradas (tiempo, energía y skill): la semilla es estable, así
        // que los mismos parámetros eligen siempre lo mismo, pero combinaciones distintas no se
        // quedan siempre en el mismo foco.
        var first = SoloSessionGenerator.ResolveFocus(
            Parameters(SoloSessionTime.Minutes30, SoloSessionEnergy.Medium, SoloSessionFocus.Surprise, pattern: null),
            BuildObjective());
        var second = SoloSessionGenerator.ResolveFocus(
            Parameters(SoloSessionTime.Minutes30, SoloSessionEnergy.Medium, SoloSessionFocus.Surprise, pattern: null),
            BuildObjective());

        Assert.True(first.IsSuccess);
        Assert.Equal(first.Value, second.Value);

        var compositions = new HashSet<string>();
        foreach (var skillId in new[] { PushSkillId, LegSkillId })
        {
            foreach (var time in new[] { SoloSessionTime.Minutes15, SoloSessionTime.Minutes30, SoloSessionTime.Minutes45, SoloSessionTime.Minutes60 })
            {
                foreach (var energy in new[] { SoloSessionEnergy.Low, SoloSessionEnergy.High })
                {
                    var composition = SoloSessionGenerator.ResolveFocus(
                        Parameters(time, energy, SoloSessionFocus.Surprise, pattern: null),
                        BuildObjective(skillId)).Value;

                    compositions.Add(Snapshot(composition));
                }
            }
        }

        Assert.True(compositions.Count >= 2, "sorpréndeme debe repartir entre skill y patrones");
    }

    [Fact]
    public void Generate_fails_for_pattern_focus_without_a_pattern()
    {
        var result = SoloSessionGenerator.Generate(
            BuildProfile(),
            BuildObjective(),
            stageOrder: null,
            Parameters(SoloSessionTime.Minutes30, SoloSessionEnergy.Medium, SoloSessionFocus.Pattern, pattern: null),
            Catalog());

        Assert.True(result.IsFailure);
        Assert.Equal(DomainErrors.SessionSuelta.PatternRequired, result.Error);
    }

    [Theory]
    [InlineData(ExerciseGroup.Core)]
    [InlineData(ExerciseGroup.Cardio)]
    public void Generate_fails_for_an_unsupported_focus_pattern(ExerciseGroup pattern)
    {
        var result = SoloSessionGenerator.Generate(
            BuildProfile(),
            BuildObjective(),
            stageOrder: null,
            Parameters(SoloSessionTime.Minutes30, SoloSessionEnergy.Medium, SoloSessionFocus.Pattern, pattern),
            Catalog());

        Assert.True(result.IsFailure);
        Assert.Equal(DomainErrors.SessionSuelta.UnsupportedPattern, result.Error);
    }

    [Fact]
    public void Generate_fails_for_an_unknown_objective_skill()
    {
        // El objetivo apunta a planche (válido en el catálogo original), pero el motor trabaja contra
        // un catálogo sin ese skill: debe fallar como skill desconocido.
        var catalog = Catalog();
        var catalogWithoutPlanche = new KnowledgeBaseCatalog(
            catalog.Exercises.Where(exercise => exercise.SkillId is null or LegSkillId).ToList(),
            [catalog.FindSkill(LegSkillId)!],
            []);

        var result = SoloSessionGenerator.Generate(
            BuildProfile(),
            BuildObjective(PushSkillId),
            stageOrder: null,
            Parameters(SoloSessionTime.Minutes30, SoloSessionEnergy.Medium, SoloSessionFocus.Pattern, ExerciseGroup.Push),
            catalogWithoutPlanche);

        Assert.True(result.IsFailure);
        Assert.Equal(DomainErrors.Objective.UnknownSkill, result.Error);
    }

    [Fact]
    public void Generate_fails_when_the_core_anchor_is_missing_for_a_long_enough_session()
    {
        var catalog = Catalog();
        var withoutCore = new KnowledgeBaseCatalog(catalog.Exercises
            .Where(exercise => exercise.Id != "hollow-body-hold")
            .ToList(), catalog.Skills, catalog.Programs);

        var result = SoloSessionGenerator.Generate(
            BuildProfile(),
            BuildObjective(),
            stageOrder: null,
            Parameters(SoloSessionTime.Minutes30, SoloSessionEnergy.Medium, SoloSessionFocus.Skill, pattern: null),
            withoutCore);

        Assert.True(result.IsFailure);
        Assert.Equal(DomainErrors.Plan.UnknownExercise("hollow-body-hold"), result.Error);
    }

    [Fact]
    public void Generate_keeps_the_fifteen_minute_session_without_core()
    {
        var session = SoloSessionGenerator.Generate(
            BuildProfile(),
            BuildObjective(),
            stageOrder: null,
            Parameters(SoloSessionTime.Minutes15, SoloSessionEnergy.Medium, SoloSessionFocus.Pattern, ExerciseGroup.Push),
            Catalog()).Value;

        Assert.DoesNotContain(session.Items, item => item.Role == SessionItemRole.Core);
        Assert.Single(session.Items);
    }

    private static SoloSessionParameters Parameters(
        SoloSessionTime time,
        SoloSessionEnergy energy,
        SoloSessionFocus focus,
        ExerciseGroup? pattern) =>
        new(time, energy, focus, pattern);

    private static string Snapshot(Session session) =>
        string.Join(
            "|",
            session.Items.Select(item =>
                $"{item.ExerciseId}:{item.Role}:{item.Pattern}:{item.Sets}:"
                + $"{item.RepsMin}-{item.RepsMax}:{item.HoldSecondsMin}-{item.HoldSecondsMax}:{item.Note}"));

    private static string Snapshot(SoloSessionComposition composition) =>
        $"{composition.Kind}:{composition.Pattern}:{composition.SkillId}";

    private static AthleteProfile BuildProfile(
        int pushUpMaximum = 10,
        int pullUpMaximum = 5,
        int squatMaximum = 20) =>
        AthleteProfile.Create(
            UserId,
            78,
            180,
            180,
            85,
            3,
            [
                new MaximumInput("push_up", pushUpMaximum),
                new MaximumInput("pull_up", pullUpMaximum),
                new MaximumInput("squat", squatMaximum),
            ]).Value;

    private static Objective BuildObjective(string skillId = PushSkillId) =>
        Objective.Create(UserId, skillId, Catalog()).Value;

    private static KnowledgeBase Catalog()
    {
        var exercises = new List<Exercise>
        {
            Conditioning("push_up", ExerciseGroup.Push),
            Conditioning("pull_up", ExerciseGroup.Pull),
            Conditioning("squat", ExerciseGroup.Leg),
            Conditioning("hollow-body-hold", ExerciseGroup.Core, Metric.Seconds),
            Conditioning("diamond-push-up", ExerciseGroup.Push),
            Conditioning("archer-push-up", ExerciseGroup.Push),
            Conditioning("knee-push-up", ExerciseGroup.Push),
            Conditioning("chin-up", ExerciseGroup.Pull),
            Conditioning("box-squat", ExerciseGroup.Leg),
            Conditioning("split-squat", ExerciseGroup.Leg),
            SkillMovement("planche-lean"),
            SkillMovement("planche-tuck"),
            SkillMovement("planche-advanced-tuck"),
            SkillMovement("planche-full"),
            SkillMovement("pistol-box", LegSkillId, Metric.Reps),
            SkillMovement("pistol-assisted", LegSkillId, Metric.Reps),
            SkillMovement("pistol-negative", LegSkillId, Metric.Reps),
            SkillMovement("pistol-full", LegSkillId, Metric.Reps),
        };

        var planche = new Skill
        {
            Id = PushSkillId,
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
                            RestSeconds = 90,
                        },
                        new RoutineItem
                        {
                            ExerciseId = "diamond-push-up",
                            Sets = 3,
                            RepsMin = 5,
                            RepsMax = 8,
                            RestSeconds = 90,
                        },
                    ],
                },
            ],
        };

        var pistol = new Skill
        {
            Id = LegSkillId,
            Name = "Pistol squat",
            Group = ExerciseGroup.Leg,
            Lever = false,
            Stages =
            [
                Stage(1, "pistol-box", Metric.Reps, target: 5, sets: 3),
                Stage(2, "pistol-assisted", Metric.Reps, target: 5, sets: 3),
                Stage(3, "pistol-negative", Metric.Reps, target: 4, sets: 3),
                Stage(4, "pistol-full", Metric.Reps, target: 3, sets: 3),
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
                            ExerciseId = "squat",
                            Sets = 3,
                            RepsMin = 6,
                            RepsMax = 8,
                            RestSeconds = 90,
                        },
                    ],
                },
            ],
        };

        var result = KnowledgeBase.Create(exercises, [planche, pistol], []);
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

    private static Exercise SkillMovement(string id, string skillId = PushSkillId, Metric metric = Metric.Seconds) =>
        new()
        {
            Id = id,
            Name = id,
            Kind = ExerciseKind.Skill,
            Group = null,
            Metric = metric,
            SkillId = skillId,
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

/// <summary>Catalogo de solo lectura para la prueba del core ausente.</summary>
internal sealed class KnowledgeBaseCatalog(
    IReadOnlyList<Exercise> exercises,
    IReadOnlyList<Skill> skills,
    IReadOnlyList<RoutineProgram> programs) : IGenerationCatalog
{
    private readonly KnowledgeBase knowledge = KnowledgeBase.Create(exercises, skills, programs).Value;

    public Exercise? FindExercise(string exerciseId) => knowledge.FindExercise(exerciseId);

    public Skill? FindSkill(string skillId) => knowledge.FindSkill(skillId);

    public IReadOnlyList<Exercise> ExercisesByGroup(ExerciseGroup group) =>
        knowledge.ExercisesByGroup(group);
}
