using Barrapp.Domain.Common;

namespace Barrapp.Domain.Sessions;

/// <summary>
/// <b>Registro</b> de una sesión: lo realmente ejecutado, anotado serie a serie (spec 0001, US-34;
/// ver <c>GLOSSARY.md</c>). Es el agregado que se ancla al plan <b>en lectura</b> por una clave de
/// sesión determinista (ADR-0014) y la <b>foto por ítem</b> que lo hace legible aunque la base de
/// conocimiento cambie.
/// </summary>
/// <remarks>
/// <para>
/// La cabecera guarda <see cref="SessionDate"/> (fecha), <see cref="Kind"/>,
/// <see cref="MesocycleId"/>, <see cref="MicrocycleNumber"/>, <see cref="SessionDay"/>,
/// <see cref="RecordedAtUtc"/> y la marca de <see cref="CompletedAtUtc"/>. El plan no se persiste
/// ni se referencia con claves foráneas: mesociclo, microciclo y día son valores.
/// </para>
/// <para>
/// Los <see cref="SessionLogItem"/> son la foto por ítem —ejercicio, papel y objetivo de
/// series/reps/segundos— con las series ejecutadas. El <see cref="RecordedAtUtc"/> lo fija el
/// servidor; el <see cref="ClientId"/> de cada ítem es el id idempotente de la outbox offline
/// (ADR-0003), de modo que un reintento actualiza el ítem en lugar de duplicarlo.
/// </para>
/// <para>
/// Toda escritura pasa por la fábrica o por sus métodos: ningún campo es mutable desde fuera.
/// </para>
/// </remarks>
public sealed class SessionLog
{
    private readonly List<SessionLogItem> _items = [];

    private SessionLog(
        Guid id,
        Guid userId,
        SessionLogKind kind,
        DateOnly sessionDate,
        Guid? mesocycleId,
        int? microcycleNumber,
        int? sessionDay,
        DateTimeOffset recordedAtUtc)
    {
        Id = id;
        UserId = userId;
        Kind = kind;
        SessionDate = sessionDate;
        MesocycleId = mesocycleId;
        MicrocycleNumber = microcycleNumber;
        SessionDay = sessionDay;
        RecordedAtUtc = recordedAtUtc;
    }

    // Requerido por EF Core para materializar la entidad; nunca se usa desde el dominio.
    private SessionLog()
    {
    }

    /// <summary>Identificador del registro de sesión.</summary>
    public Guid Id { get; private set; }

    /// <summary>Identificador del atleta al que pertenece el registro.</summary>
    public Guid UserId { get; private set; }

    /// <summary>Origen de la sesión: del mesociclo o suelta (ADR-0014).</summary>
    public SessionLogKind Kind { get; private set; }

    /// <summary>Fecha de la sesión (día natural, sin zona horaria).</summary>
    public DateOnly SessionDate { get; private set; }

    /// <summary>
    /// Identificador del mesociclo al que pertenece la sesión. <c>null</c> en una sesión suelta.
    /// Es un valor, no una clave foránea: el plan no se persiste (ADR-0012, ADR-0014).
    /// </summary>
    public Guid? MesocycleId { get; private set; }

    /// <summary>Microciclo (semana) de la sesión dentro del mesociclo, de 1 a 4.</summary>
    public int? MicrocycleNumber { get; private set; }

    /// <summary>Día de la sesión dentro del microciclo (desde 1).</summary>
    public int? SessionDay { get; private set; }

    /// <summary>Momento en el que se registró la sesión (UTC); lo fija el servidor.</summary>
    public DateTimeOffset RecordedAtUtc { get; private set; }

    /// <summary>
    /// Momento en el que la sesión se dio por completada (UTC); <c>null</c> mientras sigue
    /// pendiente. Es la marca de US23: una sesión completada cuenta para la <i>adherencia</i>.
    /// </summary>
    public DateTimeOffset? CompletedAtUtc { get; private set; }

    /// <summary>Indica si la sesión está marcada como completada.</summary>
    public bool IsCompleted => CompletedAtUtc is not null;

    /// <summary>Ítems registrados de la sesión, en orden de ejecución.</summary>
    public IReadOnlyCollection<SessionLogItem> Items => _items;

