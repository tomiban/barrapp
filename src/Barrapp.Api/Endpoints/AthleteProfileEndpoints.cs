using Barrapp.Api.Extensions;
using Barrapp.Application.Features.AthleteProfiles;
using MediatR;

namespace Barrapp.Api.Endpoints;

internal static class AthleteProfileEndpoints
{
    public static IEndpointRouteBuilder MapAthleteProfileEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/profile").WithTags("Perfil");

        group.MapGet("/", async (ISender sender, CancellationToken cancellationToken) =>
            {
                var result = await sender.Send(new GetAthleteProfileQuery(), cancellationToken);

                return result.IsSuccess
                    ? Results.Ok(result.Value)
                    : result.Error.ToProblemDetails();
            })
            .WithName("GetAthleteProfile")
            .WithSummary("Devuelve el perfil del atleta (peso, altura y días de entrenamiento).")
            .Produces<AthleteProfileResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPut(
                "/",
                async (
                    SaveAthleteProfileCommand command,
                    ISender sender,
                    CancellationToken cancellationToken) =>
                {
                    var result = await sender.Send(command, cancellationToken);

                    return result.IsSuccess
                        ? Results.Ok(result.Value)
                        : result.Error.ToProblemDetails();
                })
            .WithName("SaveAthleteProfile")
            .WithSummary("Crea o actualiza el perfil del atleta (peso, altura y días de entrenamiento).")
            .Produces<AthleteProfileResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest);

        return app;
    }
}
