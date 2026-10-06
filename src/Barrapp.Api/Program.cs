using Barrapp.Api.Endpoints;
using Barrapp.Api.Exceptions;
using Barrapp.Api.Health;
using Barrapp.Api.Http;
using Barrapp.Application;
using Barrapp.Application.Abstractions;
using Barrapp.Infrastructure;
using Barrapp.Persistence;
using Microsoft.EntityFrameworkCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext());

builder.Services.AddApplication();
builder.Services.AddInfrastructure();
builder.Services.AddPersistence(builder.Configuration);

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddOpenApi();
builder.Services
    .AddHealthChecks()
    .AddCheck<DatabaseHealthCheck>("database");

var app = builder.Build();

// Fail-fast de la base de conocimiento: si los JSON embebidos son inválidos, no arrancamos.
_ = app.Services.GetRequiredService<IKnowledgeBase>();

if (Program.ShouldApplyMigrations(builder.Configuration))
{
    await Program.ApplyMigrationsAsync(app);
}

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseExceptionHandler();
app.UseSerilogRequestLogging();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapPingEndpoints();
app.MapAthleteProfileEndpoints();
app.MapObjectiveEndpoints();
app.MapPlanEndpoints();
app.MapCatalogEndpoints();
app.MapSkillProgressEndpoints();
app.MapHealthChecks("/health");

app.Run();

/// <summary>Marca la clase de entrada como pública para los tests de integración.</summary>
public partial class Program
{
    /// <summary>
    /// Indica si se deben aplicar migraciones al arrancar la API. El valor puede configurarse desde
    /// appsettings.json o por variable de entorno; por defecto se mantiene el comportamiento actual del MVP.
    /// </summary>
    public static bool ShouldApplyMigrations(IConfiguration configuration)
    {
        return configuration.GetValue<bool?>("Database:ApplyMigrationsOnStartup") ?? true;
    }

    /// <summary>
    /// Aplica las migraciones pendientes al arrancar. En el MVP (SQLite local y un solo contenedor)
    /// es la forma más simple de garantizar el esquema; en despliegues con varias réplicas conviene
    /// separarlo del arranque.
    /// </summary>
    internal static async Task ApplyMigrationsAsync(WebApplication app)
    {
        await using var scope = app.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await dbContext.Database.MigrateAsync();
    }
}
