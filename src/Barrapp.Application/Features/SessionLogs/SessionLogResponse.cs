namespace Barrapp.Application.Features.SessionLogs;

/// <summary>Una serie de un registro de sesión, tal y como se sirve al cliente.</summary>
/// <param name="SetNumber">Número de orden de la serie, desde 1.</param>
/// <param name="Value">Valor real ejecutado: reps o segundos según el ejercicio.</param>
/// <param name="Effort">Esfuerzo real (RIR/RPE), entre 0 y 10; <c>null</c> si no se anotó.</param>
public sealed record SessionLogSetResponse(int SetNumber, int Value, int? Effort);

/// <summary>
/// Registro de sesión tal y como se sirve al cliente. La unidad (<paramref name="Metric"/>,
/// <c>reps</c> o <c>seconds</c>) la deriva el servidor del tipo de ejercicio del catálogo, nunca
/// el cliente.
/// </summary>
/// <param name="Id">Identificador del registro.</param>
/// <param name="ExerciseId">Identificador del ejercicio registrado.</param>
/// <param name="ExerciseName">Nombre del ejercicio para la UI.</param>
/// <param name="Metric">Unidad del valor: <c>reps</c> o <c>seconds</c>.</param>
/// <param name="MesocycleId">Mesociclo de la sesión, cuando está persistido (ticket #27).</param>
/// <param name="SessionDay">Día de la sesión dentro del mesociclo.</param>
/// <param name="RecordedAtUtc">Momento del registro (UTC).</param>
/// <param name="Sets">Series del ejercicio con el valor real de cada una.</param>
public sealed record SessionLogResponse(
    Guid Id,
    string ExerciseId,
    string ExerciseName,
    string Metric,
    Guid? MesocycleId,
    int SessionDay,
    DateTimeOffset RecordedAtUtc,
    IReadOnlyList<SessionLogSetResponse> Sets);
