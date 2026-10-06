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
            .WithSummary("Devuelve el mesociclo de 4 semanas del atleta (el activo si está persistido).")
            .Produces<PlanResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status400BadRequest);

        app.MapPost(
                "/plan",
                async (ISender sender, CancellationToken cancellationToken) =>
                {
                    var result = await sender.Send(new GeneratePlanCommand(), cancellationToken);

                    return result.IsSuccess
                        ? Results.Ok(result.Value)
                        : result.Error.ToProblemDetails();
                })
            .WithName("GeneratePlan")
            .WithTags("Plan")
            .WithSummary("Genera el mesociclo con el motor y lo persiste como el activo del atleta.")
            .Produces<PlanResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status400BadRequest);

        app.MapPost(
                "/plan/close",
                async (ISender sender, CancellationToken cancellationToken) =>
                {
                    var result = await sender.Send(new CloseMesocycleCommand(), cancellationToken);

                    return result.IsSuccess
                        ? Results.Ok(result.Value)
                        : result.Error.ToProblemDetails();
                })
            .WithName("CloseMesocycle")
            .WithTags("Plan")
            .WithSummary("Cierra el mesociclo activo, ajusta los máximos con las sesiones registradas y lo publica en el historial.")
            .Produces<CloseMesocycleResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        app.MapGet(
                "/plan/history",
                async (ISender sender, CancellationToken cancellationToken) =>
                {
                    var result = await sender.Send(new GetMesocycleHistoryQuery(), cancellationToken);

                    return result.IsSuccess
                        ? Results.Ok(result.Value)
                        : result.Error.ToProblemDetails();
                })
            .WithName("GetMesocycleHistory")
            .WithTags("Plan")
            .WithSummary("Lista el historial de mesociclos cerrados, más reciente primero.")
            .Produces<IReadOnlyList<MesocycleSummaryResponse>>();

        app.MapGet(
                "/plan/history/{id:guid}",
                async (Guid id, ISender sender, CancellationToken cancellationToken) =>
                {
                    var result = await sender.Send(new GetMesocycleDetailQuery(id), cancellationToken);

                    return result.IsSuccess
                        ? Results.Ok(result.Value)
                        : result.Error.ToProblemDetails();
                })
            .WithName("GetMesocycleHistoryDetail")
            .WithTags("Plan")
            .WithSummary("Abre el detalle de un mesociclo del historial, con el plan tal y como se guardó.")
            .Produces<PlanResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        return app;
    }
}
