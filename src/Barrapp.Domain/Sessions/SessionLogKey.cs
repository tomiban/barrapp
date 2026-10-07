using Barrapp.Domain.Sessions;

namespace Barrapp.Domain.Sessions;

/// <summary>
/// Clave de sesión determinista del registro (ADR-0014): identifica qué sesión del plan —o sesión
/// suelta— es la que se está anotando, sin referenciar el plan persistido. La comparten la
/// resolución del registro en el servidor y la cola offline del cliente.
/// </summary>
/// <remarks>
/// De una sesión de mesociclo la clave es (atleta, mesociclo, microciclo y día) más la fecha; de una
/// sesión suelta es (atleta, tipo suelta, fecha). La fecha entra en la clave para que dos registros
/// hechos el mismo día de sesión en semanas distintas —o dos sueltas del mismo día— no se mezclen.
/// </remarks>
/// <param name="UserId">Atleta al que pertenece el registro.</param>
/// <param name="Kind">Origen de la sesión: del mesociclo o suelta.</param>
/// <param name="SessionDate">Fecha de la sesión.</param>
/// <param name="MesocycleId">Mesociclo de la sesión; <c>null</c> en la suelta.</param>
/// <param name="MicrocycleNumber">Microciclo (1–4) de la sesión; <c>null</c> en la suelta.</param>
/// <param name="SessionDay">Día de la sesión en el microciclo; <c>null</c> en la suelta.</param>
public sealed record SessionLogKey(
    Guid UserId,
    SessionLogKind Kind,
    DateOnly SessionDate,
    Guid? MesocycleId,
    int? MicrocycleNumber,
    int? SessionDay)
{
    /// <summary>
    /// Construye la clave de una sesión de mesociclo, cuya identidad es mesociclo, microciclo y día.
    /// </summary>
    public static SessionLogKey ForMesocycle(
        Guid userId,
        Guid mesocycleId,
        int microcycleNumber,
        int sessionDay,
        DateOnly sessionDate) =>
        new(userId, SessionLogKind.Mesocycle, sessionDate, mesocycleId, microcycleNumber, sessionDay);

    /// <summary>
    /// Construye la clave de una sesión suelta: sin mesociclo, microciclo ni día (ADR-0014).
    /// </summary>
    public static SessionLogKey ForSuelta(Guid userId, DateOnly sessionDate) =>
        new(userId, SessionLogKind.Suelta, sessionDate, MesocycleId: null, MicrocycleNumber: null, SessionDay: null);
}
