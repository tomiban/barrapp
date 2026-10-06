using Barrapp.Api.Extensions;
using Barrapp.Application.Features.Objectives;
using MediatR;

namespace Barrapp.Api.Endpoints;

internal static class ObjectiveEndpoints
{
    public static IEndpointRouteBuilder MapObjectiveEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/profile/objective").WithTags("Perfil");

        group.MapGet(
                "/",
                async (ISender sender, CancellationToken cancellationToken) =>
                {
                    var result = await sender.Send(new GetObjectiveQuery(), cancellationToken);

                    return result.IsSuccess
                        ? Results.Ok(result.Value)
                        : result.Error.ToProblemDetails();
                })
            .WithName("GetObjective")
            .WithSummary("Devuelve el skill objetivo del mesociclo.")
            .Produces<ObjectiveResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPut(
                "/",
                async (
                    SetObjectiveCommand command,
                    ISender sender,
                    CancellationToken cancellationToken) =>
                {
                    var result = await sender.Send(command, cancellationToken);

                    return result.IsSuccess
                        ? Results.Ok(result.Value)
                        : result.Error.ToProblemDetails();
                })
            .WithName("SetObjective")
            .WithSummary("Fija el skill objetivo del mesociclo.")
            .Produces<ObjectiveResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest);

        return app;
    }
}
