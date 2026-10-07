using Barrapp.Application.Abstractions;
using Barrapp.Domain.Sessions;
using Microsoft.EntityFrameworkCore;

namespace Barrapp.Persistence.Repositories;

/// <summary>
/// Implementación EF Core del puerto <see cref="ISessionLogRepository"/>. No guarda: la escritura
/// la confirma el <see cref="IUnitOfWork"/> al cerrar el caso de uso. La cabecera del registro
/// viaja con sus ítems (relación one-to-many en cascada) y cada ítem con sus series (objetos valor
/// owned), así que la lectura por id las trae sin un include extra.
/// </summary>
internal sealed class SessionLogRepository(ApplicationDbContext dbContext)
    : ISessionLogRepository
{
    public void Add(SessionLog sessionLog) => dbContext.SessionLogs.Add(sessionLog);

    public Task<SessionLog?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        dbContext.SessionLogs
            .Include(log => log.Items)
            .FirstOrDefaultAsync(log => log.Id == id, cancellationToken);

    public Task<SessionLog?> GetByKeyAsync(
        SessionLogKey key,
        CancellationToken cancellationToken = default) =>
        dbContext.SessionLogs
            .Include(log => log.Items)
            .FirstOrDefaultAsync(
                log => log.UserId == key.UserId
                    && log.Kind == key.Kind
                    && log.MesocycleId == key.MesocycleId
                    && log.MicrocycleNumber == key.MicrocycleNumber
                    && log.SessionDay == key.SessionDay
                    && log.SessionDate == key.SessionDate,
                cancellationToken);

    public Task<SessionLogItem?> GetItemByIdAsync(Guid itemId, CancellationToken cancellationToken = default) =>
        dbContext.SessionLogItems
            .FirstOrDefaultAsync(item => item.Id == itemId, cancellationToken);

    public Task<SessionLogItem?> GetItemByClientIdAsync(
        Guid clientId,
        CancellationToken cancellationToken = default) =>
        dbContext.SessionLogItems.FirstOrDefaultAsync(item => item.ClientId == clientId, cancellationToken);

    public void Remove(SessionLog sessionLog) => dbContext.SessionLogs.Remove(sessionLog);
}
