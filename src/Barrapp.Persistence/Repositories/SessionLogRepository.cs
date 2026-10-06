using Barrapp.Application.Abstractions;
using Barrapp.Domain.Sessions;
using Microsoft.EntityFrameworkCore;

namespace Barrapp.Persistence.Repositories;

/// <summary>
/// Implementación EF Core del puerto <see cref="ISessionLogRepository"/>. No guarda: la escritura
/// la confirma el <see cref="IUnitOfWork"/> al cerrar el caso de uso. Las series viajan con el
/// agregado (objetos valor owned), así que la lectura por id las trae sin un include extra.
/// </summary>
internal sealed class SessionLogRepository(ApplicationDbContext dbContext)
    : ISessionLogRepository
{
    public void Add(SessionLog sessionLog) => dbContext.SessionLogs.Add(sessionLog);

    public Task<SessionLog?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        dbContext.SessionLogs.FirstOrDefaultAsync(log => log.Id == id, cancellationToken);

    public void Remove(SessionLog sessionLog) => dbContext.SessionLogs.Remove(sessionLog);
}