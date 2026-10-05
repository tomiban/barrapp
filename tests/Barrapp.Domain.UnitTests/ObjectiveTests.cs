using Barrapp.Domain.Common;
using Barrapp.Domain.Knowledge;
using Barrapp.Domain.Objectives;

namespace Barrapp.Domain.UnitTests;

/// <summary>
/// Reglas del objetivo del mesociclo: apunta a un skill y el catálogo es la única fuente de
/// verdad sobre qué skills existen.
/// </summary>
public sealed class ObjectiveTests
{
    private static readonly Guid UserId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    [Fact]
    public void Create_keeps_the_user_and_the_chosen_skill()
    {
        var result = Objective.Create(UserId, "handstand", Skills("handstand", "planche"));

        Assert.True(result.IsSuccess);
        Assert.Equal(UserId, result.Value.UserId);
        Assert.Equal("handstand", result.Value.SkillId);
    }

    [Fact]
    public void Create_rejects_a_skill_that_is_not_in_the_catalog()
    {
        var result = Objective.Create(UserId, "double-backflip", Skills("handstand"));

        Assert.True(result.IsFailure);
        Assert.Equal(DomainErrors.Objective.UnknownSkill, result.Error);
    }

    [Fact]
    public void Update_changes_the_skill_when_it_exists()
    {
        var objective = Objective.Create(UserId, "handstand", Skills("handstand", "planche")).Value;

        var result = objective.Update("planche", Skills("handstand", "planche"));

        Assert.True(result.IsSuccess);
        Assert.Equal("planche", objective.SkillId);
    }

    [Fact]
    public void Update_rejects_an_unknown_skill_and_keeps_the_previous_one()
    {
        var objective = Objective.Create(UserId, "handstand", Skills("handstand", "planche")).Value;

        var result = objective.Update("double-backflip", Skills("handstand", "planche"));

        Assert.True(result.IsFailure);
        Assert.Equal(DomainErrors.Objective.UnknownSkill, result.Error);
        Assert.Equal("handstand", objective.SkillId);
    }

    private static ISkillCatalog Skills(params string[] skillIds) => new StubSkillCatalog(skillIds);

    private sealed class StubSkillCatalog(IEnumerable<string> skillIds) : ISkillCatalog
    {
        public Skill? FindSkill(string skillId) =>
            skillIds.Contains(skillId, StringComparer.Ordinal)
                ? new Skill { Id = skillId, Name = skillId }
                : null;
    }
}
