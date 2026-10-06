using Barrapp.Api.Extensions;
using Barrapp.Application.Features.SoloSessions;
using MediatR;

namespace Barrapp.Api.Endpoints;

internal static class SoloSessionEndpoints
{
    public static IEndpointRouteBuilder MapSoloSessionEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost(
                "/sessions/suelta",
                async (
                    GenerateSoloSessionCommand command,
                    ISender sender,
                    CancellationToken cancellationToken) =>
                {
                    var result = await sender.Send(command, cancellationToken);

                    return result.IsSuccess
                        ? Results.Ok(result.Value)
                        : result.Error.ToProblemDetails();
                })
            .WithName("GenerateSoloSession")
            .WithTags("Sesión suelta")
            .WithSummary("Genera una sesión suelta con tiempo, energía y foco.")
            .Produces<SoloSessionResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status400BadRequest);

        return app;
    }
}
