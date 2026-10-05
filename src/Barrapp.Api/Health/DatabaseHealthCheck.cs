using Barrapp.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Barrapp.Api.Health;

/// <summary>
/// Comprueba que la base de datos acepta conexiones sin depender de que el esquema tenga tablas:
/// abre y cierra la conexión. A diferencia de <c>CanConnect</c>, es correcto aunque aún no haya migraciones.
/// </summary>
internal sealed class DatabaseHealthCheck(ApplicationDbContext dbContext) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await dbContext.Database.OpenConnectionAsync(cancellationToken);
            await dbContext.Database.CloseConnectionAsync();

            return HealthCheckResult.Healthy("La base de datos responde.");
        }
        catch (Exception exception)
        {
            return HealthCheckResult.Unhealthy("La base de datos no responde.", exception);
        }
    }
}
