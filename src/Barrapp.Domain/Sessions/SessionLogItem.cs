namespace Barrapp.Domain.Sessions;

using Barrapp.Domain.Common;
using Barrapp.Domain.Knowledge;
using Barrapp.Domain.Planning;

/// <summary>
/// Un ítem registrado de una sesión: la foto del ejercicio (nombre, papel, patrón, unidad y
/// objetivo) con las series realmente ejecutadas. Es la fila que el Historial muestra y la que
/// guarda la evidencia de lo que se prescribió, para que cambiar la base de conocimiento no
/// reescriba el pasado (ADR-0014).
/// </summary>
/// <remarks>
/// Las series son objetos valor <see cref="SessionLogSet"/> y toda escritura pasa por
/// <see cref="Create"/>. Un ítem se identifica por su <see cref="Id"/> dentro de la sesión y, para
/// la idempotencia offline, por su <see cref="ClientId"/>.
/// </remarks>
public sealed class SessionLogItem
{
    private readonly List<SessionLogSet> _sets = [];

    private SessionLogItem(Guid id, int position)
    {
        Id = id;
        Position = position;
    }

    // Requerido por EF Core para materializar la entidad; nunca se usa desde el dominio.
    private SessionLogItem()
    {
    }

    /// <summary>Identificador del ítem registrado.</summary>
    public Guid Id { get; private set; }

    /// <summary>Orden del ítem dentro de la sesión (desde 1), como en la prescripción.</summary>
    public int Position { get; private set; }

    /// <summary>Slug del ejercicio en el catálogo.</summary>
    public string ExerciseId { get; private set; } = string.Empty;

    /// <summary>Nombre del ejercicio tal y como se mostró al registrar.</summary>
    public string ExerciseName { get; private set; } = string.Empty;

    /// <summary>Papel del ejercicio en la sesión: bloque de skill, fuerza o core.</summary>
    public SessionItemRole Role { get; private set; }

    /// <summary>Patrón de fuerza; <c>null</c> en el bloque de skill y en el core.</summary>
    public ExerciseGroup? Pattern { get; private set; }

    /// <summary>Unidad del valor real: repeticiones o segundos mantenidos.</summary>
    public Metric Metric { get; private set; }

    /// <summary>Número de series prescritas en el objetivo.</summary>
    public int PrescribedSets { get; private set; }

    /// <summary>Repeticiones mínimas prescritas; <c>null</c> si el ejercicio se mide en segundos.</summary>
    public int? RepsMin { get; private set; }

    /// <summary>Repeticiones máximas prescritas; <c>null</c> si el ejercicio se mide en segundos.</summary>
    public int? RepsMax { get; private set; }

    /// <summary>Segundos mínimos prescritos; <c>null</c> si el ejercicio se mide en repeticiones.</summary>
    public int? HoldSecondsMin { get; private set; }

    /// <summary>Segundos máximos prescritos; <c>null</c> si el ejercicio se mide en repeticiones.</summary>
    public int? HoldSecondsMax { get; private set; }

    /// <summary>Nota de la fila para la UI; <c>null</c> cuando no hay nada que explicar.</summary>
    public string? Note { get; private set; }

    /// <summary>
    /// Id idempotente del cliente (la outbox offline) para reintentar el alta sin duplicar el ítem;
    /// <c>null</c> si el alta vino del API sin idempotencia.
    /// </summary>
    public Guid? ClientId { get; private set; }

    /// <summary>Series realmente ejecutadas, en orden.</summary>
    public IReadOnlyCollection<SessionLogSet> Sets => _sets;

    /// <summary>
    /// Crea un ítem registrado. Falla si no trae ejercicio, si el objetivo no declara series, si un
    /// rango de prescripción queda a medias o invertido, si no hay series o si alguna serie es
    /// inválida (número &lt; 1, valor negativo, RIR real fuera de 0–10 o lastre negativo). Nunca
    /// muta nada: la sesión que lo invoca solo lo agrega si el resultado es exitoso.
    /// </summary>
    public static Result<SessionLogItem> Create(int position, SessionLogItemInput input)
    {
        var built = TryBuild(input);
        if (built.IsFailure)
        {
            return Result.Failure<SessionLogItem>(built.Error);
        }

        var item = new SessionLogItem(Guid.NewGuid(), position);
        item.Copy(input);
        item._sets.AddRange(built.Value);

        return item;
    }

