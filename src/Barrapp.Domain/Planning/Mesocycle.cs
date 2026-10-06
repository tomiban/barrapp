using Barrapp.Domain.Common;

namespace Barrapp.Domain.Planning;

/// <summary>
/// Mesociclo persistido (spec 0001, US-29; ticket #27, D7): desde que el plan se genera por el
/// motor, el atleta tiene un mesociclo en curso que se guarda como snapshot. Permite listar los
/// mesociclos pasados y abrir su detalle, y dará a los <c>SessionLog</c> el mesociclo al que
/// pertenecen (cómo se cierra y ajusta los máximos es el ticket #24).
/// </summary>
/// <remarks>
/// <para>
/// La generación del plan sigue siendo del motor (<see cref="PlanGenerator"/>); este agregado solo
/// lo persiste: la <see cref="Snapshot"/> es el plan tal y como se generó, y la unidad del mesociclo
/// de datos es id / usuario / skill / frecuencia / estado / fechas + snapshot. Un atleta tiene a lo
/// sumo un mesociclo <see cref="MesocycleStatus.Active"/>; cada nueva generación lo reemplaza.
/// </para>
/// <para>
/// El cierre (<see cref="Close"/>) es el <i>seam</i> del ticket #24: cambia el estado a
/// <see cref="MesocycleStatus.Closed"/> con su fecha, y deja preparado el camino para que ese
/// ticket ajuste los máximos del atleta con los <c>SessionLog</c> del mesociclo. Solo la
/// transición <see cref="MesocycleStatus.Active"/> → <see cref="MesocycleStatus.Closed"/> existe.
/// </para>
/// </remarks>
public sealed class Mesocycle
{
    private Mesocycle(
        Guid id,
        Guid userId,
        string skillId,
        int trainingDays,
        MesocycleStatus status,
        DateTimeOffset startedAtUtc,
        DateTimeOffset? closedAtUtc,
        MesocycleSnapshot snapshot)
    {
        Id = id;
        UserId = userId;
        SkillId = skillId;
        TrainingDays = trainingDays;
        Status = status;
        StartedAtUtc = startedAtUtc;
        ClosedAtUtc = closedAtUtc;
        Snapshot = snapshot;
    }

    // Requerido por EF Core para materializar la entidad; nunca se usa desde el dominio.
    private Mesocycle()
    {
    }

    /// <summary>Identificador del mesociclo en el historial.</summary>
    public Guid Id { get; private set; }

    /// <summary>Identificador del atleta al que pertenece el mesociclo.</summary>
    public Guid UserId { get; private set; }

    /// <summary>Slug del skill objetivo del mesociclo.</summary>
    public string SkillId { get; private set; } = string.Empty;

    /// <summary>Días de entrenamiento por semana.</summary>
    public int TrainingDays { get; private set; }

    /// <summary>Estado del mesociclo: en curso o cerrado.</summary>
    public MesocycleStatus Status { get; private set; }

    /// <summary>Momento en el que se generó el mesociclo (UTC).</summary>
    public DateTimeOffset StartedAtUtc { get; private set; }

    /// <summary>Momento en el que se cerró el mesociclo (UTC); <c>null</c> mientras sigue activo.</summary>
    public DateTimeOffset? ClosedAtUtc { get; private set; }

    /// <summary>El plan tal y como se generó, en forma serializable.</summary>
    public MesocycleSnapshot Snapshot { get; private set; } = new();

    /// <summary>
    /// Crea el mesociclo en curso a partir del plan generado por el motor. Falla con
    /// <see cref="DomainErrors.Mesocycle.InvalidSnapshot"/> si el plan no es un mesociclo íntegro
    /// (sin semanas, con una semana sin sesiones o una sesión sin filas). Nace
    /// <see cref="MesocycleStatus.Active"/>.
    /// </summary>
    public static Result<Mesocycle> Create(Guid userId, Plan plan, DateTimeOffset startedAtUtc)
    {
        if (plan is null
            || plan.Microcycles.Count == 0
            || plan.Microcycles.Any(microcycle => microcycle.Sessions.Count == 0)
            || plan.Microcycles.SelectMany(microcycle => microcycle.Sessions)
                .Any(session => session.Items.Count == 0))
        {
            return Result.Failure<Mesocycle>(DomainErrors.Mesocycle.InvalidSnapshot);
        }

        return new Mesocycle(
            Guid.NewGuid(),
            userId,
            plan.SkillId,
            plan.TrainingDays,
            MesocycleStatus.Active,
            startedAtUtc,
            closedAtUtc: null,
            MesocycleSnapshot.FromPlan(plan));
    }

    /// <summary>
    /// Cierra el mesociclo: lo deja <see cref="MesocycleStatus.Closed"/> con su fecha y pasa a
    /// formar parte del historial de pasados. Falla con <see cref="DomainErrors.Mesocycle.AlreadyClosed"/>
    /// si ya estaba cerrado. El ajuste de máximos a partir de los registros es del ticket #24.
    /// </summary>
    public Result Close(DateTimeOffset closedAtUtc)
    {
        if (Status == MesocycleStatus.Closed)
        {
            return Result.Failure(DomainErrors.Mesocycle.AlreadyClosed);
        }

        Status = MesocycleStatus.Closed;
        ClosedAtUtc = closedAtUtc;

        return Result.Success();
    }
}