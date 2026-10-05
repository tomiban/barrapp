using Microsoft.Extensions.DependencyInjection;

namespace Barrapp.Infrastructure;

public static class DependencyInjection
{
    /// <summary>
    /// Registra los servicios externos (tiempo, email, caché, auth). Hoy no hay ninguno.
    /// </summary>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        return services;
    }
}
