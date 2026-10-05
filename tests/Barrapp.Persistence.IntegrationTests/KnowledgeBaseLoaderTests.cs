using Barrapp.Domain.Knowledge;
using Barrapp.Persistence.Knowledge;
using Microsoft.Extensions.Logging.Abstractions;

namespace Barrapp.Persistence.IntegrationTests;

/// <summary>
/// Comportamiento del cargador: lee los JSON, delega la validación en el dominio y falla rápido con
/// <see cref="KnowledgeBaseValidationException"/> ante datos inválidos. Las reglas concretas se
/// cubren en <c>Barrapp.Domain.UnitTests.KnowledgeBaseTests</c>; aquí se prueba el camino de
/// deserialización (contrato JSON), el recurso embebido y el fallo claro.
/// </summary>
public sealed class KnowledgeBaseLoaderTests
{
    private const string ExercisesJson = """
        {
          "schemaVersion": 1,
          "exercises": [
            { "id": "push-up", "name": "Flexiones", "kind": "conditioning", "group": "push", "metric": "reps", "tracksMaximum": false, "regressionId": null, "skillId": null },
            { "id": "planche-lean", "name": "Planche inclinada", "kind": "conditioning", "group": "push", "metric": "seconds", "tracksMaximum": false, "regressionId": null, "skillId": "planche" }
          ]
        }
        """;

    private const string SkillsJson = """
        {
          "schemaVersion": 1,
          "skills": [
            {
              "id": "planche",
              "name": "Planche",
              "group": "push",
              "lever": true,
              "stages": [
                { "order": 1, "name": "Etapa 1", "exerciseId": "planche-lean", "criterion": { "metric": "seconds", "target": 10, "sets": 3 }, "notes": "" },
                { "order": 2, "name": "Etapa 2", "exerciseId": "planche-lean", "criterion": { "metric": "seconds", "target": 10, "sets": 3 }, "notes": "" },
                { "order": 3, "name": "Etapa 3", "exerciseId": "planche-lean", "criterion": { "metric": "seconds", "target": 10, "sets": 3 }, "notes": "" },
                { "order": 4, "name": "Etapa 4", "exerciseId": "planche-lean", "criterion": { "metric": "seconds", "target": 10, "sets": 3 }, "notes": "" }
              ],
              "patternRoutines": [
                {
                  "id": "r1",
                  "name": "Modelo R1",
                  "intensity": 2,
                  "equipment": "Sin equipamiento",
                  "items": [
                    { "exerciseId": "push-up", "sets": 3, "repsMin": 5, "repsMax": 8, "restSeconds": 60 }
                  ]
                }
              ]
            }
          ]
        }
        """;

    private const string RoutinesJson = """
        {
          "schemaVersion": 1,
          "programs": [
            {
              "id": "ponte-en-forma",
              "name": "Ponte en forma",
              "type": "circuit",
              "description": null,
              "routines": [
                {
                  "id": "ponte-en-forma-r1",
                  "name": "Rutina 1",
                  "intensity": 2,
                  "durationMinutes": 10,
                  "blocks": [
                    {
                      "name": "SET 1",
                      "rounds": 3,
                      "restSeconds": 0,
                      "notes": null,
                      "items": [
                        { "exerciseId": "push-up", "sets": 1, "holdSecondsMin": 15, "holdSecondsMax": 15, "restSeconds": 0 }
                      ]
                    }
                  ]
                }
              ]
            }
          ]
        }
        """;

    private const string SupersetRoutinesJson = """
        {
          "schemaVersion": 1,
          "programs": [
            {
              "id": "p",
              "name": "Programa",
              "type": "strength",
              "routines": [
                {
                  "id": "r",
                  "name": "Rutina",
                  "intensity": 2,
                  "blocks": [
                    {
                      "name": "B",
                      "rounds": 1,
                      "restSeconds": 0,
                      "items": [
                        { "exerciseId": "push-up", "sets": 1, "repsMin": 5, "repsMax": 5, "restSeconds": 0, "supersetGroup": 1 },
                        { "exerciseId": "push-up", "sets": 1, "repsMin": 5, "repsMax": 5, "restSeconds": 0, "supersetGroup": 2 },
                        { "exerciseId": "push-up", "sets": 1, "repsMin": 5, "repsMax": 5, "restSeconds": 0, "supersetGroup": 1 }
                      ]
                    }
                  ]
                }
              ]
            }
          ]
        }
        """;

