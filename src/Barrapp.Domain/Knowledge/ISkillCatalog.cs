namespace Barrapp.Domain.Knowledge;

/// <summary>
/// Consulta de skills del catálogo. El dominio la usa para validar referencias a skills sin
/// depender del catálogo completo ni de las capas superiores.
/// </summary>
public interface ISkillCatalog
{
    /// <summary>Busca un skill por su id; <c>null</c> si no existe.</summary>
    Skill? FindSkill(string skillId);
}
