using Barrapp.Application.Abstractions;
using Barrapp.Domain.Athlete;
using Microsoft.EntityFrameworkCore;

namespace Barrapp.Persistence.Repositories;

/// <summary>
/// Implementación EF Core del puerto <see cref="IAthleteProfileRepository"/>. No guarda: la
/// escritura la confirma el <see cref="IUnitOfWork"/> al cerrar el caso de uso.
/// </summary>
internal sealed class AthleteProfileRepository(ApplicationDbContext dbContext)
    : IAthleteProfileRepository
{
    public Task<AthleteProfile?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken) =>
        dbContext.AthleteProfiles.FirstOrDefaultAsync(
            profile => profile.UserId == userId,
            cancellationToken);

    public void Add(AthleteProfile profile) => dbContext.AthleteProfiles.Add(profile);
}
