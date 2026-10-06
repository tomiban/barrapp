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

        app.MapGet(
                "/sessions/suelta",
                async (ISender sender, CancellationToken cancellationToken) =>
                {
                    var result = await sender.Send(new GetSueltaHistoryQuery(), cancellationToken);

                    return result.IsSuccess
                        ? Results.Ok(result.Value)
                        : result.Error.ToProblemDetails();
                })
            .WithName("GetSueltaHistory")
            .WithTags("Sesión suelta")
            .WithSummary("Lista el historial de sesiones sueltas, de la más reciente a la más antigua.")
            .Produces<IReadOnlyList<SessionSueltaResponse>>();

        app.MapPost(
                "/sessions/suelta/{id:guid}/registrar",
                async (Guid id, ISender sender, CancellationToken cancellationToken) =>
                {
                    var result = await sender.Send(new RecordSoloSessionCommand(id), cancellationToken);

                    return result.IsSuccess
                        ? Results.Ok(result.Value)
                        : result.Error.ToProblemDetails();
                })
            .WithName("RecordSoloSession")
            .WithTags("Sesión suelta")
            .WithSummary("Marca una sesión suelta generada como registrada, sin tocar el plan ni los máximos.")
            .Produces<SessionSueltaResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        return app;
    }
}