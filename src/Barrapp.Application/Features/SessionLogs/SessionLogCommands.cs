using Barrapp.Application.Abstractions;
using MediatR;
using Barrapp.Domain.Knowledge;
using Barrapp.Domain.Planning;
using Barrapp.Domain.Sessions;

namespace Barrapp.Application.Features.SessionLogs;

/// <summary>
/// Cabecera de la sesión que se está anotando: la clave de sesión determinista de ADR-0014. Es
/// <b>valor</b>, no una referencia: el plan no se persiste ni se enlaza con claves foráneas.
/// </summary>
/// <param name="Kind">Origen de la sesión: del mesociclo en curso o suelta.</param>
/// <param name="SessionDate">Fecha de la sesión (ISO 8601, <c>AAAA-MM-DD</c>).</param>
/// <param name="MesocycleId">Mesociclo de la sesión; obligatorio si <paramref name="Kind"/> es <c>mesocycle</c>.</param>
/// <param name="MicrocycleNumber">Microciclo (1–4) de la sesión; obligatorio en una sesión de mesociclo.</param>
/// <param name="SessionDay">Día de la sesión en el microciclo; obligatorio en una sesión de mesociclo.</param>
public sealed record SessionLogKeyInput(
    SessionLogKind Kind,
    DateOnly SessionDate,
    Guid? MesocycleId,
    int? MicrocycleNumber,
    int? SessionDay);

/// <summary>
/// Un ítem tal y como se registra: la foto del ejercicio tal y como se mostró al atleta (papel y
/// objetivo de series/reps/segundos) más las series ejecutadas. El nombre del ejercicio y su
/// <b>unidad</b> los resuelve el servidor contra el catálogo y los congela con el resto de la foto,
/// de modo que el historial no dependa de la base de conocimiento vigente.
/// </summary>
/// <param name="ExerciseId">Slug del ejercicio en el catálogo.</param>
/// <param name="Role">Papel del ejercicio en la sesión: <c>skill</c>, <c>strength</c> o <c>core</c>.</param>
/// <param name="Pattern">Patrón de fuerza (<c>push</c>, <c>pull</c>, <c>leg</c>) o <c>null</c>.</param>
/// <param name="PrescribedSets">Número de series del objetivo.</param>
/// <param name="RepsMin">Repeticiones mínimas del objetivo; <c>null</c> si se mide en segundos.</param>
/// <param name="RepsMax">Repeticiones máximas del objetivo; <c>null</c> si se mide en segundos.</param>
/// <param name="HoldSecondsMin">Segundos mínimos del objetivo; <c>null</c> si se mide en repeticiones.</param>
/// <param name="HoldSecondsMax">Segundos máximos del objetivo; <c>null</c> si se mide en repeticiones.</param>
/// <param name="Note">Nota de la fila para la UI; <c>null</c> cuando no hay.</param>
/// <param name="Sets">Series ejecutadas con su valor real, RIR real y lastre opcionales.</param>
public sealed record SessionLogItemBody(
    string ExerciseId,
    SessionItemRole Role,
    ExerciseGroup? Pattern,
    int PrescribedSets,
    int? RepsMin,
    int? RepsMax,
    int? HoldSecondsMin,
    int? HoldSecondsMax,
    string? Note,
    IReadOnlyList<SessionLogSetInput> Sets);

/// <summary>
/// Registra lo ejecutado en un ejercicio de una sesión, serie a serie (spec 0001, US-34). El
/// servidor resuelve o crea la sesión por su clave determinista (ADR-0014) y guarda el ítem con su
/// foto: nombre y unidad los deriva del catálogo, el resto de la foto viene del plan que el atleta
/// tenía delante.
/// </summary>
/// <param name="Session">Clave de sesión determinista: tipo, fecha, mesociclo, microciclo y día.</param>
/// <param name="Item">Ítem a registrar con su foto y sus series.</param>
/// <param name="ClientId">
/// Id idempotente del cliente (outbox offline, ADR-0003). Si un ítem del mismo atleta ya tiene ese
/// id, el reintento actualiza sus series en su sitio en lugar de duplicarlo.
/// </param>
public sealed record RegisterSessionLogCommand(
    SessionLogKeyInput Session,
    SessionLogItemBody Item,
    Guid? ClientId = null) : ICommand<SessionLogResponse>;

/// <summary>
/// Sustituye las series de un ítem registrado (spec 0001, US-36; decisión D5). La identidad de la
/// sesión y la foto del ítem no cambian: editar solo pisa los valores de las series.
/// </summary>
/// <param name="SessionLogId">Sesión a la que pertenece el ítem.</param>
/// <param name="ItemId">Ítem registrado a editar.</param>
/// <param name="Sets">Nuevas series, numeradas desde 1 y en orden.</param>
public sealed record UpdateSessionLogItemCommand(
    Guid SessionLogId,
    Guid ItemId,
    IReadOnlyList<SessionLogSetInput> Sets) : ICommand<SessionLogItemResponse>;

/// <summary>
/// Borra un ítem registrado (spec 0001, US-36). Des-completa la sesión, de modo que lo que dependía
/// de ella —la <i>adherencia</i>— se recalcula; si era el último ítem, la sesión se borra también.
/// </summary>
/// <param name="SessionLogId">Sesión a la que pertenece el ítem.</param>
/// <param name="ItemId">Ítem registrado a borrar.</param>
public sealed record DeleteSessionLogItemCommand(
    Guid SessionLogId,
    Guid ItemId) : ICommand<Unit>;

/// <summary>
/// Marca una sesión registrada como completada o la des-completa (spec 0001, US-23). Ambas
/// operaciones son idempotentes: un reintento offline no pisa la marca ni pierde el registro.
/// </summary>
/// <param name="SessionLogId">Sesión a marcar.</param>
/// <param name="Completed"><c>true</c> para completarla, <c>false</c> para des-completarla.</param>
public sealed record SetSessionLogCompletionCommand(
    Guid SessionLogId,
    bool Completed) : ICommand<SessionLogResponse>;
