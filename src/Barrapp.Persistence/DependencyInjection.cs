using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Barrapp.Persistence;

public static class DependencyInjection
{
    /// <summary>
    /// Registra el acceso a datos: EF Core sobre SQLite. La conexión sale de configuración,
    /// nunca del código (ver <c>docs/engineering-standards.md</c>).
    /// </summary>
    public static IServiceCollection AddPersistence(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException("Falta la cadena de conexión 'Default'.");

        services.AddDbContext<ApplicationDbContext>(options => options.UseSqlite(connectionString));

        return services;
    }
}
