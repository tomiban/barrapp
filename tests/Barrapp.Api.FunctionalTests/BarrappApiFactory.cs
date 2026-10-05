using Barrapp.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Barrapp.Api.FunctionalTests;

/// <summary>
/// Arranca el API real sobre una base SQLite efímera y propia de cada clase de test, en lugar de
/// la base en fichero que usa el desarrollo. Así los tests funcionales son herméticos: no dependen
/// del estado que dejó otra ejecución (o la API local) ni dejan rastro al terminar.
/// </summary>
public sealed class BarrappApiFactory : WebApplicationFactory<Program>
{
    private readonly string _databasePath =
        Path.Combine(Path.GetTempPath(), $"barrapp-functional-{Guid.NewGuid():N}.db");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(services =>
        {
            var descriptor = services.SingleOrDefault(
                service => service.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));

            if (descriptor is not null)
            {
                services.Remove(descriptor);
            }

            services.AddDbContext<ApplicationDbContext>(
                options => options.UseSqlite($"Data Source={_databasePath}"));
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        TryDelete(_databasePath);
        TryDelete($"{_databasePath}-shm");
        TryDelete($"{_databasePath}-wal");
    }

    private static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch (IOException)
        {
            // La limpieza de un fichero temporal no debe romper el resultado del test.
        }
    }
}
