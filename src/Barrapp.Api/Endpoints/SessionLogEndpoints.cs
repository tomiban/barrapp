using Barrapp.Api.Extensions;
using Barrapp.Application.Features.SessionLogs;
using Barrapp.Domain.Common;
using Barrapp.Domain.Knowledge;
using Barrapp.Domain.Planning;
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
            .WithSummary("Lista los registros de sesión del atleta, de la fecha más reciente a la más antigua, con la foto de cada ítem.")
            .Produces<IReadOnlyList<SessionLogResponse>>();

        group.MapGet(
                "/{id:guid}",
                async (Guid id, ISender sender, CancellationToken cancellationToken) =>
                {
                    var result = await sender.Send(new GetSessionLogQuery(id), cancellationToken);

                    return result.IsSuccess
                        ? Results.Ok(result.Value)
                        : result.Error.ToProblemDetails();
                })
            .WithName("GetSessionLog")
            .WithSummary("Abre un registro de sesión con sus ítems y la foto de cada uno.")
            .Produces<SessionLogResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPost(
                "/",
                async (RegisterSessionLogBody body, ISender sender, CancellationToken cancellationToken) =>
                {
                    // Los códigos de cable (tipo de sesión, papel y patrón) se traducen aquí; un
                    // código desconocido se responde con su propio problema, no con una excepción.
                    if (ParseKind(body.Session.Kind) is not { } kind)
                    {
                        return Result.Failure<SessionLogResponse>(
                            DomainErrors.SessionLog.KindOutOfRange).Error.ToProblemDetails();
                    }

                    if (ParseRole(body.Item.Role) is not { } role)
                    {
                        return Result.Failure<SessionLogResponse>(
                            DomainErrors.SessionLog.RoleOutOfRange).Error.ToProblemDetails();
                    }

                    var result = await sender.Send(
                        new RegisterSessionLogCommand(
                            new SessionLogKeyInput(
                                kind,
                                body.Session.Date,
                                body.Session.MesocycleId,
                                body.Session.MicrocycleNumber,
                                body.Session.SessionDay),
                            new SessionLogItemBody(
                                body.Item.ExerciseId,
                                role,
                                ParsePattern(body.Item.Pattern),
                                body.Item.PrescribedSets,
                                body.Item.RepsMin,
                                body.Item.RepsMax,
                                body.Item.HoldSecondsMin,
                                body.Item.HoldSecondsMax,
                                body.Item.Note,
                                ToSetInputs(body.Item.Sets)),
                            body.ClientId),
                        cancellationToken);

                    return result.IsSuccess
                        ? Results.Ok(result.Value)
                        : result.Error.ToProblemDetails();
                })
            .WithName("RegisterSessionLog")
            .WithSummary("Registra lo ejecutado de un ejercicio de una sesión, serie a serie, creando la sesión si es la primera vez.")
            .Produces<SessionLogResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest);

        group.MapPut(
                "/{id:guid}/items/{itemId:guid}",
                async (
                    Guid id,
                    Guid itemId,
                    UpdateSessionLogItemBody body,
                    ISender sender,
                    CancellationToken cancellationToken) =>
                {
                    var result = await sender.Send(
                        new UpdateSessionLogItemCommand(
                            id,
                            itemId,
                            ToSetInputs(body.Sets)),
                        cancellationToken);

                    return result.IsSuccess
                        ? Results.Ok(result.Value)
                        : result.Error.ToProblemDetails();
                })
            .WithName("UpdateSessionLogItem")
            .WithSummary("Edita un ítem registrado: sustituye los valores de sus series.")
            .Produces<SessionLogItemResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapDelete(
                "/{id:guid}/items/{itemId:guid}",
                async (Guid id, Guid itemId, ISender sender, CancellationToken cancellationToken) =>
                {
                    var result = await sender.Send(
                        new DeleteSessionLogItemCommand(id, itemId),
                        cancellationToken);

                    return result.IsSuccess
                        ? Results.NoContent()
                        : result.Error.ToProblemDetails();
                })
            .WithName("DeleteSessionLogItem")
            .WithSummary("Borra un ítem registrado y des-completa la sesión; si era el último, borra la sesión.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPost(
                "/{id:guid}/complete",
                async (Guid id, ISender sender, CancellationToken cancellationToken) =>
                {
                    var result = await sender.Send(
                        new SetSessionLogCompletionCommand(id, Completed: true),
                        cancellationToken);

                    return result.IsSuccess
                        ? Results.Ok(result.Value)
                        : result.Error.ToProblemDetails();
                })
            .WithName("CompleteSessionLog")
            .WithSummary("Marca la sesión registrada como completada (spec 0001, US-23).")
            .Produces<SessionLogResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapDelete(
                "/{id:guid}/complete",
                async (Guid id, ISender sender, CancellationToken cancellationToken) =>
                {
                    var result = await sender.Send(
                        new SetSessionLogCompletionCommand(id, Completed: false),
                        cancellationToken);

                    return result.IsSuccess
                        ? Results.Ok(result.Value)
                        : result.Error.ToProblemDetails();
                })
            .WithName("UncompleteSessionLog")
            .WithSummary("Des-completa la sesión registrada sin perder sus ítems.")
            .Produces<SessionLogResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        return app;
    }

    /// <summary>Traduce las series del cuerpo a la entrada del dominio.</summary>
    private static List<SessionLogSetInput> ToSetInputs(IReadOnlyList<SessionSetBody> sets) => sets
        .Select(set => new SessionLogSetInput(set.SetNumber, set.Value, set.ActualRir, set.LoadKg))
        .ToList();

    /// <summary>
    /// Traduce el código de cable del origen de la sesión al enum del dominio; un valor desconocido
    /// devuelve <c>null</c> para que el endpoint lo responda con su propio problema.
    /// </summary>
    private static SessionLogKind? ParseKind(string? kind) => kind switch
    {
        "mesocycle" => SessionLogKind.Mesocycle,
        "suelta" => SessionLogKind.Suelta,
        _ => null,
    };

    /// <summary>Traduce el código de cable del papel de la fila al enum del dominio.</summary>
    private static SessionItemRole? ParseRole(string? role) => role switch
    {
        "skill" => SessionItemRole.Skill,
        "strength" => SessionItemRole.Strength,
        "core" => SessionItemRole.Core,
        _ => null,
    };

    /// <summary>Traduce el código de cable del patrón de fuerza al enum del dominio.</summary>
    private static ExerciseGroup? ParsePattern(string? pattern) => pattern switch
    {
        "push" => ExerciseGroup.Push,
        "pull" => ExerciseGroup.Pull,
        "leg" => ExerciseGroup.Leg,
        "core" => ExerciseGroup.Core,
        "cardio" => ExerciseGroup.Cardio,
        _ => null,
    };
}

