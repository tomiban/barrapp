using Barrapp.Domain.Athlete;
using Barrapp.Domain.Common;
using Barrapp.Domain.Knowledge;
using Barrapp.Domain.Objectives;
using Barrapp.Domain.Planning;

namespace Barrapp.Domain.UnitTests;

/// <summary>
/// Reglas del motor de generación (#10): da un mesociclo de cuatro semanas con reparto full-body
/// de 3 días o alterno tren superior / tren inferior de 4 días (#15), cada patrón recibe trabajo
/// al menos dos veces por semana, cada sesión empieza por el bloque de skill en la etapa actual del
/// atleta (#14) y el resultado es determinista. La carga de fuerza se deriva del máximo del atleta
/// y nunca llega al fallo (#11); el bloque de skill se ajusta por la palanca del atleta en los
/// skills apalancados (#68). El RIR baja de 3 a 1 en las tres primeras semanas, lo que sube las
/// reps y el volumen (#12); la semana 4 es un deload con RIR 4 y ~50 % del volumen de la semana 3
/// (#13). Con un máximo de 0, el hueco de fuerza de ese patrón pasa a la regresión del ejercicio
/// (#17). Solo se prueba por su interfaz pública.
/// </summary>
public sealed class PlanGeneratorTests
{
    private static readonly Guid UserId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private const string SkillId = "planche";
    private const string PistolSkillId = "pistol-squat";

    /// <summary>Etapa 1 de la escalera, explícita en los tests que fijan esa etapa.</summary>
    private const int FirstStageOrder = 1;

    [Fact]
    public void Generate_returns_a_four_week_plan_with_three_sessions_per_week()
    {
        var result = PlanGenerator.Generate(BuildProfile(3), BuildObjective(), FirstStageOrder, Catalog());

        Assert.True(result.IsSuccess);
        Assert.Equal(SkillId, result.Value.SkillId);
        Assert.Equal(3, result.Value.TrainingDays);
        Assert.Equal(4, result.Value.Microcycles.Count);
        Assert.All(result.Value.Microcycles, microcycle => Assert.Equal(3, microcycle.Sessions.Count));
    }

    [Fact]
    public void Generate_for_four_days_alternates_upper_and_lower_sessions()
    {
        var plan = PlanGenerator.Generate(BuildProfile(4), BuildObjective(), FirstStageOrder, Catalog()).Value;

        Assert.Equal(4, plan.TrainingDays);
        Assert.All(plan.Microcycles, microcycle => Assert.Equal(4, microcycle.Sessions.Count));

        foreach (var microcycle in plan.Microcycles)
        {
            // Días 1 y 3: tren superior (bloque de skill + empuje + tirón + core).
            foreach (var day in new[] { 1, 3 })
            {
                var session = microcycle.Sessions.Single(s => s.Day == day);
                Assert.Equal(
                    new[]
                    {
                        SessionItemRole.Skill,
                        SessionItemRole.Strength,
                        SessionItemRole.Strength,
                        SessionItemRole.Core,
                    },
                    session.Items.Select(item => item.Role));
                Assert.Equal(
                    new[] { "push_up", "pull_up" },
                    session.Items
                        .Where(item => item.Role == SessionItemRole.Strength)
                        .Select(item => item.ExerciseId));
            }

            // Días 2 y 4: tren inferior (pierna + core).
            foreach (var day in new[] { 2, 4 })
            {
                var session = microcycle.Sessions.Single(s => s.Day == day);
                Assert.Equal(
                    new[] { SessionItemRole.Strength, SessionItemRole.Core },
                    session.Items.Select(item => item.Role));
                Assert.Equal(
                    new[] { "squat" },
                    session.Items
                        .Where(item => item.Role == SessionItemRole.Strength)
                        .Select(item => item.ExerciseId));
            }
        }
    }

