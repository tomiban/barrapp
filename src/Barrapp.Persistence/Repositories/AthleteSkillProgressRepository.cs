using Barrapp.Application.Abstractions;
using Barrapp.Domain.SkillProgress;
using Microsoft.EntityFrameworkCore;

namespace Barrapp.Persistence.Repositories;

/// <summary>
/// Implementación EF Core del puerto <see cref="IAthleteSkillProgressRepository"/>. No guarda: la
/// escritura la confirma el <see cref="IUnitOfWork"/> al cerrar el caso de uso.
/// </summary>
internal sealed class AthleteSkillProgressRepository(ApplicationDbContext dbContext)
    : IAthleteSkillProgressRepository
{
    public Task<AthleteSkillProgress?> GetAsync(
        Guid userId,
        string skillId,
        CancellationToken cancellationToken) =>
        dbContext.AthleteSkillProgresses.FirstOrDefaultAsync(
            progress => progress.UserId == userId && progress.SkillId == skillId,
            cancellationToken);

    public void Add(AthleteSkillProgress progress) => dbContext.AthleteSkillProgresses.Add(progress);
}