    /// <summary>
    /// Vuelve a escribir la foto del ítem y sus series conservando su identidad: lo usa la sesión
    /// al volver a registrar el mismo ejercicio (última escritura gana), de modo que la fila y sus
    /// series siguen siendo las mismas. Es atómico: si la entrada no es válida, el ítem queda como
    /// estaba.
    /// </summary>
    internal Result Apply(SessionLogItemInput input)
    {
        var built = TryBuild(input);
        if (built.IsFailure)
        {
            return Result.Failure(built.Error);
        }

        Copy(input);
        _sets.Clear();
        _sets.AddRange(built.Value);

        return Result.Success();
    }

    /// <summary>
    /// Sustituye las series del ítem (spec 0001, US-36; decisión D5: editar reemplaza los valores de
    /// la serie) aplicando los mismos invariantes que <see cref="Create"/>, de forma atómica: si
    /// alguna serie es inválida, el ítem conserva las suyas. La foto del ítem no cambia.
    /// </summary>
    internal Result UpdateSets(IReadOnlyCollection<SessionLogSetInput> sets)
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
    /// Valida la foto del ítem y construye sus series, sin mutar nada. La comparten la creación y
    /// la reescritura para que un ítem nunca quede con media foto.
    /// </summary>
    private static Result<IReadOnlyList<SessionLogSet>> TryBuild(SessionLogItemInput input)
    {
        if (string.IsNullOrWhiteSpace(input.ExerciseId) || string.IsNullOrWhiteSpace(input.ExerciseName))
        {
            return Result.Failure<IReadOnlyList<SessionLogSet>>(DomainErrors.SessionLog.ExerciseRequired);
        }

        if (input.PrescribedSets < 1)
        {
            return Result.Failure<IReadOnlyList<SessionLogSet>>(
                DomainErrors.SessionLog.PrescribedSetsOutOfRange);
        }

        if (HasHalfRange(input.RepsMin, input.RepsMax)
            || HasHalfRange(input.HoldSecondsMin, input.HoldSecondsMax))
        {
            return Result.Failure<IReadOnlyList<SessionLogSet>>(
                DomainErrors.SessionLog.PrescriptionRangeIncomplete);
        }

        if ((input.RepsMin is not null && input.RepsMax is not null && input.RepsMin > input.RepsMax)
            || (input.HoldSecondsMin is not null
                && input.HoldSecondsMax is not null
                && input.HoldSecondsMin > input.HoldSecondsMax))
        {
            return Result.Failure<IReadOnlyList<SessionLogSet>>(
                DomainErrors.SessionLog.PrescriptionRangeInverted);
        }

        return TryBuildSets(input.Sets);
    }

    /// <summary>
    /// Valida y construye las series de la entrada, aplicando los invariantes compartidos por la
    /// creación y la edición (al menos una serie, número &ge; 1, valor no negativo, RIR real 0–10,
    /// lastre no negativo y números consecutivos desde 1). Nunca muta.
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
            var creation = SessionLogSet.Create(input.SetNumber, input.Value, input.ActualRir, input.LoadKg);
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

    private static bool HasHalfRange(int? min, int? max) => (min is null) != (max is null);

    /// <summary>Copia la foto del ítem desde la entrada; la validación llega antes.</summary>
    private void Copy(SessionLogItemInput input)
    {
        ExerciseId = input.ExerciseId;
        ExerciseName = input.ExerciseName;
        Role = input.Role;
        Pattern = input.Pattern;
        Metric = input.Metric;
        PrescribedSets = input.PrescribedSets;
        RepsMin = input.RepsMin;
        RepsMax = input.RepsMax;
        HoldSecondsMin = input.HoldSecondsMin;
        HoldSecondsMax = input.HoldSecondsMax;
        Note = input.Note;
        ClientId = input.ClientId;
    }
}