    [Fact]
    public void Generate_for_four_days_puts_the_skill_block_only_on_upper_days()
    {
        var plan = PlanGenerator.Generate(BuildProfile(4), BuildObjective(), FirstStageOrder, Catalog()).Value;

        foreach (var microcycle in plan.Microcycles)
        {
            // El skill se practica fresco en los dos días de tren superior (1 y 3) y no aparece en
            // los de tren inferior (2 y 4).
            Assert.All(
                microcycle.Sessions.Where(session => session.Day is 1 or 3),
                session => Assert.Equal(SessionItemRole.Skill, session.Items[0].Role));

            Assert.All(
                microcycle.Sessions.Where(session => session.Day is 2 or 4),
                session => Assert.DoesNotContain(session.Items, item => item.Role == SessionItemRole.Skill));
        }
    }

    [Fact]
    public void Generate_for_four_days_gives_every_strength_pattern_work_twice_per_week()
    {
        var plan = PlanGenerator.Generate(BuildProfile(4), BuildObjective(), FirstStageOrder, Catalog()).Value;

        foreach (var microcycle in plan.Microcycles)
        {
            var workByPattern = microcycle.Sessions
                .SelectMany(session => session.Items)
                .Where(item => item.Role == SessionItemRole.Strength)
                .GroupBy(item => item.Pattern!.Value)
                .ToDictionary(group => group.Key, group => group.Count());

            // El reparto alterno (US-12) da a cada patrón exactamente 2×/semana: empuje y tirón
            // trabajan en los días superiores y la pierna en los inferiores.
            Assert.Equal(2, workByPattern[ExerciseGroup.Push]);
            Assert.Equal(2, workByPattern[ExerciseGroup.Pull]);
            Assert.Equal(2, workByPattern[ExerciseGroup.Leg]);
        }
    }

    [Fact]
    public void Generate_for_four_days_applies_the_rir_wave_to_the_upper_day_strength()
    {
        // La onda de RIR (#12) atraviesa el reparto: con un máximo de 10, el tope prescrito en el
        // tren superior sube 7 → 8 → 9 en las tres primeras semanas.
        const int maximum = 10;
        var plan = PlanGenerator
            .Generate(BuildProfile(4, maximum, maximum, maximum), BuildObjective(), FirstStageOrder, Catalog())
            .Value;

        Assert.Equal(7, StrengthItem(plan, 0, day: 1, "push_up").RepsMax); // RIR 3 (base)
        Assert.Equal(8, StrengthItem(plan, 1, day: 1, "push_up").RepsMax); // RIR 2
        Assert.Equal(9, StrengthItem(plan, 2, day: 1, "push_up").RepsMax); // RIR 1
    }

    [Fact]
    public void Generate_gives_every_strength_pattern_work_at_least_twice_per_week()
    {
        var plan = PlanGenerator.Generate(BuildProfile(3), BuildObjective(), FirstStageOrder, Catalog()).Value;

        foreach (var microcycle in plan.Microcycles)
        {
            var workByPattern = microcycle.Sessions
                .SelectMany(session => session.Items)
                .Where(item => item.Role == SessionItemRole.Strength)
                .GroupBy(item => item.Pattern!.Value)
                .ToDictionary(group => group.Key, group => group.Count());

            // Invariante compartido por todos los repartos: ningún patrón se queda por debajo de
            // ~2×/semana. El full-body de 3 días lo supera a propósito (US11: «cubrirlo todo en
            // cada sesión»): cada patrón aparece una vez por sesión, 3 veces por semana. Lo fija
            // el test de composición de la sesión; aquí basta el suelo.
            Assert.True(workByPattern.GetValueOrDefault(ExerciseGroup.Push) >= 2);
            Assert.True(workByPattern.GetValueOrDefault(ExerciseGroup.Pull) >= 2);
            Assert.True(workByPattern.GetValueOrDefault(ExerciseGroup.Leg) >= 2);
        }
    }