    [Fact]
    public void Load_reads_a_valid_catalog_from_the_json_contract()
    {
        var catalog = CreateLoader().Load(ExercisesJson, SkillsJson, RoutinesJson);

        Assert.Equal(2, catalog.Exercises.Count);
        Assert.Equal("Flexiones", catalog.FindExercise("push-up")!.Name);
        Assert.Single(catalog.Skills);
        Assert.True(catalog.FindSkill("planche")!.Lever);
        Assert.Single(catalog.Programs);
        Assert.Equal(RoutineProgramType.Circuit, catalog.Programs[0].Type);
    }

    [Fact]
    public void Load_from_embedded_resources_returns_the_validated_real_catalog()
    {
        var catalog = CreateLoader().LoadEmbeddedResources();

        Assert.NotEmpty(catalog.Exercises);
        Assert.NotNull(catalog.Skills);
        Assert.NotNull(catalog.Programs);

        // El catálogo real cubre los cinco patrones y ningún grupo queda vacío.
        foreach (var group in Enum.GetValues<ExerciseGroup>())
        {
            Assert.NotEmpty(catalog.ExercisesByGroup(group));
        }

        // Cada regresión declarada resuelve dentro del propio catálogo (lo garantiza el cargador).
        Assert.All(
            catalog.Exercises.Where(exercise => exercise.RegressionId is not null),
            exercise => Assert.NotNull(catalog.FindExercise(exercise.RegressionId!)));

        // Los básicos declaran que se registra su máximo.
        Assert.Contains(catalog.Exercises, exercise => exercise.TracksMaximum);
    }

    [Fact]
    public void Load_from_embedded_resources_validates_every_skill_ladder_and_pattern_routine()
    {
        var catalog = CreateLoader().LoadEmbeddedResources();

        // Los cuatro skills de la M2, cada uno con su patrón y su flag de palanca.
        Assert.Equal(
            ["handstand", "front-lever", "planche", "pistol-squat"],
            catalog.Skills.Select(skill => skill.Id));

        foreach (var skill in catalog.Skills)
        {
            Assert.NotEqual(ExerciseGroup.Cardio, skill.Group);
            Assert.Equal(5, skill.Stages.Count);
            Assert.Equal(
                Enumerable.Range(1, skill.Stages.Count),
                skill.Stages.Select(stage => stage.Order));
            Assert.All(skill.Stages, stage =>
            {
                Assert.True(stage.Criterion.Target > 0);
                Assert.True(stage.Criterion.Sets > 0);
                Assert.False(string.IsNullOrWhiteSpace(stage.Name));
                Assert.NotNull(catalog.FindExercise(stage.ExerciseId));
            });

            Assert.NotEmpty(skill.PatternRoutines);
            Assert.All(skill.PatternRoutines, routine =>
            {
                Assert.False(string.IsNullOrWhiteSpace(routine.Id));
                Assert.False(string.IsNullOrWhiteSpace(routine.Name));
                Assert.NotEmpty(routine.Items);
                Assert.All(
                    routine.Items,
                    item => Assert.NotNull(catalog.FindExercise(item.ExerciseId)));
            });
        }

        // Planche y front lever traen varios modelos (R1–R5); pino y pistol, uno.
        Assert.Equal(5, catalog.FindSkill("planche")!.PatternRoutines.Count);
        Assert.Equal(5, catalog.FindSkill("front-lever")!.PatternRoutines.Count);
        Assert.Single(catalog.FindSkill("handstand")!.PatternRoutines);
        Assert.Single(catalog.FindSkill("pistol-squat")!.PatternRoutines);

        // Los apalancados lo declaran; pino (equilibrio) y pistol (pierna) no.
        Assert.True(catalog.FindSkill("planche")!.Lever);
        Assert.True(catalog.FindSkill("front-lever")!.Lever);
        Assert.False(catalog.FindSkill("handstand")!.Lever);
        Assert.False(catalog.FindSkill("pistol-squat")!.Lever);

        // Los holds se miden en segundos y el pistol squat en repeticiones.
        foreach (var skillId in new[] { "handstand", "front-lever", "planche" })
        {
            Assert.All(
                catalog.FindSkill(skillId)!.Stages,
                stage => Assert.Equal(Metric.Seconds, stage.Criterion.Metric));
        }

        Assert.All(
            catalog.FindSkill("pistol-squat")!.Stages,
            stage => Assert.Equal(Metric.Reps, stage.Criterion.Metric));

        // Los 20 movimientos de skill resuelven a su skill y no declaran patrón propio.
        var skillMovements = catalog.Exercises
            .Where(exercise => exercise.Kind == ExerciseKind.Skill)
            .ToList();
        Assert.Equal(20, skillMovements.Count);
        Assert.All(skillMovements, movement =>
        {
            Assert.Null(movement.Group);
            Assert.NotNull(catalog.FindSkill(movement.SkillId!));
        });
    }

