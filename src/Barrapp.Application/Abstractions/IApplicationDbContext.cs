using Barrapp.Domain.Athlete;

namespace Barrapp.Application.Abstractions;

/// <summary>
/// Acceso de solo lectura a la base de datos para las <i>queries</i>, que proyectan
/// directamente a DTO. Las escrituras pasan por repositorios y <see cref="IUnitOfWork"/>.
/// </summary>
public interface IApplicationDbContext
{
    /// <summary>Perfiles de atleta.</summary>
    IQueryable<AthleteProfile> AthleteProfiles { get; }
}