    [Fact]
    public void Generate_builds_each_session_as_skill_then_strength_by_pattern_then_core()
    {
        var plan = PlanGenerator.Generate(BuildProfile(3), BuildObjective(), FirstStageOrder, Catalog()).Value;

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

        var plan = PlanGenerator.Generate(BuildProfile(3), BuildObjective(), FirstStageOrder, catalog).Value;

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
    public void Generate_practises_the_current_stage_in_the_skill_block()
    {
        var catalog = Catalog();
        var currentStage = catalog.FindSkill(SkillId)!.Stages.Single(stage => stage.Order == 3);

        var plan = PlanGenerator.Generate(
            BuildProfile(3),
            BuildObjective(),
            currentStage.Order,
            catalog).Value;

        foreach (var session in plan.Microcycles.SelectMany(microcycle => microcycle.Sessions))
        {
            var firstItem = session.Items[0];

            Assert.Equal(SessionItemRole.Skill, firstItem.Role);
            Assert.Equal(currentStage.ExerciseId, firstItem.ExerciseId);
            Assert.Equal(currentStage.Criterion.Sets, firstItem.Sets);
            Assert.Equal(currentStage.Criterion.Target, firstItem.HoldSecondsMax);
            Assert.Null(firstItem.RepsMax);
        }
    }

    [Fact]
    public void Generate_builds_a_reps_skill_block_for_a_reps_stage()
    {
        var catalog = Catalog();
        var currentStage = catalog.FindSkill(PistolSkillId)!.Stages.Single(stage => stage.Order == 2);

        var plan = PlanGenerator.Generate(
            BuildProfile(3),
            BuildObjective(PistolSkillId),
            currentStage.Order,
            catalog).Value;

        var firstItem = plan.Microcycles[0].Sessions[0].Items[0];

        Assert.Equal(SessionItemRole.Skill, firstItem.Role);
        Assert.Equal(currentStage.ExerciseId, firstItem.ExerciseId);
        Assert.Equal(currentStage.Criterion.Sets, firstItem.Sets);
        Assert.Equal(currentStage.Criterion.Target, firstItem.RepsMin);
        Assert.Equal(currentStage.Criterion.Target, firstItem.RepsMax);
        Assert.Null(firstItem.HoldSecondsMax);
    }

    [Fact]
    public void Generate_defaults_to_the_first_stage_when_there_is_no_progress()
    {
        var catalog = Catalog();
        var firstStage = catalog.FindSkill(SkillId)!.Stages.Single(stage => stage.Order == 1);

        var plan = PlanGenerator
            .Generate(BuildProfile(3), BuildObjective(), stageOrder: null, catalog)
            .Value;

        Assert.All(
            plan.Microcycles.SelectMany(microcycle => microcycle.Sessions),
            session => Assert.Equal(firstStage.ExerciseId, session.Items[0].ExerciseId));
    }

    [Fact]
    public void Generate_fails_when_the_current_stage_is_not_in_the_ladder()
    {
        var result = PlanGenerator.Generate(BuildProfile(3), BuildObjective(), stageOrder: 99, Catalog());

        Assert.True(result.IsFailure);
        Assert.Equal(DomainErrors.Plan.UnknownStage, result.Error);
    }

    [Fact]
    public void Generate_characterises_the_base_week_strength_reps_for_each_maximum()
    {
        // Semana base (RIR 3): el tope es máximo - 3 y el rango baja dos repeticiones.
        var plan = PlanGenerator
            .Generate(
                BuildProfile(3, pushUpMaximum: 8, pullUpMaximum: 4, squatMaximum: 20),
                BuildObjective(),
                FirstStageOrder,
                Catalog())
            .Value;

        Assert.Equal(3, StrengthItem(plan, "push_up").RepsMin);
        Assert.Equal(5, StrengthItem(plan, "push_up").RepsMax);
        Assert.Equal(1, StrengthItem(plan, "pull_up").RepsMin);
        Assert.Equal(1, StrengthItem(plan, "pull_up").RepsMax);
        Assert.Equal(15, StrengthItem(plan, "squat").RepsMin);
        Assert.Equal(17, StrengthItem(plan, "squat").RepsMax);
    }

    [Fact]
    public void Generate_scales_strength_reps_with_the_maximum()
    {
        var lowMaximums = PlanGenerator
            .Generate(BuildProfile(3, pushUpMaximum: 6, pullUpMaximum: 6, squatMaximum: 6), BuildObjective(), FirstStageOrder, Catalog())
            .Value;
        var highMaximums = PlanGenerator
            .Generate(BuildProfile(3, pushUpMaximum: 15, pullUpMaximum: 15, squatMaximum: 15), BuildObjective(), FirstStageOrder, Catalog())
            .Value;

        foreach (var exerciseId in new[] { "push_up", "pull_up", "squat" })
        {
            Assert.True(
                StrengthItem(highMaximums, exerciseId).RepsMax > StrengthItem(lowMaximums, exerciseId).RepsMax,
                $"{exerciseId} debe prescribir más reps con un máximo mayor");
        }
    }

    [Fact]
    public void Generate_descends_the_strength_reserve_across_the_first_three_microcycles()
    {
        // Onda de RIR (#12): la semana 1 deja 3 reps en reserva, la 2 dos y la 3 una. Con un mismo
        // máximo, el tope prescrito sube una repetición por semana.
        const int maximum = 10;

        var plan = PlanGenerator
            .Generate(BuildProfile(3, maximum, maximum, maximum), BuildObjective(), FirstStageOrder, Catalog())
            .Value;

        foreach (var exerciseId in new[] { "push_up", "pull_up", "squat" })
        {
            Assert.Equal(7, StrengthItem(plan, 0, exerciseId).RepsMax); // RIR 3 (base)
            Assert.Equal(8, StrengthItem(plan, 1, exerciseId).RepsMax); // RIR 2
            Assert.Equal(9, StrengthItem(plan, 2, exerciseId).RepsMax); // RIR 1
        }
    }

    [Fact]
    public void Generate_raises_strength_volume_across_the_first_three_microcycles()
    {
        // Al bajar el RIR suben las reps prescritas y, con las series fijas, el volumen total
        // (series × reps) crece semana a semana.
        var plan = PlanGenerator
            .Generate(BuildProfile(3, 10, 10, 10), BuildObjective(), FirstStageOrder, Catalog())
            .Value;

        var volumeByMicrocycle = VolumeByMicrocycle(plan, 3);

        Assert.True(volumeByMicrocycle[0] < volumeByMicrocycle[1], "el volumen de la semana 2 debe superar al de la 1");
        Assert.True(volumeByMicrocycle[1] < volumeByMicrocycle[2], "el volumen de la semana 3 debe superar al de la 2");
    }

    [Theory]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(10)]
    public void Generate_never_reduces_strength_volume_across_the_first_three_microcycles(int maximum)
    {
        // La onda nunca quita volumen: lo sube o, cuando el máximo no da margen para reservar más
        // reps (≤ 2), lo deja plano. Convertir ese caso en una regresión real es #17.
        var plan = PlanGenerator
            .Generate(BuildProfile(3, maximum, maximum, maximum), BuildObjective(), FirstStageOrder, Catalog())
            .Value;

        var volumeByMicrocycle = VolumeByMicrocycle(plan, 3);

        Assert.True(volumeByMicrocycle[0] <= volumeByMicrocycle[1], "la semana 2 no puede bajar el volumen de la 1");
        Assert.True(volumeByMicrocycle[1] <= volumeByMicrocycle[2], "la semana 3 no puede bajar el volumen de la 2");
    }

