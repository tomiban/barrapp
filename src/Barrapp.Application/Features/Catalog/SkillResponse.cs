namespace Barrapp.Application.Features.Catalog;

/// <summary>Criterio para superar una etapa, con la métrica en minúsculas (<c>reps</c> o <c>seconds</c>).</summary>
/// <param name="Metric">Unidad del criterio en minúsculas.</param>
/// <param name="Target">Objetivo a alcanzar.</param>
/// <param name="Sets">Series en las que hay que lograrlo.</param>
public sealed record StageCriterionResponse(string Metric, int Target, int Sets);

/// <summary>Etapa de la escalera de progresión de un skill.</summary>
/// <param name="Order">Posición en la escalera, consecutiva desde 1.</param>
/// <param name="Name">Nombre de la etapa para la UI, en español.</param>
/// <param name="ExerciseId">Ejercicio que representa la etapa.</param>
/// <param name="Criterion">Criterio para superarla.</param>
/// <param name="Notes">Notas para la UI, en español.</param>
public sealed record SkillStageResponse(
    int Order,
    string Name,
    string ExerciseId,
    StageCriterionResponse Criterion,
    string Notes);

/// <summary>Fila de una rutina de patrón: un ejercicio con sus series, rango y descanso.</summary>
/// <param name="ExerciseId">Ejercicio de la fila.</param>
/// <param name="Sets">Número de series.</param>
/// <param name="RepsMin">Repeticiones mínimas del rango; opcional.</param>
/// <param name="RepsMax">Repeticiones máximas del rango; opcional.</param>
/// <param name="HoldSecondsMin">Segundos mantenidos mínimos del rango; opcional.</param>
/// <param name="HoldSecondsMax">Segundos mantenidos máximos del rango; opcional.</param>
/// <param name="RestSeconds">Descanso tras la serie (o el grupo de superserie), en segundos.</param>
/// <param name="Tempo">Cadencia opcional (<c>positiva:isométrica:excéntrica</c>).</param>
/// <param name="SupersetGroup">Entero que agrupa filas consecutivas en una superserie; opcional.</param>
/// <param name="Notes">Notas para la UI, en español; opcional.</param>
public sealed record RoutineItemResponse(
    string ExerciseId,
    int Sets,
    int? RepsMin,
    int? RepsMax,
    int? HoldSecondsMin,
    int? HoldSecondsMax,
    int RestSeconds,
    string? Tempo,
    int? SupersetGroup,
    string? Notes);

/// <summary>Rutina de patrón de un skill: un modelo por intensidad y material con sus filas.</summary>
/// <param name="Id">Identificador único dentro del skill.</param>
/// <param name="Name">Nombre del modelo para la UI, en español.</param>
/// <param name="Intensity">Intensidad del modelo (2 o 3).</param>
/// <param name="Equipment">Material necesario; opcional.</param>
/// <param name="Items">Filas de la rutina.</param>
public sealed record PatternRoutineResponse(
    string Id,
    string Name,
    int Intensity,
    string? Equipment,
    IReadOnlyList<RoutineItemResponse> Items);

/// <summary>
/// Skill del catálogo con su escalera de progresión y sus rutinas de patrón, tal y como lo
/// consume la app. El patrón va en minúsculas (<c>push</c>, <c>pull</c>, <c>leg</c>, <c>core</c>).
/// </summary>
/// <param name="Id">Slug estable del skill.</param>
/// <param name="Name">Nombre para la UI, en español.</param>
/// <param name="Group">Patrón que entrena, en minúsculas.</param>
/// <param name="Lever">Indica si es un skill apalancado.</param>
/// <param name="Stages">Escalera de progresión, ordenada por <c>order</c>.</param>
/// <param name="PatternRoutines">Rutinas de patrón del skill.</param>
public sealed record SkillResponse(
    string Id,
    string Name,
    string Group,
    bool Lever,
    IReadOnlyList<SkillStageResponse> Stages,
    IReadOnlyList<PatternRoutineResponse> PatternRoutines);
