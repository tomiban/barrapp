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
