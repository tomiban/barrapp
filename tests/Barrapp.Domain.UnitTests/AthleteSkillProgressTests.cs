using Barrapp.Domain.Common;
using Barrapp.Domain.Knowledge;
using Barrapp.Domain.SkillProgress;

namespace Barrapp.Domain.UnitTests;

/// <summary>
/// Reglas de la etapa actual del atleta por skill: apunta a una etapa existente de la escalera y
/// el catálogo es la única fuente de verdad sobre qué skills y etapas existen.
/// </summary>
public sealed class AthleteSkillProgressTests
{
    private static readonly Guid UserId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    [Fact]
    public void Create_keeps_the_user_skill_and_stage()
    {
        var result = AthleteSkillProgress.Create(UserId, "planche", 2, Catalog("planche", 1, 2, 3));

        Assert.True(result.IsSuccess);
        Assert.Equal(UserId, result.Value.UserId);
        Assert.Equal("planche", result.Value.SkillId);
        Assert.Equal(2, result.Value.StageOrder);
    }

    [Fact]
    public void Create_rejects_a_stage_that_is_not_in_the_ladder()
    {
        var result = AthleteSkillProgress.Create(UserId, "planche", 9, Catalog("planche", 1, 2, 3));

        Assert.True(result.IsFailure);
        Assert.Equal(DomainErrors.SkillProgress.UnknownStage, result.Error);
    }

    [Fact]
    public void Create_rejects_a_skill_that_is_not_in_the_catalog()
    {
        var result = AthleteSkillProgress.Create(UserId, "double-backflip", 1, Catalog("planche", 1, 2, 3));

        Assert.True(result.IsFailure);
        Assert.Equal(DomainErrors.SkillProgress.UnknownSkill, result.Error);
    }

    [Fact]
    public void Update_changes_the_stage_when_it_exists()
    {
        var progress = AthleteSkillProgress.Create(UserId, "planche", 1, Catalog("planche", 1, 2, 3)).Value;

        var result = progress.Update(3, Catalog("planche", 1, 2, 3));

        Assert.True(result.IsSuccess);
        Assert.Equal(3, progress.StageOrder);
    }

    [Fact]
    public void Update_rejects_an_unknown_stage_and_keeps_the_previous_one()
    {
        var progress = AthleteSkillProgress.Create(UserId, "planche", 2, Catalog("planche", 1, 2, 3)).Value;

        var result = progress.Update(9, Catalog("planche", 1, 2, 3));

        Assert.True(result.IsFailure);
        Assert.Equal(DomainErrors.SkillProgress.UnknownStage, result.Error);
        Assert.Equal(2, progress.StageOrder);
    }

    private static ISkillCatalog Catalog(string skillId, params int[] stageOrders) =>
        new StubSkillCatalog(skillId, stageOrders);

    private sealed class StubSkillCatalog(string skillId, int[] stageOrders) : ISkillCatalog
    {
        public Skill? FindSkill(string requestedSkillId) =>
            string.Equals(skillId, requestedSkillId, StringComparison.Ordinal)
                ? new Skill
                {
                    Id = skillId,
                    Name = skillId,
                    Stages = stageOrders
                        .Select(order => new SkillStage { Order = order, Name = $"Etapa {order}" })
                        .ToList(),
                }
                : null;
    }
}
