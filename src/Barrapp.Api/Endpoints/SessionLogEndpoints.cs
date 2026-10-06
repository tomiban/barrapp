using Barrapp.Api.Extensions;
using Barrapp.Application.Features.SessionLogs;
using Barrapp.Domain.Sessions;
using MediatR;

namespace Barrapp.Api.Endpoints;

internal static class SessionLogEndpoints
{
    public static IEndpointRouteBuilder MapSessionLogEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/session-logs").WithTags("Registro");

        group.MapGet(
                "/",
                async (ISender sender, CancellationToken cancellationToken) =>
                {
                    var result = await sender.Send(new GetSessionLogsQuery(), cancellationToken);

                    return result.IsSuccess
                        ? Results.Ok(result.Value)
                        : result.Error.ToProblemDetails();
                })
            .WithName("GetSessionLogs")
            .WithSummary("Lista los registros de sesión del atleta, serie a serie.")
            .Produces<IReadOnlyList<SessionLogResponse>>();

        group.MapPost(
                "/",
                async (RegisterSessionLogBody body, ISender sender, CancellationToken cancellationToken) =>
                {
                    var result = await sender.Send(
                        new RegisterSessionLogCommand(
                            body.ExerciseId,
                            body.MesocycleId,
                            body.SessionDay,
                            body.Sets
                                .Select(set => new SessionLogSetInput(set.SetNumber, set.Value, set.Effort))
                                .ToList(),
                            body.ClientId),
                        cancellationToken);

                    return result.IsSuccess
                        ? Results.Ok(result.Value)
                        : result.Error.ToProblemDetails();
                })
            .WithName("RegisterSessionLog")
            .WithSummary("Registra lo ejecutado, serie a serie, en un ejercicio de una sesión.")
            .Produces<SessionLogResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest);

        group.MapPut(
                "/{id:guid}",
                async (Guid id, UpdateSessionLogBody body, ISender sender, CancellationToken cancellationToken) =>
                {
                    var result = await sender.Send(
                        new UpdateSessionLogCommand(
                            id,
                            body.Sets
                                .Select(set => new SessionLogSetInput(set.SetNumber, set.Value, set.Effort))
                                .ToList()),
                        cancellationToken);

                    return result.IsSuccess
                        ? Results.Ok(result.Value)
                        : result.Error.ToProblemDetails();
                })
            .WithName("UpdateSessionLog")
            .WithSummary("Edita un registro de sesión: sustituye los valores de sus series.")
            .Produces<SessionLogResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapDelete(
                "/{id:guid}",
                async (Guid id, ISender sender, CancellationToken cancellationToken) =>
                {
                    var result = await sender.Send(new DeleteSessionLogCommand(id), cancellationToken);

                    return result.IsSuccess
                        ? Results.NoContent()
                        : result.Error.ToProblemDetails();
                })
            .WithName("DeleteSessionLog")
            .WithSummary("Elimina un registro de sesión y sus series.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound);

        return app;
    }
}

/// <summary>Cuerpo del <c>POST</c>: el ejercicio, la sesión y las series ejecutadas.</summary>
/// <param name="ExerciseId">Identificador del ejercicio registrado en el catálogo.</param>
/// <param name="MesocycleId">Mesociclo de la sesión, si el plan ya está persistido (ticket #27).</param>
/// <param name="SessionDay">Día de la sesión dentro del mesociclo, desde 1.</param>
/// <param name="Sets">Series ejecutadas, numeradas desde 1 y en orden.</param>
/// <param name="ClientId">Id idempotente de la outbox offline del cliente (ticket #26); opcional.</param>
internal sealed record RegisterSessionLogBody(
    string ExerciseId,
    Guid? MesocycleId,
    int SessionDay,
    IReadOnlyList<RegisterSessionLogSetBody> Sets,
    Guid? ClientId = null);

/// <summary>Una serie del cuerpo de registro.</summary>
/// <param name="SetNumber">Número de orden de la serie, desde 1.</param>
/// <param name="Value">Valor real ejecutado: reps o segundos según el ejercicio.</param>
/// <param name="Effort">Esfuerzo real (RIR/RPE), entre 0 y 10; opcional.</param>
internal sealed record RegisterSessionLogSetBody(int SetNumber, int Value, int? Effort);

/// <summary>Cuerpo del <c>PUT</c>: solo las series nuevas; la identidad de la sesión no cambia.</summary>
/// <param name="Sets">Series ejecutadas, numeradas desde 1 y en orden.</param>
internal sealed record UpdateSessionLogBody(IReadOnlyList<RegisterSessionLogSetBody> Sets);
