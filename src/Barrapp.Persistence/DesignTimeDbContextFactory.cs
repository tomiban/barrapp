using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Barrapp.Persistence;

/// <summary>
/// Fábrica de tiempo de diseño para que <c>dotnet ef migrations</c> pueda construir el contexto
/// sin arrancar el API. En tiempo de diseño no hay configuración de la app; se usa una base
/// SQLite local solo para generar el SQL de la migración.
/// </summary>
internal sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite("Data Source=barrapp.db")
            .Options;

        return new ApplicationDbContext(options);
    }
}
