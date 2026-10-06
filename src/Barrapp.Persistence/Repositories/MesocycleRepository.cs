using Barrapp.Application.Abstractions;
using Barrapp.Domain.Planning;
using Microsoft.EntityFrameworkCore;

namespace Barrapp.Persistence.Repositories;

/// <summary>
/// Implementación EF Core del puerto <see cref="IMesocycleRepository"/>. No guarda: la escritura
/// la confirma el <see cref="IUnitOfWork"/> al cerrar el caso de uso.
/// </summary>
internal sealed class MesocycleRepository(ApplicationDbContext dbContext)
    : IMesocycleRepository
{
    public async Task<Mesocycle?> GetByIdAsync(Guid id, Guid userId, CancellationToken cancellationToken) =>
        await dbContext.Mesocycles
            .FirstOrDefaultAsync(
                candidate => candidate.Id == id && candidate.UserId == userId,
                cancellationToken);

    public async Task<Mesocycle?> GetActiveByUserIdAsync(Guid userId, CancellationToken cancellationToken) =>
        await dbContext.Mesocycles
            .FirstOrDefaultAsync(
                candidate => candidate.UserId == userId && candidate.Status == MesocycleStatus.Active,
                cancellationToken);

    public void Add(Mesocycle mesocycle) => dbContext.Mesocycles.Add(mesocycle);

    public void Remove(Mesocycle mesocycle) => dbContext.Mesocycles.Remove(mesocycle);
}