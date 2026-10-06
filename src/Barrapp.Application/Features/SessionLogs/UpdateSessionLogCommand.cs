using Barrapp.Application.Abstractions;
using Barrapp.Domain.Sessions;

namespace Barrapp.Application.Features.SessionLogs;

/// <summary>
/// Edita un registro de sesión ya guardado (spec 0001, US-36; decisión D5): sustituye los valores
/// de sus series, con el esfuerzo (RIR/RPE) opcional por serie. La identidad de la sesión —el
/// ejercicio, el día y el mesociclo— no cambia; la unidad del valor la sigue derivando el servidor
/// del tipo de ejercicio.
/// </summary>
/// <param name="Id">Identificador del registro a editar.</param>
/// <param name="Sets">Nuevas series, numeradas desde 1 y en orden.</param>
public sealed record UpdateSessionLogCommand(
    Guid Id,
    IReadOnlyList<SessionLogSetInput> Sets) : ICommand<SessionLogResponse>;