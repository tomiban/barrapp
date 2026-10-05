namespace Barrapp.Domain.Athlete;

/// <summary>
/// Ejercicio básico del que el atleta da su máximo: un <b>código</b> estable en inglés,
/// un <b>nombre</b> para la UI en español y su <see cref="ExercisePattern"/>.
/// </summary>
/// <remarks>
/// El catálogo completo de ejercicios llega en el ticket #7; este es el conjunto pequeño y
/// estable que necesita el perfil en M1 como ancla para derivar cargas. Ver
/// <c>docs/adr/0009-maximos-anclados-a-ejercicios-basicos.md</c>.
/// </remarks>
/// <param name="Code">Código estable en inglés, usado como clave entre capas.</param>
/// <param name="Name">Nombre para mostrar en la UI, en español.</param>
/// <param name="Pattern">Patrón de movimiento al que pertenece.</param>
public sealed record BasicExercise(string Code, string Name, ExercisePattern Pattern);
