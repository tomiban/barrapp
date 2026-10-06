using Barrapp.Api.Extensions;
using Barrapp.Application.Features.SkillProgress;
using MediatR;

namespace Barrapp.Api.Endpoints;

internal static class SkillProgressEndpoints
{
    public static IEndpointRouteBuilder MapSkillProgressEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/catalog/progress").WithTags("Catálogo");

        group.MapGet(
                "/",
                async (ISender sender, CancellationToken cancellationToken) =>
                {
                    var result = await sender.Send(new GetSkillProgressQuery(), cancellationToken);

                    return result.IsSuccess
                        ? Results.Ok(result.Value)
                        : result.Error.ToProblemDetails();
                })
            .WithName("GetSkillProgress")
            .WithSummary("Devuelve la etapa actual del atleta en cada skill.")
            .Produces<IReadOnlyList<SkillProgressResponse>>();

        group.MapPut(
                "/{skillId}",
                async (
                    string skillId,
                    SetSkillProgressBody body,
                    ISender sender,
                    CancellationToken cancellationToken) =>
                {
                    var result = await sender.Send(
                        new SetSkillProgressCommand(skillId, body.StageOrder),
                        cancellationToken);

                    return result.IsSuccess
                        ? Results.Ok(result.Value)
                        : result.Error.ToProblemDetails();
                })
            .WithName("SetSkillProgress")
            .WithSummary("Fija la etapa actual del atleta en un skill.")
            .Produces<SkillProgressResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest);

        return app;
    }
}

/// <summary>Cuerpo del <c>PUT</c>: la etapa que se quiere fijar; el skill va en la ruta.</summary>
/// <param name="StageOrder">Orden de la etapa dentro de la escalera del skill.</param>
internal sealed record SetSkillProgressBody(int StageOrder);