    [Fact]
    public void Generate_raises_the_strength_reserve_to_four_in_the_deload_microcycle()
    {
        // Deload (#13): la semana 4 deja 4 reps en reserva (RIR 4), el tope más conservador de la
        // onda. Con un mismo máximo, el tope prescrito baja respecto a la semana 3 (RIR 1).
        const int maximum = 10;

        var plan = PlanGenerator
            .Generate(BuildProfile(3, maximum, maximum, maximum), BuildObjective(), FirstStageOrder, Catalog())
            .Value;

        foreach (var exerciseId in new[] { "push_up", "pull_up", "squat" })
        {
            Assert.Equal(9, StrengthItem(plan, 2, exerciseId).RepsMax); // RIR 1 (semana 3)
            Assert.Equal(6, StrengthItem(plan, 3, exerciseId).RepsMax); // RIR 4 (deload)
        }
    }

    [Fact]
    public void Generate_halves_the_strength_volume_in_the_deload_microcycle()
    {
        // Deload (#13): la semana 4 baja las series de fuerza (3 → 2) y sube el RIR a 4, dejando el
        // volumen (series × reps) en ~50 % del de la semana 3. Con los máximos concretos del
        // fixture (10/5/20) el cociente queda en ~48 %.
        var plan = PlanGenerator
            .Generate(BuildProfile(3), BuildObjective(), FirstStageOrder, Catalog())
            .Value;

        var volume = VolumeByMicrocycle(plan, 4);
        var ratio = volume[3] / (double)volume[2];

        Assert.True(volume[3] < volume[2], "el deload no puede subir el volumen de la semana 3");
        Assert.True(
            ratio is >= 0.4 and <= 0.6,
            $"el volumen del deload debe rondar el 50 % del de la semana 3 (fue {ratio:P0})");
    }

