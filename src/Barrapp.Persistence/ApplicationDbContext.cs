using Barrapp.Application.Abstractions;
using Barrapp.Domain.Athlete;
using Barrapp.Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace Barrapp.Persistence;

/// <summary>
/// Contexto de EF Core sobre SQLite. Además de exponer las entidades para EF, implementa los
/// puertos de Application: <see cref="IApplicationDbContext"/> para las queries y
/// <see cref="IUnitOfWork"/> para confirmar las escrituras.
/// </summary>
public sealed class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : DbContext(options), IApplicationDbContext, IUnitOfWork
{
    /// <summary>Usuarios propietarios de los datos; en el MVP, uno solo.</summary>
    public DbSet<User> Users => Set<User>();

    /// <summary>Perfiles de atleta.</summary>
    public DbSet<AthleteProfile> AthleteProfiles => Set<AthleteProfile>();

    IQueryable<AthleteProfile> IApplicationDbContext.AthleteProfiles => AthleteProfiles;

    Task<TResponse?> IApplicationDbContext.FirstOrDefaultAsync<TResponse>(
        IQueryable<TResponse> query,
        CancellationToken cancellationToken)
        where TResponse : class =>
        EntityFrameworkQueryableExtensions.FirstOrDefaultAsync(query, cancellationToken);

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
    }
}
