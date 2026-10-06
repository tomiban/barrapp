using Barrapp.Application.Abstractions;

namespace Barrapp.Application.Features.SessionLogs;

/// <summary>
/// Una serie registrada tal y como se sirve al cliente: el valor real ejecutado con la unidad del
/// ejercicio, el RIR real y el lastre opcionales (spec 0001, US-34, US-35; ver <c>GLOSSARY.md</c>).
/// </summary>
/// <param name="SetNumber">Número de orden de la serie, desde 1.</param>
/// <param name="Value">Valor real ejecutado: repeticiones o segundos según la unidad del ítem.</param>
/// <param name="Metric">Unidad del valor: <c>reps</c> o <c>seconds</c>; la misma en todas las series del ítem.</param>
/// <param name="ActualRir">RIR real de la serie, entre 0 y 10; <c>null</c> si no se anotó.</param>
/// <param name="LoadKg">Lastre en kg de la serie; <c>null</c> si se hizo a peso corporal.</param>
public sealed record SessionLogSetResponse(
    int SetNumber,
    int Value,
    string Metric,
    int? ActualRir,
    double? LoadKg);

/// <summary>
/// Objetivo prescrito de un ítem, congelado al registrar (ADR-0014): lo que el plan pedía para ese
/// ejercicio. El Historial lo muestra junto a lo ejecutado sin volver a leer el plan.
/// </summary>
/// <param name="Sets">Número de series prescritas.</param>
/// <param name="RepsMin">Repeticiones mínimas; <c>null</c> si el ejercicio se mide en segundos.</param>
/// <param name="RepsMax">Repeticiones máximas; <c>null</c> si el ejercicio se mide en segundos.</param>
/// <param name="HoldSecondsMin">Segundos mínimos; <c>null</c> si el ejercicio se mide en repeticiones.</param>
/// <param name="HoldSecondsMax">Segundos máximos; <c>null</c> si el ejercicio se mide en repeticiones.</param>
public sealed record SessionLogObjectiveResponse(
    int Sets,
    int? RepsMin,
    int? RepsMax,
    int? HoldSecondsMin,
    int? HoldSecondsMax);

/// <summary>
/// Un ítem registrado de una sesión: la foto del ejercicio (nombre, papel, patrón y unidad) con su
/// objetivo y las series realmente ejecutadas.
/// </summary>
/// <param name="Id">Identificador del ítem, con el que se edita y se borra.</param>
/// <param name="SessionLogId">Sesión a la que pertenece.</param>
/// <param name="Position">Orden del ítem dentro de la sesión, desde 1.</param>
/// <param name="ExerciseId">Slug del ejercicio.</param>
/// <param name="ExerciseName">Nombre del ejercicio tal y como se registró.</param>
/// <param name="Role">Papel en la sesión: <c>skill</c>, <c>strength</c> o <c>core</c>.</param>
/// <param name="Pattern">Patrón de fuerza o <c>null</c>.</param>
/// <param name="Metric">Unidad del valor: <c>reps</c> o <c>seconds</c>.</param>
/// <param name="Objective">Lo que prescribía el plan para ese ejercicio.</param>
/// <param name="Note">Nota de la fila para la UI; <c>null</c> cuando no hay.</param>
/// <param name="Sets">Series ejecutadas, en orden.</param>
/// <param name="Pending">
/// Solo lo marca el cliente: <c>true</c> mientras el ítem sigue en la cola local (#26). Los
/// registros que llegan del API no la traen.
/// </param>
public sealed record SessionLogItemResponse(
    Guid Id,
    Guid SessionLogId,
    int Position,
    string ExerciseId,
    string ExerciseName,
    string Role,
    string? Pattern,
    string Metric,
    SessionLogObjectiveResponse Objective,
    string? Note,
    IReadOnlyList<SessionLogSetResponse> Sets,
    bool? Pending = null);

/// <summary>
/// Registro de sesión tal y como se sirve al cliente: la cabecera con su clave de sesión
/// determinista (ADR-0014), su marca de completada y los ítems registrados con su foto.
/// </summary>
/// <param name="Id">Identificador del registro de sesión.</param>
/// <param name="Kind">Origen de la sesión: <c>mesocycle</c> o <c>suelta</c>.</param>
/// <param name="SessionDate">Fecha de la sesión (ISO 8601, <c>AAAA-MM-DD</c>).</param>
/// <param name="MesocycleId">Mesociclo de la sesión; <c>null</c> en una sesión suelta.</param>
/// <param name="MicrocycleNumber">Microciclo (1–4) de la sesión; <c>null</c> en una sesión suelta.</param>
/// <param name="SessionDay">Día de la sesión en el microciclo; <c>null</c> en una sesión suelta.</param>
/// <param name="RecordedAtUtc">Momento en que se registró la sesión (UTC); lo fija el servidor.</param>
/// <param name="CompletedAtUtc">
/// Momento en que se dio por completada (UTC); <c>null</c> mientras sigue pendiente (US23).
/// </param>
/// <param name="Items">Ítems registrados, en orden de ejecución.</param>
public sealed record SessionLogResponse(
    Guid Id,
    string Kind,
    DateOnly SessionDate,
    Guid? MesocycleId,
    int? MicrocycleNumber,
    int? SessionDay,
    DateTimeOffset RecordedAtUtc,
    DateTimeOffset? CompletedAtUtc,
    IReadOnlyList<SessionLogItemResponse> Items)
{
    /// <summary>Si la sesión está marcada como completada (spec 0001, US-23).</summary>
    public bool Completed => CompletedAtUtc is not null;
}
