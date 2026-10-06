namespace Barrapp.Domain.Knowledge;

/// <summary>
/// Consulta del catálogo que necesita el motor de generación: resolver skills (con su escalera) y
/// ejercicios por su id. Es un puerto del dominio; lo implementa el catálogo en memoria y lo
/// consumen el motor de generación y la sesión suelta sin depender de capas superiores.
/// </summary>
public interface IGenerationCatalog : ISkillCatalog
{
    /// <summary>Busca un ejercicio por su id; <c>null</c> si no existe.</summary>
    Exercise? FindExercise(string exerciseId);

    /// <summary>Ejercicios del grupo indicado (empuje, tirón, pierna, core o cardio).</summary>
    IReadOnlyList<Exercise> ExercisesByGroup(ExerciseGroup group);
}
