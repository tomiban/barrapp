using Barrapp.Application.Behaviors;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Barrapp.Application;

public static class DependencyInjection
{
    /// <summary>
    /// Registra CQRS (MediatR), validación (FluentValidation) y el pipeline de cross-cutting.
    /// Orden de fuera hacia dentro: logging → validación.
    /// </summary>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = typeof(DependencyInjection).Assembly;

        // El tiempo se inyecta (`TimeProvider`) en vez de leerse de `DateTime.UtcNow` dentro de los
        // casos de uso: el «hoy» del calendario (#94) y las ventanas de mesociclo se vuelven
        // comprobables en los tests.
        services.TryAddSingleton(TimeProvider.System);

        services.AddMediatR(configuration =>
        {
            configuration.RegisterServicesFromAssembly(assembly);
            configuration.AddOpenBehavior(typeof(LoggingBehavior<,>));
            configuration.AddOpenBehavior(typeof(ValidationBehavior<,>));
        });

        services.AddValidatorsFromAssembly(assembly, includeInternalTypes: true);

        return services;
    }
}
