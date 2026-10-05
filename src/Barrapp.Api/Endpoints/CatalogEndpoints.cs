using Barrapp.Api.Extensions;
using Barrapp.Application.Features.Catalog;
using MediatR;

namespace Barrapp.Api.Endpoints;

internal static class CatalogEndpoints
{
    public static IEndpointRouteBuilder MapCatalogEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/catalog").WithTags("Catálogo");

        group.MapGet(
                "/exercises",
                async (ISender sender, CancellationToken cancellationToken) =>
                {
                    var result = await sender.Send(new GetExercisesQuery(), cancellationToken);

                    return result.IsSuccess
                        ? Results.Ok(result.Value)
                        : result.Error.ToProblemDetails();
                })
            .WithName("GetExercises")
            .WithSummary("Devuelve el catálogo de ejercicios agrupado por patrón.")
            .Produces<ExerciseCatalogResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapGet(
                "/skills",
                async (ISender sender, CancellationToken cancellationToken) =>
                {
                    var result = await sender.Send(new GetSkillsQuery(), cancellationToken);

                    return result.IsSuccess
                        ? Results.Ok(result.Value)
                        : result.Error.ToProblemDetails();
                })
            .WithName("GetSkills")
            .WithSummary("Devuelve los skills con su escalera de progresión y sus rutinas de patrón.")
            .Produces<IReadOnlyList<SkillResponse>>();

        return app;
    }
}
