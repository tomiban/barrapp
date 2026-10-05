using System.Linq.Expressions;
using Barrapp.Domain.Athlete;

namespace Barrapp.Application.Features.AthleteProfiles;

/// <summary>
/// Proyección compartida de los máximos del perfil a <see cref="MaximumResponse"/>. La usan
/// la query (traducida por EF Core dentro de la proyección a DTO) y el handler de escritura
/// (sobre las entidades del agregado ya cargadas), para que el orden y la forma de la
/// respuesta no diverjan entre los dos caminos.
/// </summary>
internal static class MaximumResponses
{
    /// <summary>Clave de orden: el código del ejercicio.</summary>
    public static readonly Expression<Func<Maximum, string>> OrderByCode =
        maximum => maximum.ExerciseCode;

    /// <summary>Proyección de un máximo a su DTO; EF Core la traduce de forma estructural.</summary>
    public static readonly Expression<Func<Maximum, MaximumResponse>> Selector =
        maximum => new MaximumResponse(maximum.ExerciseCode, maximum.Repetitions);

    private static readonly Func<Maximum, string> OrderKey = OrderByCode.Compile();

    private static readonly Func<Maximum, MaximumResponse> Map = Selector.Compile();

    /// <summary>Proyecta una colección de máximos del dominio, ordenada por código.</summary>
    public static IReadOnlyList<MaximumResponse> From(IEnumerable<Maximum> maximums) =>
        maximums
            .OrderBy(OrderKey, StringComparer.Ordinal)
            .Select(Map)
            .ToList();
}