    [Fact]
    public void Load_from_embedded_resources_validates_every_program_routine_and_block()
    {
        var catalog = CreateLoader().LoadEmbeddedResources();

        // Los tres programas generales de la M2, con su tipo y su número de rutinas.
        Assert.Equal(
            ["ponte-en-forma", "base-perfecta", "home-workout"],
            catalog.Programs.Select(program => program.Id));

        var ponteEnForma = catalog.Programs.Single(program => program.Id == "ponte-en-forma");
        Assert.Equal("Ponte en forma", ponteEnForma.Name);
        Assert.Equal(RoutineProgramType.Circuit, ponteEnForma.Type);
        Assert.Equal(
            new int?[] { 14, 8, 10, 15, 15 },
            ponteEnForma.Routines.Select(routine => routine.DurationMinutes));

        var basePerfecta = catalog.Programs.Single(program => program.Id == "base-perfecta");
        Assert.Equal(RoutineProgramType.Strength, basePerfecta.Type);
        Assert.Equal(8, basePerfecta.Routines.Count);

        var homeWorkout = catalog.Programs.Single(program => program.Id == "home-workout");
        Assert.Equal(RoutineProgramType.Strength, homeWorkout.Type);
        Assert.Equal(5, homeWorkout.Routines.Count);

        // Cada rutina describe bloques completos cuyas filas resuelven a ejercicios del catálogo.
        foreach (var program in catalog.Programs)
        {
            Assert.NotEmpty(program.Routines);
            Assert.Equal(
                program.Routines.Count,
                program.Routines.Select(routine => routine.Id).Distinct(StringComparer.Ordinal).Count());

            foreach (var routine in program.Routines)
            {
                Assert.False(string.IsNullOrWhiteSpace(routine.Name));
                Assert.InRange(routine.Intensity, 2, 3);
                Assert.NotEmpty(routine.Blocks);

                foreach (var block in routine.Blocks)
                {
                    Assert.False(string.IsNullOrWhiteSpace(block.Name));
                    Assert.True(block.Rounds >= 1);
                    Assert.True(block.RestSeconds >= 0);
                    Assert.NotEmpty(block.Items);

                    foreach (var item in block.Items)
                    {
                        Assert.NotNull(catalog.FindExercise(item.ExerciseId));
                        Assert.True(item.Sets > 0);
                        Assert.True(item.RestSeconds >= 0);

                        var hasRepsRange = item.RepsMin is not null && item.RepsMax is not null;
                        var hasHoldRange = item.HoldSecondsMin is not null && item.HoldSecondsMax is not null;
                        Assert.True(hasRepsRange || hasHoldRange);
                    }
                }
            }
        }

        // Los programas generales apoyan los skills: referencian movimientos kind: skill.
        var referencedExerciseIds = catalog.Programs
            .SelectMany(program => program.Routines)
            .SelectMany(routine => routine.Blocks)
            .SelectMany(block => block.Items)
            .Select(item => item.ExerciseId)
            .ToHashSet(StringComparer.Ordinal);
        Assert.Contains("planche-tuck", referencedExerciseIds);
        Assert.Contains("front-lever-full", referencedExerciseIds);
        Assert.All(
            referencedExerciseIds,
            exerciseId => Assert.NotNull(catalog.FindExercise(exerciseId)));
    }

