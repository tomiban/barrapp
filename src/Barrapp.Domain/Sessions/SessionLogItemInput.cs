using Barrapp.Domain.Knowledge;
using Barrapp.Domain.Planning;

namespace Barrapp.Domain.Sessions;

/// <summary>
/// Entrada de un ítem de sesión para registrar dentro de un <see cref="SessionLog"/>: la
/// <b>foto por ítem</b> de ADR-0014 (ejercicio, papel y objetivo de series/reps/segundos) más las
/// series realmente ejecutadas.
/// </summary>
/// <param name="ExerciseId">Slug del ejercicio en el catálogo.</param>
/// <param name="ExerciseName">Nombre del ejercicio tal y como se mostró al registrar (foto).</param>
/// <param name="Role">Papel del ejercicio en la sesión (foto).</param>
/// <param name="Pattern">Patrón de fuerza de la fila (foto); <c>null</c> en skill y core.</param>
/// <param name="Metric">
/// Unidad con la que se mide el ejercicio (foto): la deriva el servidor del tipo de ejercicio, de
/// modo que el historial sigue siendo legible aunque la base de conocimiento cambie.
/// </param>
/// <param name="PrescribedSets">Número de series prescritas en el objetivo (foto).</param>
/// <param name="RepsMin">Repeticiones mínimas prescritas; <c>null</c> si el ejercicio se mide en segundos.</param>
/// <param name="RepsMax">Repeticiones máximas prescritas; <c>null</c> si el ejercicio se mide en segundos.</param>
/// <param name="HoldSecondsMin">Segundos mínimos prescritos; <c>null</c> si se mide en repeticiones.</param>
/// <param name="HoldSecondsMax">Segundos máximos prescritos; <c>null</c> si se mide en repeticiones.</param>
/// <param name="Note">Nota de la fila para la UI; <c>null</c> cuando no hay nada que explicar.</param>
/// <param name="Sets">Series ejecutadas, numeradas desde 1 y en orden.</param>
/// <param name="ClientId">
/// Id idempotente del cliente (la outbox offline, ADR-0003) para reintentar el alta sin duplicar el
/// ítem; <c>null</c> si el alta vino del API sin idempotencia.
/// </param>
public sealed record SessionLogItemInput(
    string ExerciseId,
    string ExerciseName,
    SessionItemRole Role,
    ExerciseGroup? Pattern,
    Metric Metric,
    int PrescribedSets,
    int? RepsMin,
    int? RepsMax,
    int? HoldSecondsMin,
    int? HoldSecondsMax,
    string? Note,
    IReadOnlyCollection<SessionLogSetInput> Sets,
    Guid? ClientId = null);
