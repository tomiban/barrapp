using Microsoft.EntityFrameworkCore;

namespace Barrapp.Persistence;

/// <summary>
/// Contexto de EF Core sobre SQLite. Las entidades y sus configuraciones llegan con el modelo de datos
/// (ticket #3 en adelante).
/// </summary>
public sealed class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
    }
}
