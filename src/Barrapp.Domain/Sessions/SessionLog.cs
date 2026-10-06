using Barrapp.Domain.Common;

namespace Barrapp.Domain.Sessions;

/// <summary>
/// Registro de lo realmente ejecutado en un ejercicio de una sesión del plan, serie a serie
/// (spec 0001, US-34). Guarda reps en ejercicios de fuerza y segundos en holds/skill: el valor
/// es un número y la unidad la deriva el tipo de ejercicio, nunca el cliente.
/// </summary>
/// <remarks>
/// Pertenece a un usuario (<see cref="UserId"/>); en el MVP mono-usuario, todos al usuario fijo.
/// La sesión se identifica por su día dentro del mesociclo (<see cref="SessionDay"/>) y, cuando
/// el mesociclo se persista (ticket #27), por <see cref="MesocycleId"/>. <see cref="ClientId"/> es
/// el id idempotente que genera el cliente de la app (la outbox offline, #26): el servidor lo usa
/// para no duplicar filas cuando un envío se repite tras perder la respuesta, y es opcional, de
/// modo que el alta directa del API sin idempotencia sigue existiendo. Las series son objetos
/// valor <see cref="SessionLogSet"/> y toda escritura pasa por <see cref="Create"/>.
/// </remarks>
public sealed class SessionLog
{
    private readonly List<SessionLogSet> _sets = [];

    private SessionLog(
        Guid id,
        Guid userId,
        string exerciseId,
        Guid? mesocycleId,
        int sessionDay,
        DateTimeOffset recordedAtUtc,
        Guid? clientId)
    {
        Id = id;
        UserId = userId;
        ExerciseId = exerciseId;
        MesocycleId = mesocycleId;
        SessionDay = sessionDay;
        RecordedAtUtc = recordedAtUtc;
        ClientId = clientId;
    }

    // Requerido por EF Core para materializar la entidad; nunca se usa desde el dominio.
    private SessionLog()
    {
    }

    /// <summary>Identificador del registro.</summary>
    public Guid Id { get; private set; }

    /// <summary>Identificador del atleta al que pertenece el registro.</summary>
    public Guid UserId { get; private set; }

    /// <summary>Identificador del ejercicio registrado en el catálogo.</summary>
    public string ExerciseId { get; private set; } = string.Empty;

    /// <summary>
    /// Identificador del mesociclo al que pertenece la sesión, si el plan ya está persistido
    /// (ticket #27). <c>null</c> mientras el mesociclo se genera al vuelo.
    /// </summary>
    public Guid? MesocycleId { get; private set; }

    /// <summary>Día de la sesión dentro del mesociclo (1 en adelante).</summary>
    public int SessionDay { get; private set; }

    /// <summary>Momento en el que se registró la sesión (UTC).</summary>
    public DateTimeOffset RecordedAtUtc { get; private set; }

    /// <summary>
    /// Id idempotente que genera el cliente (la outbox offline, #26) para identificar este registro
    /// entre reintentos; <c>null</c> si el alta vino del API sin idempotencia. El servidor lo
    /// garantiza único por atleta (índice filtrado en la base).
    /// </summary>
    public Guid? ClientId { get; private set; }

    /// <summary>Series del ejercicio, en orden, con el valor real ejecutado en cada una.</summary>
    public IReadOnlyCollection<SessionLogSet> Sets => _sets;

    /// <summary>
    /// Crea un registro de sesión. Falla si el día de la sesión es menor que 1, si no hay series,
    /// si alguna serie tiene un valor o esfuerzo inválidos, o si los números de serie no son
    /// consecutivos desde 1.
    /// </summary>
    public static Result<SessionLog> Create(
        Guid userId,
        string exerciseId,
        Guid? mesocycleId,
        int sessionDay,
        DateTimeOffset recordedAtUtc,
        IReadOnlyCollection<SessionLogSetInput> sets,
        Guid? clientId = null)
    {
        if (sessionDay < 1)
        {
            return Result.Failure<SessionLog>(DomainErrors.SessionLog.SessionDayOutOfRange);
        }

        var built = TryBuildSets(sets);
        if (built.IsFailure)
        {
            return Result.Failure<SessionLog>(built.Error);
        }

        var log = new SessionLog(
            Guid.NewGuid(),
            userId,
            exerciseId,
            mesocycleId,
            sessionDay,
            recordedAtUtc,
            clientId);
        log._sets.AddRange(built.Value);

        return log;
    }

    /// <summary>
    /// Sustituye las series del registro por las indicadas (spec 0001, US-36; decisión D5:
    /// editar reemplaza los valores de la serie). Aplica los mismos invariantes que
    /// <see cref="Create"/> —al menos una serie, valores no negativos, esfuerzo 0–10 y números
    /// consecutivos desde 1— y falla sin tocar el registro si alguno no se cumple (atómico).
    /// La identidad de la sesión (ejercicio, día, mesociclo, momento) no cambia.
    /// </summary>
    public Result Update(IReadOnlyCollection<SessionLogSetInput> sets)
    {
        var built = TryBuildSets(sets);
        if (built.IsFailure)
        {
            return Result.Failure(built.Error);
        }

        _sets.Clear();
        _sets.AddRange(built.Value);

        return Result.Success();
    }

    /// <summary>
    /// Valida y construye las series de la entrada, aplicando los invariantes compartidos por
    /// <see cref="Create"/> y <see cref="Update"/>. Nunca muta el registro.
    /// </summary>
    private static Result<IReadOnlyList<SessionLogSet>> TryBuildSets(
        IReadOnlyCollection<SessionLogSetInput> sets)
    {
        if (sets is null || sets.Count == 0)
        {
            return Result.Failure<IReadOnlyList<SessionLogSet>>(DomainErrors.SessionLog.SetsRequired);
        }

        var built = new List<SessionLogSet>(sets.Count);
        foreach (var input in sets)
        {
            var creation = SessionLogSet.Create(input.SetNumber, input.Value, input.Effort);
            if (creation.IsFailure)
            {
                return Result.Failure<IReadOnlyList<SessionLogSet>>(creation.Error);
            }

            built.Add(creation.Value);
        }

        for (var index = 0; index < built.Count; index++)
        {
            if (built[index].SetNumber != index + 1)
            {
                return Result.Failure<IReadOnlyList<SessionLogSet>>(
                    DomainErrors.SessionLog.SetNumbersNotConsecutive);
            }
        }

        return built;
    }
}
