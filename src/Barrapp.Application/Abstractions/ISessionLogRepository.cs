using Barrapp.Domain.Sessions;

namespace Barrapp.Application.Abstractions;

/// <summary>
/// Puerto de persistencia del registro de sesión. Las escrituras las confirma la unidad de trabajo;
/// las lecturas del listado las proyecta <see cref="IApplicationDbContext"/>.
/// </summary>
public interface ISessionLogRepository
{
    /// <summary>Marca un registro nuevo para que se inserte al guardar la unidad de trabajo.</summary>
    void Add(SessionLog sessionLog);

    /// <summary>Recupera un registro por su identificador, con sus ítems, o <c>null</c> si no existe.</summary>
    Task<SessionLog?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Recupera el registro de la clave de sesión determinista (ADR-0014) con sus ítems, o
    /// <c>null</c> si esa sesión todavía no tiene registro. Es la vía por la que un alta sin
    /// idempotencia no duplica la sesión.
    /// </summary>
    Task<SessionLog?> GetByKeyAsync(SessionLogKey key, CancellationToken cancellationToken = default);

    /// <summary>Recupera un ítem registrado por su identificador, o <c>null</c> si no existe.</summary>
    Task<SessionLogItem?> GetItemByIdAsync(Guid itemId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Recupera el ítem idempotente de un cliente por su id de cliente (outbox offline, ADR-0003), o
    /// <c>null</c> si este reintento aún no tiene fila.
    /// </summary>
    Task<SessionLogItem?> GetItemByClientIdAsync(
        Guid clientId,
        CancellationToken cancellationToken = default);

    /// <summary>Marca un registro para que se elimine al guardar la unidad de trabajo.</summary>
    void Remove(SessionLog sessionLog);
}
