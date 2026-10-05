namespace Barrapp.Application.Features.Catalog;

/// <summary>Ejercicio del catálogo tal y como lo consume la app.</summary>
/// <param name="Id">Slug estable del ejercicio.</param>
/// <param name="Name">Nombre para la UI, en español.</param>
/// <param name="Metric">Unidad del ejercicio en minúsculas: <c>reps</c> o <c>seconds</c>.</param>
/// <param name="TracksMaximum">Indica si el atleta registra su máximo en este ejercicio.</param>
/// <param name="RegressionId">Ejercicio de regresión cuando el máximo es 0; opcional.</param>
/// <param name="SkillId">Skill al que apoya el ejercicio; opcional.</param>
public sealed record ExerciseResponse(
    string Id,
    string Name,
    string Metric,
    bool TracksMaximum,
    string? RegressionId,
    string? SkillId);

/// <summary>Ejercicios de un patrón, con el patrón en minúsculas (p. ej. <c>push</c>).</summary>
/// <param name="Group">Patrón del grupo en minúsculas.</param>
/// <param name="Exercises">Ejercicios del grupo, en el orden del catálogo.</param>
public sealed record ExerciseGroupResponse(
    string Group,
    IReadOnlyList<ExerciseResponse> Exercises);

/// <summary>
/// Catálogo de ejercicios agrupado por patrón (push, pull, leg, core, cardio), solo con los
/// grupos que tienen ejercicios.
/// </summary>
/// <param name="Groups">Grupos con ejercicios, en el orden canónico de los patrones.</param>
public sealed record ExerciseCatalogResponse(IReadOnlyList<ExerciseGroupResponse> Groups);