    [Fact]
    public void Load_rejects_non_consecutive_superset_groups()
    {
        var exception = Assert.Throws<KnowledgeBaseValidationException>(
            () => CreateLoader().Load(ExercisesJson, SkillsJson, SupersetRoutinesJson));

        Assert.Equal("knowledge.superset_group_not_consecutive", exception.Code);
    }

    [Fact]
    public void Load_rejects_malformed_json()
    {
        var exception = Assert.Throws<KnowledgeBaseValidationException>(
            () => CreateLoader().Load("{ not json", SkillsJson, RoutinesJson));

        Assert.Equal("knowledge.invalid_json", exception.Code);
    }

    public static TheoryData<string, string, string, string> InvalidCatalogs()
    {
        var data = new TheoryData<string, string, string, string>
        {
            {
                ExercisesJson.Replace("\"id\": \"planche-lean\"", "\"id\": \"push-up\"", StringComparison.Ordinal),
                SkillsJson,
                RoutinesJson,
                "knowledge.duplicate_exercise_id"
            },
            {
                ExercisesJson,
                SkillsJson.Replace("\"exerciseId\": \"planche-lean\"", "\"exerciseId\": \"ghost\"", StringComparison.Ordinal),
                RoutinesJson,
                "knowledge.unknown_exercise_reference"
            },
            {
                ExercisesJson,
                SkillsJson.Replace("\"order\": 4", "\"order\": 6", StringComparison.Ordinal),
                RoutinesJson,
                "knowledge.stage_order_not_consecutive"
            },
            {
                ExercisesJson,
                SkillsJson.Replace("\"target\": 10", "\"target\": 0", StringComparison.Ordinal),
                RoutinesJson,
                "knowledge.criterion_target_must_be_positive"
            },
            {
                ExercisesJson,
                SkillsJson.Replace("\"repsMin\": 5, \"repsMax\": 8", "\"repsMin\": null, \"repsMax\": null", StringComparison.Ordinal),
                RoutinesJson,
                "knowledge.routine_item_requires_range"
            },
            {
                ExercisesJson,
                SkillsJson.Replace("\"repsMin\": 5, \"repsMax\": 8", "\"repsMin\": 8, \"repsMax\": 5", StringComparison.Ordinal),
                RoutinesJson,
                "knowledge.routine_range_min_greater_than_max"
            },
            {
                ExercisesJson.Replace("\"schemaVersion\": 1", "\"schemaVersion\": 99", StringComparison.Ordinal),
                SkillsJson,
                RoutinesJson,
                "knowledge.unsupported_schema_version"
            },
        };

        return data;
    }

    [Theory]
    [MemberData(nameof(InvalidCatalogs))]
    public void Load_rejects_an_invalid_catalog(string exercisesJson, string skillsJson, string routinesJson, string expectedCode)
    {
        var exception = Assert.Throws<KnowledgeBaseValidationException>(
            () => CreateLoader().Load(exercisesJson, skillsJson, routinesJson));

        Assert.Equal(expectedCode, exception.Code);
    }

    private static KnowledgeBaseLoader CreateLoader() => new(NullLogger<KnowledgeBaseLoader>.Instance);
}
