using Barrapp.Domain.Knowledge;
using Barrapp.Domain.Planning;

namespace Barrapp.Domain.Sessions;

/// <summary>
/// Entrada de una fila para crear el snapshot de una <see cref="SessionSuelta"/>: el ejercicio, su
/// papel, su patrón (solo en fuerza), sus series y su prescripción —un rango de repeticiones o de
/// segundos mantenidos—.
/// </summary>
/// <param name="ExerciseId">Slug del ejercicio en el catálogo.</param>
/// <param name="Role">Papel del ejercicio dentro de la sesión.</param>
/// <param name="Pattern">Patrón de fuerza; <c>null</c> en el bloque de skill y en el core.</param>
/// <param name="Sets">Número de series.</param>
/// <param name="RepsMin">Repeticiones mínimas del rango.</param>
/// <param name="RepsMax">Repeticiones máximas del rango.</param>
/// <param name="HoldSecondsMin">Segundos mantenidos mínimos del rango.</param>
/// <param name="HoldSecondsMax">Segundos mantenidos máximos del rango.</param>
/// <param name="Note">Nota para la UI; <c>null</c> si no hay nada que explicar.</param>
public sealed record SessionSueltaItemInput(
    string ExerciseId,
    SessionItemRole Role,
    ExerciseGroup? Pattern,
    int Sets,
    int? RepsMin,
    int? RepsMax,
    int? HoldSecondsMin,
    int? HoldSecondsMax,
    string? Note = null);