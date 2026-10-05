using Barrapp.Application.Abstractions;
using Barrapp.Domain.Athlete;
using Barrapp.Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace Barrapp.Persistence;

/// <summary>
/// Contexto de EF Core sobre SQLite. Expone las entidades para EF e implementa
/// <see cref="IUnitOfWork"/> para confirmar las escrituras; las lecturas van por los puertos de
/// lectura de Application y las escrituras, por los repositorios.
/// </summary>
public sealed class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : DbContext(options), IUnitOfWork
{
    /// <summary>Usuarios propietarios de los datos; en el MVP, uno solo.</summary>
    public DbSet<User> Users => Set<User>();

    /// <summary>Perfiles de atleta.</summary>
    public DbSet<AthleteProfile> AthleteProfiles => Set<AthleteProfile>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
    }
}
