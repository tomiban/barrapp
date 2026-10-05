using Barrapp.Api.Extensions;
using Barrapp.Application.Features.Ping;
using MediatR;

namespace Barrapp.Api.Endpoints;

internal static class PingEndpoints
{
    public static IEndpointRouteBuilder MapPingEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/ping", async (ISender sender, CancellationToken cancellationToken) =>
            {
                var result = await sender.Send(new PingQuery(), cancellationToken);

                return result.IsSuccess
                    ? Results.Ok(result.Value)
                    : result.Error.ToProblemDetails();
            })
            .WithName("Ping")
            .WithSummary("Comprueba que el API y su pipeline de MediatR responden.")
            .WithTags("Sistema")
            .Produces<PingResponse>();

        return app;
    }
}
