using Barrapp.Application.Abstractions;
using Barrapp.Domain.Sessions;

namespace Barrapp.Persistence.Repositories;

/// <summary>
/// Implementación EF Core del puerto <see cref="ISessionLogRepository"/>. No guarda: la escritura
/// la confirma el <see cref="IUnitOfWork"/> al cerrar el caso de uso.
/// </summary>
internal sealed class SessionLogRepository(ApplicationDbContext dbContext)
    : ISessionLogRepository
{
    public void Add(SessionLog sessionLog) => dbContext.SessionLogs.Add(sessionLog);
}