    /// <summary>
    /// Crea el registro de una sesión. Una sesión de <see cref="SessionLogKind.Mesocycle"/> exige
    /// su clave completa —mesociclo, microciclo de 1 a 4 y día desde 1—; una
    /// <see cref="SessionLogKind.Suelta"/> no admite ninguno de los tres. Falla con
    /// <see cref="DomainErrors.SessionLog.MesocycleKeyRequired"/>,
    /// <see cref="DomainErrors.SessionLog.SueltaKeyNotAllowed"/>,
    /// <see cref="DomainErrors.SessionLog.MicrocycleOutOfRange"/> o
    /// <see cref="DomainErrors.SessionLog.SessionDayOutOfRange"/>. Nace sin ítems y sin completar.
    /// </summary>
    public static Result<SessionLog> Create(
        Guid userId,
        SessionLogKind kind,
        DateOnly sessionDate,
        Guid? mesocycleId,
        int? microcycleNumber,
        int? sessionDay,
        DateTimeOffset recordedAtUtc)
    {
        if (kind == SessionLogKind.Mesocycle)
        {
            if (mesocycleId is null)
            {
                return Result.Failure<SessionLog>(DomainErrors.SessionLog.MesocycleKeyRequired);
            }

            if (microcycleNumber is null or < 1 or > 4)
            {
                return Result.Failure<SessionLog>(DomainErrors.SessionLog.MicrocycleOutOfRange);
            }

            if (sessionDay is null or < 1)
            {
                return Result.Failure<SessionLog>(DomainErrors.SessionLog.SessionDayOutOfRange);
            }
        }
        else if (mesocycleId is not null || microcycleNumber is not null || sessionDay is not null)
        {
            return Result.Failure<SessionLog>(DomainErrors.SessionLog.SueltaKeyNotAllowed);
        }

        return new SessionLog(
            Guid.NewGuid(),
            userId,
            kind,
            sessionDate,
            mesocycleId,
            microcycleNumber,
            sessionDay,
            recordedAtUtc);
    }

    /// <summary>
    /// Registra un ítem de la sesión con su foto y sus series. Si el ejercicio ya estaba
    /// registrado, lo sustituye en su sitio —misma posición y mismo identificador— con la última
    /// escritura (last-write-wins, coherente con la sincronización offline, ADR-0003). Falla, sin
    /// tocar la sesión, si el ítem no cumple los invariantes de <see cref="SessionLogItem.Create"/>.
    /// </summary>
    public Result<SessionLogItem> UpsertItem(SessionLogItemInput input)
    {
        var existing = FindItemByExercise(input.ExerciseId);
        if (existing is not null)
        {
            var applied = existing.Apply(input);
            return applied.IsFailure
                ? Result.Failure<SessionLogItem>(applied.Error)
                : existing;
        }

        var creation = SessionLogItem.Create(Id, _items.Count + 1, input);
        if (creation.IsFailure)
        {
            return creation;
        }

        _items.Add(creation.Value);

        return creation;
    }

    /// <summary>
    /// Sustituye las series de un ítem ya registrado (spec 0001, US-36; decisión D5: editar
    /// reemplaza los valores de la serie). Falla con
    /// <see cref="DomainErrors.SessionLog.ItemNotFound"/> si el ítem no pertenece a la sesión y, si
    /// las series no son válidas, conserva las suyas (atómico). La foto del ítem no cambia.
    /// </summary>
    public Result<SessionLogItem> UpdateItemSets(
        Guid itemId,
        IReadOnlyCollection<SessionLogSetInput> sets)
    {
        var item = FindItem(itemId);
        if (item is null)
        {
            return Result.Failure<SessionLogItem>(DomainErrors.SessionLog.ItemNotFound);
        }

        var update = item.UpdateSets(sets);
        if (update.IsFailure)
        {
            return Result.Failure<SessionLogItem>(update.Error);
        }

        return item;
    }

    /// <summary>
    /// Borra un ítem del registro (spec 0001, US-36). Des-completa la sesión: lo que dependía de
    /// ella —la <i>adherencia</i>— se recalcula sobre lo que queda. Falla con
    /// <see cref="DomainErrors.SessionLog.ItemNotFound"/> si el ítem no pertenece a la sesión.
    /// </summary>
    public Result RemoveItem(Guid itemId)
    {
        var item = FindItem(itemId);
        if (item is null)
        {
            return Result.Failure(DomainErrors.SessionLog.ItemNotFound);
        }

        _items.Remove(item);
        CompletedAtUtc = null;

        return Result.Success();
    }

    /// <summary>
    /// Marca la sesión como completada con su marca temporal (spec 0001, US-23): cuenta para la
    /// <i>adherencia</i>. Es idempotente: si ya estaba completada conserva la primera marca, de modo
    /// que un reintento offline no la pise.
    /// </summary>
    public Result Complete(DateTimeOffset completedAtUtc)
    {
        CompletedAtUtc ??= completedAtUtc;

        return Result.Success();
    }

    /// <summary>
    /// Des-completa la sesión: deja de contar para la <i>adherencia</i> sin perder los ítems
    /// registrados. Es idempotente.
    /// </summary>
    public Result Uncomplete()
    {
        CompletedAtUtc = null;

        return Result.Success();
    }

    private SessionLogItem? FindItem(Guid itemId) => _items.FirstOrDefault(candidate => candidate.Id == itemId);

    /// <summary>El ítem de la sesión que registra ese ejercicio, o <c>null</c> si no está.</summary>
    private SessionLogItem? FindItemByExercise(string? exerciseId) =>
        _items.FirstOrDefault(candidate =>
            string.Equals(candidate.ExerciseId, exerciseId, StringComparison.Ordinal));
}
