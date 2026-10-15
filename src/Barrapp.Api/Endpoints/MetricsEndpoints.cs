using Barrapp.Api.Extensions;
using Barrapp.Application.Features.Metrics;
using MediatR;

namespace Barrapp.Api.Endpoints;

internal static class MetricsEndpoints
{
    public static IEndpointRouteBuilder MapMetricsEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet(
                "/metrics",
                async (ISender sender, int? microcycle, CancellationToken cancellationToken) =>
                {
                    var result = await sender.Send(
                        new GetMetricsQuery(microcycle),
                        cancellationToken);

                    return result.IsSuccess
                        ? Results.Ok(result.Value)
                        : result.Error.ToProblemDetails();
                })
            .WithName("GetMetrics")
            .WithTags("Metrics")
            .WithSummary("Devuelve las métricas derivadas del mesociclo activo: adherencia y volumen semanal.")
            .WithDescription(
                "Adherencia = sesiones de mesociclo completadas / programadas hasta hoy; las sueltas no cuentan. " +
                "Volumen semanal = series por patrón (empuje/tirón/pierna), prescrito por el motor y ejecutado " +
                "según los registros. El microciclo se puede indicar con `?microcycle=n`; si no, es el en curso.")
            .Produces<MetricsResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status400BadRequest);

        return app;
    }
}