    [Fact]
    public void Generate_reduces_strength_and_core_sets_in_the_deload_microcycle()
    {
        // Deload (#13): la semana 4 descarga el volumen bajando las series de fuerza y de core
        // (3 → 2); el bloque de skill y la anatomía de la sesión no cambian.
        var plan = PlanGenerator
            .Generate(BuildProfile(3), BuildObjective(), FirstStageOrder, Catalog())
            .Value;

        foreach (var microcycle in plan.Microcycles)
        {
            var expectedSets = microcycle.Number == 4 ? 2 : 3;
            foreach (var session in microcycle.Sessions)
            {
                Assert.All(
                    session.Items.Where(item => item.Role == SessionItemRole.Strength),
                    item => Assert.Equal(expectedSets, item.Sets));
                Assert.Equal(expectedSets, session.Items[^1].Sets); // core
                Assert.Equal(3, session.Items[0].Sets); // skill: el deload no toca la escalera
            }
        }
    }

    [Theory]
    [InlineData(2, 2, 2)]
    [InlineData(7, 2, 12)]
    [InlineData(3, 4, 6)]
    [InlineData(20, 5, 8)]
    public void Generate_never_prescribes_strength_reps_to_failure(
        int pushUpMaximum,
        int pullUpMaximum,
        int squatMaximum)
    {
        var profile = BuildProfile(3, pushUpMaximum, pullUpMaximum, squatMaximum);

        var plan = PlanGenerator.Generate(profile, BuildObjective(), FirstStageOrder, Catalog()).Value;

        foreach (var item in AllStrengthItems(plan))
        {
            var maximum = profile.MaximumFor(item.ExerciseId);
            Assert.NotNull(maximum);

            // Un máximo de 0 o 1 no admite reserva; ese caso lo cubre el test de degenerados.
            if (maximum < 2)
            {
                continue;
            }

            Assert.True(item.RepsMin >= 1, $"{item.ExerciseId} no puede prescribir 0 repeticiones");
            Assert.True(item.RepsMin <= item.RepsMax, $"{item.ExerciseId} tiene un rango invertido");
            Assert.True(
                item.RepsMax < maximum,
                $"{item.ExerciseId}: {item.RepsMax} reps llegan al fallo con un máximo de {maximum}");
        }
    }

