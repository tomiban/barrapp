using Barrapp.Application.Abstractions;
using Barrapp.Persistence.Knowledge;
using Barrapp.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Barrapp.Persistence;

public static class DependencyInjection
{
    /// <summary>
    /// Registra el acceso a datos: EF Core sobre SQLite, los adaptadores de los puertos de
    /// Application (repositorios, <see cref="IApplicationDbContext"/> y <see cref="IUnitOfWork"/>)
    /// y el catálogo de la base de conocimiento como singleton.
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
        services.AddScoped<IObjectiveRepository, ObjectiveRepository>();
        services.AddScoped<IAthleteSkillProgressRepository, AthleteSkillProgressRepository>();
        services.AddScoped<IApplicationDbContext>(provider =>
            provider.GetRequiredService<ApplicationDbContext>());
        services.AddScoped<IUnitOfWork>(provider =>
            provider.GetRequiredService<ApplicationDbContext>());

        // Catálogo de conocimiento: se carga y valida una sola vez (fail-fast).
        services.AddSingleton<IKnowledgeBase>(provider =>
            new KnowledgeBaseCatalog(
                new KnowledgeBaseLoader(provider.GetRequiredService<ILogger<KnowledgeBaseLoader>>())
                    .LoadEmbeddedResources()));

        return services;
    }
}
