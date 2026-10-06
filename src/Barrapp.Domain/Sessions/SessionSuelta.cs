using Barrapp.Domain.Common;
using Barrapp.Domain.Knowledge;
using Barrapp.Domain.Planning;

namespace Barrapp.Domain.Sessions;

/// <summary>
/// Una sesión suelta guardada en el historial (spec 0001, US-28; ticket #29): los parámetros con
/// los que el atleta la pidió —tiempo, energía y foco—, el foco ya resuelto por el motor (qué ha
/// tocado, sobre todo en «sorpréndeme») y la sesión generada como snapshot en <see cref="Items"/>.
/// </summary>
/// <remarks>
/// <para>
/// El aislamiento es una decisión de estructura: la suelta vive en su propio agregado y su propia
/// tabla, fuera de los flujos de <see cref="SessionLog"/> que alimentan el avance de etapa y el
/// ajuste de máximos. Registrar una suelta (marcarla como <see cref="SessionSueltaStatus.Recorded"/>)
/// cambia solo su propio estado; nunca crea registros de sesión ni toca el mesociclo ni los
/// máximos del atleta (D8, `docs/adr`). La generación llega aquí desde <see cref="SoloSessionGenerator"/>
/// a través del caso de uso de Application; el día de la sesión no aplica (está fuera del mesociclo).
/// </para>
/// <para>
/// El snapshot guarda las filas que el motor compuso, en el mismo orden, como objetos valor
/// <see cref="SessionSueltaItem"/>. Cada suelta nace <see cref="SessionSueltaStatus.Generated"/> y,
/// cuando el atleta la da por hecha, pasa a <see cref="SessionSueltaStatus.Recorded"/> vía
/// <see cref="MarkRecorded"/>; solo esa transición existe.
/// </para>
/// </remarks>
public sealed class SessionSuelta
{
    private readonly List<SessionSueltaItem> _items = [];

    private SessionSuelta(
        Guid id,
        Guid userId,
        int timeMinutes,
        SoloSessionEnergy energy,
        SoloSessionFocus focus,
        ExerciseGroup? pattern,
        string? skillId,
        SessionSueltaStatus status,
        DateTimeOffset createdAtUtc,
        DateTimeOffset? recordedAtUtc)
    {
        Id = id;
        UserId = userId;
        TimeMinutes = timeMinutes;
        Energy = energy;
        Focus = focus;
        Pattern = pattern;
        SkillId = skillId;
        Status = status;
        CreatedAtUtc = createdAtUtc;
        RecordedAtUtc = recordedAtUtc;
    }

    // Requerido por EF Core para materializar la entidad; nunca se usa desde el dominio.
    private SessionSuelta()
    {
    }

    /// <summary>Identificador de la sesión suelta en el historial.</summary>
    public Guid Id { get; private set; }

    /// <summary>Identificador del atleta al que pertenece la suelta.</summary>
    public Guid UserId { get; private set; }

    /// <summary>Tiempo disponible con el que se pidió la suelta: 15, 30, 45 o 60 minutos.</summary>
    public int TimeMinutes { get; private set; }

    /// <summary>Energía declarada con la que se pidió la suelta.</summary>
    public SoloSessionEnergy Energy { get; private set; }

    /// <summary>Foco con el que se pidió la suelta (patrón, skill o «sorpréndeme»).</summary>
    public SoloSessionFocus Focus { get; private set; }

    /// <summary>
    /// Patrón resuelto por el motor; <c>null</c> cuando la composición es de skill. Para el foco de
    /// patrón es el grupo pedido; para «sorpréndeme», el que el motor eligió de forma determinista.
    /// </summary>
    public ExerciseGroup? Pattern { get; private set; }

    /// <summary>
    /// Skill resuelto por el motor; <c>null</c> cuando la composición es de patrón. Es el objetivo
    /// del atleta cuando la suelta toca su escalera.
    /// </summary>
    public string? SkillId { get; private set; }

    /// <summary>Estado de la suelta en el historial.</summary>
    public SessionSueltaStatus Status { get; private set; }

    /// <summary>Momento en el que se generó la suelta (UTC).</summary>
    public DateTimeOffset CreatedAtUtc { get; private set; }

    /// <summary>Momento en el que el atleta la marcó como registrada (UTC); <c>null</c> si sigue generada.</summary>
    public DateTimeOffset? RecordedAtUtc { get; private set; }

    /// <summary>Filas de la sesión generada, en orden de ejecución.</summary>
    public IReadOnlyCollection<SessionSueltaItem> Items => _items;

    /// <summary>
    /// Crea la sesión suelta del historial. Falla si el tiempo no es uno de los cuatro admitidos,
    /// si la composición resuelta no trae ni patrón ni skill (o trae ambos), si no hay filas que
    /// guardar o si alguna fila es inválida. Nace <see cref="SessionSueltaStatus.Generated"/>.
    /// </summary>
    public static Result<SessionSuelta> Create(
        Guid userId,
        int timeMinutes,
        SoloSessionEnergy energy,
        SoloSessionFocus focus,
        ExerciseGroup? pattern,
        string? skillId,
        DateTimeOffset createdAtUtc,
        IReadOnlyCollection<SessionSueltaItemInput> items)
    {
        if (timeMinutes is not (15 or 30 or 45 or 60))
        {
            return Result.Failure<SessionSuelta>(DomainErrors.SessionSuelta.TimeMinutesOutOfRange);
        }

        if (pattern is not null && skillId is not null)
        {
            return Result.Failure<SessionSuelta>(DomainErrors.SessionSuelta.CompositionConflict);
        }

        if (pattern is null && skillId is null)
        {
            return Result.Failure<SessionSuelta>(DomainErrors.SessionSuelta.CompositionRequired);
        }

        if (items is null || items.Count == 0)
        {
            return Result.Failure<SessionSuelta>(DomainErrors.SessionSuelta.ItemsRequired);
        }

        var built = new List<SessionSueltaItem>(items.Count);
        var position = 0;
        foreach (var input in items)
        {
            position++;
            var creation = SessionSueltaItem.Create(position, input);
            if (creation.IsFailure)
            {
                return Result.Failure<SessionSuelta>(creation.Error);
            }

            built.Add(creation.Value);
        }

        var suelta = new SessionSuelta(
            Guid.NewGuid(),
            userId,
            timeMinutes,
            energy,
            focus,
            pattern,
            skillId,
            SessionSueltaStatus.Generated,
            createdAtUtc,
            recordedAtUtc: null);
        suelta._items.AddRange(built);

        return suelta;
    }

    /// <summary>
    /// Marca la suelta como registrada cuando el atleta la da por hecha. No crea registros de
    /// sesión ni altera el mesociclo ni los máximos: solo cambia el estado de la suelta. Falla con
    /// <see cref="DomainErrors.SessionSuelta.AlreadyRecorded"/> si ya estaba registrada.
    /// </summary>
    public Result MarkRecorded(DateTimeOffset recordedAtUtc)
    {
        if (Status == SessionSueltaStatus.Recorded)
        {
            return Result.Failure(DomainErrors.SessionSuelta.AlreadyRecorded);
        }

        Status = SessionSueltaStatus.Recorded;
        RecordedAtUtc = recordedAtUtc;

        return Result.Success();
    }
}