    [Fact]
    public void Generate_uses_the_pattern_regression_when_an_anchor_maximum_is_zero()
    {
        // #17: un máximo de 0 no admite ninguna prescripción sobre el ancla; el hueco de fuerza de
        // ese patrón pasa a la regresión (incline-push-up). Los patrones con máximo real conservan
        // su ancla y siguen derivando de ese máximo.
        var plan = PlanGenerator
            .Generate(
                BuildProfile(3, pushUpMaximum: 0, pullUpMaximum: 5, squatMaximum: 20),
                BuildObjective(),
                FirstStageOrder,
                Catalog())
            .Value;

        var pushItem = StrengthItem(plan, "incline-push-up");
        var pullItem = StrengthItem(plan, "pull_up");
        var squatItem = StrengthItem(plan, "squat");

        Assert.Equal(ExerciseGroup.Push, pushItem.Pattern);
        Assert.Equal(ExerciseGroup.Pull, pullItem.Pattern);
        Assert.Equal(ExerciseGroup.Leg, squatItem.Pattern);

        // La regresión se prescribe sobre una base de trabajo asumida y modesta con la onda de RIR:
        // semana base 3–5, semana 2 4–6, semana 3 5–7; la semana 4 es deload (#13) y baja a 2–4 (RIR 4).
        Assert.Equal(3, pushItem.RepsMin);
        Assert.Equal(5, pushItem.RepsMax);
        Assert.Equal(4, StrengthItem(plan, 1, "incline-push-up").RepsMin);
        Assert.Equal(6, StrengthItem(plan, 1, "incline-push-up").RepsMax);
        Assert.Equal(5, StrengthItem(plan, 2, "incline-push-up").RepsMin);
        Assert.Equal(7, StrengthItem(plan, 2, "incline-push-up").RepsMax);
        Assert.Equal(2, StrengthItem(plan, 3, "incline-push-up").RepsMin);
        Assert.Equal(4, StrengthItem(plan, 3, "incline-push-up").RepsMax);

        // Los patrones intactos mantienen sus reps derivadas del máximo (RIR 3 en la semana base).
        Assert.Equal(1, pullItem.RepsMin);
        Assert.Equal(2, pullItem.RepsMax);
        Assert.Equal(15, squatItem.RepsMin);
        Assert.Equal(17, squatItem.RepsMax);
    }

    [Fact]
    public void Generate_substitutes_every_pattern_by_its_regression_when_all_maximums_are_zero()
    {
        var plan = PlanGenerator
            .Generate(BuildProfile(3, 0, 0, 0), BuildObjective(), FirstStageOrder, Catalog())
            .Value;

        Assert.Equal(
            new[] { "incline-push-up", "negative-pull-up", "box-squat" },
            StrengthItems(plan).Select(item => item.ExerciseId));

        // La regresión debe prescribirse con margen (nunca al fallo) y nunca con 0 repeticiones.
        Assert.All(StrengthItems(plan), item =>
        {
            Assert.Equal(3, item.Sets);
            Assert.True(item.RepsMin >= 1, $"{item.ExerciseId} no puede prescribir 0 repeticiones");
            Assert.True(item.RepsMin <= item.RepsMax, $"{item.ExerciseId} tiene un rango invertido");
        });
    }

    [Fact]
    public void Generate_keeps_a_neutral_strength_placeholder_for_a_maximum_of_one()
    {
        var result = PlanGenerator
            .Generate(BuildProfile(3, 1, 1, 1), BuildObjective(), FirstStageOrder, Catalog());

        Assert.True(result.IsSuccess);

        // (1,1) es el marcador neutro documentado, no una prescripción segura. Un máximo de 1 no
        // deja margen para reservar, pero tampoco entra en la regresión (#17 solo aplica al 0).
        Assert.All(StrengthItems(result.Value), item =>
        {
            Assert.Equal(1, item.RepsMin);
            Assert.Equal(1, item.RepsMax);
        });
    }

    [Fact]
    public void Generate_adds_one_set_and_a_slower_note_to_the_skill_block_for_an_unfavorable_lever()
    {
        var catalog = Catalog();
        var stage = catalog.FindSkill(SkillId)!.Stages.Single(s => s.Order == 1);
        var plan = PlanGenerator.Generate(UnfavorableProfile(), BuildObjective(), FirstStageOrder, catalog).Value;

        foreach (var session in plan.Microcycles.SelectMany(microcycle => microcycle.Sessions))
        {
            var skillBlock = session.Items[0];

            Assert.Equal(stage.Criterion.Sets + 1, skillBlock.Sets);
            Assert.NotNull(skillBlock.Note);
            Assert.Contains("lento", skillBlock.Note);

            // Solo el bloque de skill se ajusta; fuerza y core no cambian ni llevan nota.
            Assert.All(session.Items.Skip(1), item => Assert.Null(item.Note));
        }
    }

