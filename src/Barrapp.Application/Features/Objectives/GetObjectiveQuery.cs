using Barrapp.Application.Abstractions;

namespace Barrapp.Application.Features.Objectives;

/// <summary>
/// Devuelve el objetivo del mesociclo. Falla con <c>Not Found</c> si todavía no se ha elegido.
/// </summary>
public sealed record GetObjectiveQuery : IQuery<ObjectiveResponse>;
