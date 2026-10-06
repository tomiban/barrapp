using Barrapp.Application.Abstractions;

namespace Barrapp.Application.Features.Catalog;

/// <summary>
/// Devuelve el catálogo de ejercicios agrupado por patrón. Es solo lectura: la base de
/// conocimiento ya viene validada y en memoria.
/// </summary>
public sealed record GetExercisesQuery : IQuery<ExerciseCatalogResponse>;
