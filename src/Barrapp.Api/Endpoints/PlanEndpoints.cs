using Barrapp.Api.Extensions;
using Barrapp.Application.Features.Plans;
using MediatR;

namespace Barrapp.Api.Endpoints;

internal static class PlanEndpoints
{
    public static IEndpointRouteBuilder MapPlanEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet(
                "/plan",
                async (ISender sender, CancellationToken cancellationToken) =>
                {
                    var result = await sender.Send(new GetPlanQuery(), cancellationToken);

                    return result.IsSuccess
                        ? Results.Ok(result.Value)
                        : result.Error.ToProblemDetails();
                })
            .WithName("GetPlan")
            .WithTags("Plan")
            .WithSummary("Genera y devuelve el mesociclo de 4 semanas del atleta.")
            .Produces<PlanResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status400BadRequest);

        return app;
    }
}
