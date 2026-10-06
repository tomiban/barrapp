using Barrapp.Application.Common;
using Barrapp.Domain.Knowledge;
using Barrapp.Domain.Objectives;
using Barrapp.Persistence.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Barrapp.Persistence.IntegrationTests;

/// <summary>
/// Round-trip real del objetivo contra SQLite: el esquema se crea aplicando las migraciones
/// (el mismo camino que producción) y la lectura usa un contexto nuevo.
/// </summary>
public sealed class ObjectivePersistenceTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly ApplicationDbContext _dbContext;

    public ObjectivePersistenceTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        _dbContext = CreateContext();
        _dbContext.Database.Migrate();
    }

    [Fact]
    public async Task There_is_no_objective_before_one_is_saved()
    {
        var repository = new ObjectiveRepository(CreateContext());

        var loaded = await repository.GetByUserIdAsync(SingleUser.Id, CancellationToken.None);

        Assert.Null(loaded);
    }

    [Fact]
    public async Task A_new_objective_is_written_and_read_back_with_the_same_skill()
    {
        var objective = Objective.Create(SingleUser.Id, "handstand", Catalog("handstand")).Value;
        _dbContext.Objectives.Add(objective);
        await _dbContext.SaveChangesAsync();

        await using var readContext = CreateContext();
        var reloaded = await new ObjectiveRepository(readContext)
            .GetByUserIdAsync(SingleUser.Id, CancellationToken.None);

        Assert.NotNull(reloaded);
        Assert.Equal(objective.Id, reloaded!.Id);
        Assert.Equal(SingleUser.Id, reloaded.UserId);
        Assert.Equal("handstand", reloaded.SkillId);
    }

    private static ISkillCatalog Catalog(params string[] skillIds) => new StubSkillCatalog(skillIds);

    private sealed class StubSkillCatalog(IEnumerable<string> skillIds) : ISkillCatalog
    {
        public Skill? FindSkill(string skillId) =>
            skillIds.Contains(skillId, StringComparer.Ordinal)
                ? new Skill { Id = skillId, Name = skillId }
                : null;
    }

    private ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(_connection)
            .Options;

        return new ApplicationDbContext(options);
    }

    public void Dispose()
    {
        _dbContext.Dispose();
        _connection.Dispose();
    }
}
