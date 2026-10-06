using Barrapp.Application.Abstractions;
using Barrapp.Domain.Sessions;

namespace Barrapp.Application.Features.SessionLogs;

/// <summary>
/// Registra lo ejecutado, serie a serie, en un ejercicio de una sesión del plan (spec 0001,
/// US-34). El valor real de cada serie —reps en fuerza o segundos en holds— se guarda tal cual;
/// la unidad la deriva el servidor del tipo de ejercicio. Con <paramref name="ClientId"/> el alta
/// es idempotente (ticket #26, outbox offline): si ya existe un registro con ese id para el
/// atleta, el servidor actualiza sus series en su sitio en lugar de duplicar la fila.
/// </summary>
/// <param name="ExerciseId">Identificador del ejercicio registrado en el catálogo.</param>
/// <param name="MesocycleId">Mesociclo de la sesión, si el plan ya está persistido (ticket #27).</param>
/// <param name="SessionDay">Día de la sesión dentro del mesociclo, desde 1.</param>
/// <param name="Sets">Series ejecutadas, numeradas desde 1 y en orden.</param>
/// <param name="ClientId">Id idempotente del cliente (de la outbox offline); opcional.</param>
public sealed record RegisterSessionLogCommand(
    string ExerciseId,
    Guid? MesocycleId,
    int SessionDay,
    IReadOnlyList<SessionLogSetInput> Sets,
    Guid? ClientId = null) : ICommand<SessionLogResponse>;
