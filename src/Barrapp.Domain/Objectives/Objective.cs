using Barrapp.Domain.Common;
using Barrapp.Domain.Knowledge;

namespace Barrapp.Domain.Objectives;

/// <summary>
/// Objetivo del mesociclo: el skill que el atleta persigue durante el bloque (ver
/// <c>GLOSSARY.md</c>, término <i>Objetivo</i>). Pertenece a un usuario y apunta a un skill
/// del catálogo.
/// </summary>
/// <remarks>
/// El estado solo cambia por sus métodos (<see cref="Create"/>, <see cref="Update"/>), que
/// validan el invariante —el skill existe en el catálogo— y son atómicos. En el MVP
/// mono-usuario, sin login, todos los objetivos pertenecen al mismo usuario fijo.
/// </remarks>
public sealed class Objective
{
    private Objective(Guid id, Guid userId, string skillId)
    {
        Id = id;
        UserId = userId;
        SkillId = skillId;
    }

    // Requerido por EF Core para materializar la entidad; nunca se usa desde el dominio.
    private Objective()
    {
    }

    /// <summary>Identificador del objetivo.</summary>
    public Guid Id { get; private set; }

    /// <summary>Identificador del atleta al que pertenece el objetivo.</summary>
    public Guid UserId { get; private set; }

    /// <summary>Slug del skill objetivo, siempre presente en el catálogo.</summary>
    public string SkillId { get; private set; } = string.Empty;

    /// <summary>
    /// Crea el objetivo para <paramref name="userId"/>. Falla con
    /// <see cref="DomainErrors.Objective.UnknownSkill"/> si el skill no está en el catálogo.
    /// </summary>
    public static Result<Objective> Create(Guid userId, string skillId, ISkillCatalog skillCatalog)
    {
        var validation = ValidateSkill(skillId, skillCatalog);
        if (validation.IsFailure)
        {
            return Result.Failure<Objective>(validation.Error);
        }

        return new Objective(Guid.NewGuid(), userId, skillId);
    }

    /// <summary>
    /// Cambia el skill objetivo. Falla si no está en el catálogo; en ese caso no se modifica nada.
    /// </summary>
    public Result Update(string skillId, ISkillCatalog skillCatalog)
    {
        var validation = ValidateSkill(skillId, skillCatalog);
        if (validation.IsFailure)
        {
            return validation;
        }

        SkillId = skillId;
        return Result.Success();
    }

    private static Result ValidateSkill(string skillId, ISkillCatalog skillCatalog)
    {
        if (skillCatalog is null
            || string.IsNullOrWhiteSpace(skillId)
            || skillCatalog.FindSkill(skillId) is null)
        {
            return Result.Failure(DomainErrors.Objective.UnknownSkill);
        }

        return Result.Success();
    }
}
