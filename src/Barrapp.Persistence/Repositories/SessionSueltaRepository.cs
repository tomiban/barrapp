using Barrapp.Application.Abstractions;
using Barrapp.Domain.Sessions;
using Microsoft.EntityFrameworkCore;

namespace Barrapp.Persistence.Repositories;

/// <summary>
/// Implementación EF Core del puerto <see cref="ISessionSueltaRepository"/>. No guarda: la
/// escritura la confirma el <see cref="IUnitOfWork"/> al cerrar el caso de uso. La suelta vive en
/// su propia tabla, fuera de los flujos de <see cref="SessionLog"/>.
/// </summary>
internal sealed class SessionSueltaRepository(ApplicationDbContext dbContext)
    : ISessionSueltaRepository
{
    public async Task<SessionSuelta?> GetByIdAsync(Guid id, Guid userId, CancellationToken cancellationToken) =>
        await dbContext.SessionSuelta
            .FirstOrDefaultAsync(
                candidate => candidate.Id == id && candidate.UserId == userId,
                cancellationToken);

    public void Add(SessionSuelta sessionSuelta) => dbContext.SessionSuelta.Add(sessionSuelta);
}
