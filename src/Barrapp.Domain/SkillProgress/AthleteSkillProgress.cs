using Barrapp.Domain.Common;
using Barrapp.Domain.Knowledge;

namespace Barrapp.Domain.SkillProgress;

/// <summary>
/// Progresión de skill: la etapa actual del atleta dentro de la escalera de un skill (ver
/// <c>GLOSSARY.md</c>, término <i>Progresión de skill</i>). Pertenece a un usuario y apunta a una
/// etapa existente de la escalera del skill.
/// </summary>
/// <remarks>
/// El estado solo cambia por sus métodos (<see cref="Create"/>, <see cref="Update"/>), que validan
/// el invariante —el skill existe y la etapa existe en su escalera— y son atómicos. En el MVP
/// mono-usuario, sin login, toda la progresión pertenece al mismo usuario fijo.
/// La regla de avance automático (criterio cumplido en dos sesiones consecutivas) no vive aquí.
/// </remarks>
public sealed class AthleteSkillProgress
{
    private AthleteSkillProgress(Guid id, Guid userId, string skillId, int stageOrder)
    {
        Id = id;
        UserId = userId;
        SkillId = skillId;
        StageOrder = stageOrder;
    }

    // Requerido por EF Core para materializar la entidad; nunca se usa desde el dominio.
    private AthleteSkillProgress()
    {
    }

    /// <summary>Identificador de la progresión.</summary>
    public Guid Id { get; private set; }

    /// <summary>Identificador del atleta al que pertenece la progresión.</summary>
    public Guid UserId { get; private set; }

    /// <summary>Slug del skill, siempre presente en el catálogo.</summary>
    public string SkillId { get; private set; } = string.Empty;

    /// <summary>Orden de la etapa actual dentro de la escalera del skill.</summary>
    public int StageOrder { get; private set; }

    /// <summary>
    /// Crea la progresión inicial de <paramref name="userId"/> para <paramref name="skillId"/> en
    /// una etapa concreta. Falla con <see cref="DomainErrors.SkillProgress.UnknownSkill"/> si el
    /// skill no está en el catálogo y con <see cref="DomainErrors.SkillProgress.UnknownStage"/> si
    /// la etapa no existe en su escalera.
    /// </summary>
    public static Result<AthleteSkillProgress> Create(
        Guid userId,
        string skillId,
        int stageOrder,
        ISkillCatalog skillCatalog)
    {
        var validation = ValidateStage(skillId, stageOrder, skillCatalog);
        if (validation.IsFailure)
        {
            return Result.Failure<AthleteSkillProgress>(validation.Error);
        }

        return new AthleteSkillProgress(Guid.NewGuid(), userId, skillId, stageOrder);
    }

    /// <summary>
    /// Cambia la etapa actual. Falla si la etapa no existe en la escalera del skill; en ese caso no
    /// se modifica nada.
    /// </summary>
    public Result Update(int stageOrder, ISkillCatalog skillCatalog)
    {
        var validation = ValidateStage(SkillId, stageOrder, skillCatalog);
        if (validation.IsFailure)
        {
            return validation;
        }

        StageOrder = stageOrder;
        return Result.Success();
    }

    private static Result ValidateStage(string skillId, int stageOrder, ISkillCatalog skillCatalog)
    {
        var skill = skillCatalog is null || string.IsNullOrWhiteSpace(skillId)
            ? null
            : skillCatalog.FindSkill(skillId);

        if (skill is null)
        {
            return Result.Failure(DomainErrors.SkillProgress.UnknownSkill);
        }

        if (!skill.Stages.Any(stage => stage.Order == stageOrder))
        {
            return Result.Failure(DomainErrors.SkillProgress.UnknownStage);
        }

        return Result.Success();
    }
}