/// <summary>
/// Cuerpo del <c>POST</c>: la clave de sesión determinista de ADR-0014 y el ítem a registrar con su
/// foto (papel y objetivo) y sus series.
/// </summary>
/// <param name="Session">Qué sesión se está anotando: tipo, fecha, mesociclo, microciclo y día.</param>
/// <param name="Item">El ejercicio, su objetivo prescrito y las series realmente ejecutadas.</param>
/// <param name="ClientId">Id idempotente de la outbox offline del cliente (ADR-0003); opcional.</param>
internal sealed record RegisterSessionLogBody(
    SessionKeyBody Session,
    SessionItemBody Item,
    Guid? ClientId = null);

/// <summary>
/// Clave de sesión determinista del registro (ADR-0014). Para <c>kind: "mesocycle"</c> van mesociclo,
/// microciclo y día; para <c>kind: "suelta"</c> ninguno de los tres.
/// </summary>
/// <param name="Kind"><c>mesocycle</c> o <c>suelta</c>.</param>
/// <param name="Date">Fecha de la sesión (ISO 8601, <c>AAAA-MM-DD</c>).</param>
/// <param name="MesocycleId">Mesociclo de la sesión; obligatorio en una sesión de mesociclo.</param>
/// <param name="MicrocycleNumber">Microciclo (1–4) de la sesión; obligatorio en una de mesociclo.</param>
/// <param name="SessionDay">Día de la sesión en el microciclo; obligatorio en una de mesociclo.</param>
internal sealed record SessionKeyBody(
    string Kind,
    DateOnly Date,
    Guid? MesocycleId = null,
    int? MicrocycleNumber = null,
    int? SessionDay = null);

/// <summary>Un ítem del cuerpo de registro: ejercicio, objetivo prescrito y series ejecutadas.</summary>
internal sealed record SessionItemBody(
    string ExerciseId,
    string Role,
    string? Pattern,
    int PrescribedSets,
    int? RepsMin,
    int? RepsMax,
    int? HoldSecondsMin,
    int? HoldSecondsMax,
    string? Note,
    IReadOnlyList<SessionSetBody> Sets);

/// <summary>Una serie del cuerpo: valor real con su unidad del ejercicio, RIR real y lastre.</summary>
/// <param name="SetNumber">Número de orden de la serie, desde 1.</param>
/// <param name="Value">Valor real ejecutado: repeticiones o segundos según el ejercicio.</param>
/// <param name="ActualRir">RIR real de la serie (0–10); opcional.</param>
/// <param name="LoadKg">Lastre en kg de la serie; opcional.</param>
internal sealed record SessionSetBody(int SetNumber, int Value, int? ActualRir = null, double? LoadKg = null);

/// <summary>Cuerpo del <c>PUT</c>: solo las series nuevas; la cabecera y la foto no cambian.</summary>
/// <param name="Sets">Series ejecutadas, numeradas desde 1 y en orden.</param>
internal sealed record UpdateSessionLogItemBody(IReadOnlyList<SessionSetBody> Sets);
