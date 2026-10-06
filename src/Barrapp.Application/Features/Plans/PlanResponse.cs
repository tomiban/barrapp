namespace Barrapp.Application.Features.Plans;

/// <summary>Fila de una sesión tal y como la consume la app.</summary>
/// <param name="ExerciseId">Slug del ejercicio en el catálogo.</param>
/// <param name="ExerciseName">Nombre del ejercicio para la UI, en español.</param>
/// <param name="Role">Papel en la sesión: <c>skill</c>, <c>strength</c> o <c>core</c>.</param>
/// <param name="Pattern">Patrón de fuerza (<c>push</c>, <c>pull</c>, <c>leg</c>) o <c>null</c>.</param>
/// <param name="Sets">Número de series.</param>
/// <param name="RepsMin">Repeticiones mínimas del rango; <c>null</c> si se mide en segundos.</param>
/// <param name="RepsMax">Repeticiones máximas del rango; <c>null</c> si se mide en segundos.</param>
/// <param name="HoldSecondsMin">Segundos mínimos del rango; <c>null</c> si se mide en repeticiones.</param>
/// <param name="HoldSecondsMax">Segundos máximos del rango; <c>null</c> si se mide en repeticiones.</param>
/// <param name="Note">Nota de la fila para la UI (p. ej. el ritmo esperado por la palanca); <c>null</c> si no hay.</param>
public sealed record SessionItemResponse(
    string ExerciseId,
    string ExerciseName,
    string Role,
    string? Pattern,
    int Sets,
    int? RepsMin,
    int? RepsMax,
    int? HoldSecondsMin,
    int? HoldSecondsMax,
    string? Note);

/// <summary>Sesión del mesociclo tal y como la consume la app.</summary>
/// <param name="Day">Día dentro del microciclo (empieza en 1).</param>
/// <param name="Items">Filas de la sesión, en orden.</param>
public sealed record SessionResponse(int Day, IReadOnlyList<SessionItemResponse> Items);

/// <summary>Semana del mesociclo tal y como la consume la app.</summary>
/// <param name="Number">Número de la semana (1–4).</param>
/// <param name="Sessions">Sesiones de la semana.</param>
public sealed record MicrocycleResponse(int Number, IReadOnlyList<SessionResponse> Sessions);

/// <summary>Plan mensual tal y como lo consume la app: el mesociclo semana a semana.</summary>
/// <param name="SkillId">Slug del skill objetivo.</param>
/// <param name="TrainingDays">Días de entrenamiento por semana.</param>
/// <param name="Microcycles">Semanas del mesociclo.</param>
public sealed record PlanResponse(
    string SkillId,
    int TrainingDays,
    IReadOnlyList<MicrocycleResponse> Microcycles);
