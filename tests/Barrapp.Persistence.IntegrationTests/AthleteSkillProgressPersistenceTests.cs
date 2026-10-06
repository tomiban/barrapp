using Barrapp.Application;
using Barrapp.Application.Abstractions;
using Barrapp.Application.Features.SkillProgress;
using Barrapp.Persistence.Knowledge;
using Barrapp.Persistence.Repositories;
using MediatR;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Barrapp.Persistence.IntegrationTests;

/// <summary>
/// Round-trip real de la etapa actual por skill contra SQLite, a través del pipeline de Application
/// (command y query de MediatR) y con el catálogo real: el esquema se crea aplicando las
/// migraciones (el mismo camino que producción) y la lectura usa un contexto nuevo.
/// </summary>
public sealed class AthleteSkillProgressPersistenceTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly ApplicationDbContext _dbContext;

    public AthleteSkillProgressPersistenceTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        _dbContext = CreateContext();
        _dbContext.Database.Migrate();
    }

    [Fact]
    public async Task A_skill_stage_is_written_and_read_back_by_skill()
    {
        using (var services = CreateServices(_dbContext))
        {
            var saved = await services.GetRequiredService<ISender>()
                .Send(new SetSkillProgressCommand("front-lever", 3));

            Assert.True(saved.IsSuccess);
            Assert.Equal("front-lever", saved.Value.SkillId);
            Assert.Equal(3, saved.Value.StageOrder);
        }

        // Contexto nuevo: la etapa se lee de la base, no de la memoria del contexto de escritura.
        using var readContext = CreateContext();
        using var readServices = CreateServices(readContext);
        var progress = await readServices.GetRequiredService<ISender>()
            .Send(new GetSkillProgressQuery());

        Assert.True(progress.IsSuccess);
        var frontLever = progress.Value.Single(entry => entry.SkillId == "front-lever");
        Assert.Equal(3, frontLever.StageOrder);

        // El resto de skills sigue en la etapa 1.
        Assert.All(
            progress.Value.Where(entry => entry.SkillId != "front-lever"),
            entry => Assert.Equal(1, entry.StageOrder));
    }

    [Fact]
    public async Task A_skill_without_a_row_is_read_at_the_first_stage()
    {
        using var services = CreateServices(_dbContext);

        var result = await services.GetRequiredService<ISender>().Send(new GetSkillProgressQuery());

        Assert.True(result.IsSuccess);
        Assert.NotEmpty(result.Value);
        Assert.All(result.Value, entry => Assert.Equal(1, entry.StageOrder));
        Assert.Contains(result.Value, entry => entry.SkillId == "planche");
    }

    [Fact]
    public async Task Setting_a_stage_that_is_not_in_the_ladder_is_rejected()
    {
        using var services = CreateServices(_dbContext);

        var result = await services.GetRequiredService<ISender>()
            .Send(new SetSkillProgressCommand("planche", 99));

        Assert.True(result.IsFailure);
        Assert.Equal("validation.failed", result.Error.Code);
        Assert.Contains(
            "La etapa indicada no existe en la escalera del skill.",
            result.Error.Description);
    }

    private static ServiceProvider CreateServices(ApplicationDbContext dbContext)
    {
        var knowledgeBase = new KnowledgeBaseCatalog(
            new KnowledgeBaseLoader(NullLogger<KnowledgeBaseLoader>.Instance).LoadEmbeddedResources());

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddApplication();
        services.AddSingleton(dbContext);
        services.AddSingleton<IApplicationDbContext>(dbContext);
        services.AddSingleton<IUnitOfWork>(dbContext);
        services.AddSingleton<IAthleteSkillProgressRepository, AthleteSkillProgressRepository>();
        services.AddSingleton<IKnowledgeBase>(knowledgeBase);

        return services.BuildServiceProvider();
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