    [Fact]
    public void Generate_removes_one_set_and_a_faster_note_from_the_skill_block_for_a_favorable_lever()
    {
        var catalog = Catalog();
        var stage = catalog.FindSkill(SkillId)!.Stages.Single(s => s.Order == 1);
        var plan = PlanGenerator.Generate(FavorableProfile(), BuildObjective(), FirstStageOrder, catalog).Value;

        foreach (var session in plan.Microcycles.SelectMany(microcycle => microcycle.Sessions))
        {
            var skillBlock = session.Items[0];

            Assert.Equal(stage.Criterion.Sets - 1, skillBlock.Sets);
            Assert.NotNull(skillBlock.Note);
            Assert.Contains("rápido", skillBlock.Note);
        }
    }

    [Fact]
    public void Generate_keeps_the_stage_criterion_unchanged_by_the_lever()
    {
        var catalog = Catalog();
        var stage = catalog.FindSkill(SkillId)!.Stages.Single(s => s.Order == 1);

        var favorableBlock = PlanGenerator
            .Generate(FavorableProfile(), BuildObjective(), FirstStageOrder, catalog).Value
            .Microcycles[0].Sessions[0].Items[0];
        var unfavorableBlock = PlanGenerator
            .Generate(UnfavorableProfile(), BuildObjective(), FirstStageOrder, catalog).Value
            .Microcycles[0].Sessions[0].Items[0];

        // El criterio (etapa, ejercicio y marca) no depende del cubo: solo cambia el volumen del bloque.
        Assert.Equal(favorableBlock.ExerciseId, unfavorableBlock.ExerciseId);
        Assert.Equal(stage.Criterion.Target, favorableBlock.HoldSecondsMax);
        Assert.Equal(stage.Criterion.Target, unfavorableBlock.HoldSecondsMax);
        Assert.Equal(stage.Criterion.Sets, favorableBlock.Sets + 1);
        Assert.Equal(stage.Criterion.Sets, unfavorableBlock.Sets - 1);
    }

    [Fact]
    public void Generate_leaves_a_non_levered_skill_unadjusted_and_without_a_note()
    {
        var catalog = Catalog(lever: false);
        var stage = catalog.FindSkill(SkillId)!.Stages.Single(s => s.Order == 1);
        var plan = PlanGenerator.Generate(UnfavorableProfile(), BuildObjective(), FirstStageOrder, catalog).Value;

        foreach (var session in plan.Microcycles.SelectMany(microcycle => microcycle.Sessions))
        {
            Assert.Equal(stage.Criterion.Sets, session.Items[0].Sets);
            Assert.Null(session.Items[0].Note);
        }
    }

    [Theory]
    [InlineData(3)]
    [InlineData(4)]
    public void Generate_is_deterministic_for_the_supported_frequencies(int trainingDays)
    {
        var first = PlanGenerator.Generate(BuildProfile(trainingDays), BuildObjective(), FirstStageOrder, Catalog()).Value;
        var second = PlanGenerator.Generate(BuildProfile(trainingDays), BuildObjective(), FirstStageOrder, Catalog()).Value;

        Assert.Equal(Snapshot(first), Snapshot(second));
    }

    [Theory]
    [InlineData(5)]
    public void Generate_fails_for_a_frequency_without_a_declared_weekly_split(int trainingDays)
    {
        var result = PlanGenerator.Generate(BuildProfile(trainingDays), BuildObjective(), FirstStageOrder, Catalog());

        Assert.True(result.IsFailure);
        Assert.Equal(DomainErrors.Plan.UnsupportedFrequency, result.Error);
        Assert.Contains("solo se puede generar un plan de 3 o 4 días", result.Error.Description);
    }

