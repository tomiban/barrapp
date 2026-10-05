using Barrapp.Application.Features.AthleteProfiles;
using Microsoft.EntityFrameworkCore;

namespace Barrapp.Persistence.Queries;

/// <summary>
/// Implementación EF Core del puerto de lectura del perfil: proyecta la entidad al DTO en la
/// propia consulta, sin materializarla. La escritura sigue yendo por el repositorio y la unidad
/// de trabajo.
/// </summary>
internal sealed class AthleteProfileReadService(ApplicationDbContext dbContext)
    : IAthleteProfileReadService
{
    public Task<AthleteProfileResponse?> GetAsync(Guid userId, CancellationToken cancellationToken) =>
        dbContext.AthleteProfiles
            .Where(profile => profile.UserId == userId)
            .Select(profile => new AthleteProfileResponse(
                profile.WeightKilograms,
                profile.HeightCentimeters))
            .FirstOrDefaultAsync(cancellationToken);
}
