using Barrapp.Api.Endpoints;
using Barrapp.Api.Exceptions;
using Barrapp.Api.Health;
using Barrapp.Api.Http;
using Barrapp.Application;
using Barrapp.Infrastructure;
using Barrapp.Persistence;
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

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseExceptionHandler();
app.UseSerilogRequestLogging();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapPingEndpoints();
app.MapHealthChecks("/health");

app.Run();

/// <summary>Marca la clase de entrada como pública para los tests de integración.</summary>
public partial class Program;