    private static string Snapshot(Plan plan) =>
        string.Join(
            "|",
            plan.Microcycles.SelectMany(microcycle =>
                microcycle.Sessions.SelectMany(session =>
                    session.Items.Select(item =>
                        $"{microcycle.Number}:{session.Day}:{item.ExerciseId}:{item.Role}:{item.Pattern}:"
                        + $"{item.Sets}:{item.RepsMin}-{item.RepsMax}:{item.HoldSecondsMin}-{item.HoldSecondsMax}:{item.Note}"))));

    private static SessionItem StrengthItem(Plan plan, string exerciseId) =>
        StrengthItem(plan, 0, exerciseId);

    private static SessionItem StrengthItem(Plan plan, int microcycleIndex, string exerciseId) =>
        plan.Microcycles[microcycleIndex].Sessions[0].Items.Single(item => item.ExerciseId == exerciseId);

    private static SessionItem StrengthItem(Plan plan, int microcycleIndex, int day, string exerciseId) =>
        plan.Microcycles[microcycleIndex].Sessions.Single(session => session.Day == day)
            .Items.Single(item => item.ExerciseId == exerciseId);

    private static IEnumerable<SessionItem> StrengthItems(Plan plan) =>
        plan.Microcycles[0].Sessions[0].Items.Where(item => item.Role == SessionItemRole.Strength);

    private static IEnumerable<SessionItem> AllStrengthItems(Plan plan) =>
        plan.Microcycles
            .SelectMany(microcycle => microcycle.Sessions)
            .SelectMany(session => session.Items)
            .Where(item => item.Role == SessionItemRole.Strength);

    /// <summary>Volumen de fuerza (series × reps) de las primeras <paramref name="microcycleCount"/> semanas.</summary>
    private static int[] VolumeByMicrocycle(Plan plan, int microcycleCount) =>
        plan.Microcycles
            .Take(microcycleCount)
            .Select(microcycle => microcycle.Sessions
                .SelectMany(session => session.Items)
                .Where(item => item.Role == SessionItemRole.Strength)
                .Sum(item => item.Sets * item.RepsMax!.Value))
            .ToArray();

    private static AthleteProfile BuildProfile(
        int trainingDays,
        int pushUpMaximum = 10,
        int pullUpMaximum = 5,
        int squatMaximum = 20,
        double weightKilograms = 78,
        double heightCentimeters = 180,
        double armSpanCentimeters = 180,
        double inseamCentimeters = 85) =>
        AthleteProfile.Create(
            UserId,
            weightKilograms,
            heightCentimeters,
            armSpanCentimeters,
            inseamCentimeters,
            trainingDays,
            [
                new MaximumInput("push_up", pushUpMaximum),
                new MaximumInput("pull_up", pullUpMaximum),
                new MaximumInput("squat", squatMaximum),
            ]).Value;

    private static Objective BuildObjective(string skillId = SkillId) =>
        Objective.Create(UserId, skillId, Catalog()).Value;

    private static AthleteProfile FavorableProfile() =>
        BuildProfile(3, weightKilograms: 55, heightCentimeters: 165, armSpanCentimeters: 165, inseamCentimeters: 82);

    private static AthleteProfile UnfavorableProfile() =>
        BuildProfile(3, weightKilograms: 100, heightCentimeters: 185, armSpanCentimeters: 190, inseamCentimeters: 80);

    private static KnowledgeBase Catalog(bool lever = true)
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
            SkillMovement("pistol-box", PistolSkillId, Metric.Reps),
            SkillMovement("pistol-assisted", PistolSkillId, Metric.Reps),
            SkillMovement("pistol-negative", PistolSkillId, Metric.Reps),
            SkillMovement("pistol-full", PistolSkillId, Metric.Reps),
        };

        var planche = new Skill
        {
            Id = SkillId,
            Name = "Planche",
            Group = ExerciseGroup.Push,
            Lever = lever,
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

        var pistol = new Skill
        {
            Id = PistolSkillId,
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

    private static Exercise SkillMovement(string id, string skillId = SkillId, Metric metric = Metric.Seconds) =>
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
