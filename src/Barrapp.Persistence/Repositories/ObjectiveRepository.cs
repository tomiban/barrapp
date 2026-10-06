using Barrapp.Application.Abstractions;
using Barrapp.Domain.Objectives;
using Microsoft.EntityFrameworkCore;

namespace Barrapp.Persistence.Repositories;

/// <summary>
/// Implementación EF Core del puerto <see cref="IObjectiveRepository"/>. No guarda: la escritura
/// la confirma el <see cref="IUnitOfWork"/> al cerrar el caso de uso.
/// </summary>
internal sealed class ObjectiveRepository(ApplicationDbContext dbContext) : IObjectiveRepository
{
    public Task<Objective?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken) =>
        dbContext.Objectives.FirstOrDefaultAsync(
            objective => objective.UserId == userId,
            cancellationToken);

    public void Add(Objective objective) => dbContext.Objectives.Add(objective);
}
