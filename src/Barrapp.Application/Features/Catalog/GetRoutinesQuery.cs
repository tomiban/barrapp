using Barrapp.Application.Abstractions;

namespace Barrapp.Application.Features.Catalog;

/// <summary>
/// Devuelve los programas generales de acondicionamiento con sus rutinas y bloques. Es solo
/// lectura: la base de conocimiento ya viene validada y en memoria.
/// </summary>
public sealed record GetRoutinesQuery : IQuery<IReadOnlyList<RoutineProgramResponse>>;
