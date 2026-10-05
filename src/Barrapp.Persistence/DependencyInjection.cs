using Barrapp.Application.Abstractions;
using Barrapp.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Barrapp.Persistence;

public static class DependencyInjection
{
    /// <summary>
    /// Registra el acceso a datos: EF Core sobre SQLite y los adaptadores de los puertos de
    /// Application (repositorios, <see cref="IApplicationDbContext"/> y <see cref="IUnitOfWork"/>).
    /// La conexión sale de configuración, nunca del código (ver <c>docs/engineering-standards.md</c>).
    /// </summary>
    public static IServiceCollection AddPersistence(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException("Falta la cadena de conexión 'Default'.");

        services.AddDbContext<ApplicationDbContext>(options => options.UseSqlite(connectionString));

        services.AddScoped<IAthleteProfileRepository, AthleteProfileRepository>();
        services.AddScoped<IApplicationDbContext>(provider =>
            provider.GetRequiredService<ApplicationDbContext>());
        services.AddScoped<IUnitOfWork>(provider =>
            provider.GetRequiredService<ApplicationDbContext>());

        return services;
    }
}
