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

    /// <summary>
    /// Materializa de forma asíncrona la primera fila de <paramref name="query"/> (o
    /// <c>null</c>). Permite que la proyección viva en Application sin que esta capa dependa de
    /// EF Core: la implementación (Persistence) usa el proveedor asíncrono de EF.
    /// </summary>
    Task<TResponse?> FirstOrDefaultAsync<TResponse>(
        IQueryable<TResponse> query,
        CancellationToken cancellationToken)
        where TResponse : class;
}
