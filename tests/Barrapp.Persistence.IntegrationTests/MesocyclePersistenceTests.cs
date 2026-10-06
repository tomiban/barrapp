using Barrapp.Application.Common;
using Barrapp.Domain.Athlete;
using Barrapp.Domain.Objectives;
using Barrapp.Domain.Planning;
using Barrapp.Persistence.Knowledge;
using Barrapp.Persistence.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace Barrapp.Persistence.IntegrationTests;

/// <summary>
/// Round-trip real del mesociclo persistido (#27) contra SQLite: el esquema se crea aplicando las
/// migraciones (el mismo camino que producción) sobre una base en memoria, y cada lectura usa un
/// contexto nuevo para no depender de la caché del contexto que escribió. El plan se guarda como
/// snapshot en una columna JSON (decisión de D7); aquí se comprueba que el round-trip conserva el
/// plan íntegro y que el estado sigue su transición Active → Closed.
/// </summary>
public sealed class MesocyclePersistenceTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly ApplicationDbContext _dbContext;

    public MesocyclePersistenceTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        _dbContext = CreateContext();
        _dbContext.Database.Migrate();
    }

    [Fact]
    public async Task There_is_no_active_mesocycle_before_one_is_generated()
    {
        var repository = new MesocycleRepository(CreateContext());

        var active = await repository.GetActiveByUserIdAsync(SingleUser.Id, CancellationToken.None);

        Assert.Null(active);
    }

    [Fact]
    public async Task A_mesocycle_is_written_and_read_back_with_its_plan_intact()
    {
        var plan = BuildPlan();
        var startedAtUtc = DateTimeOffset.Parse("2026-10-01T08:00:00+00:00");
        var mesocycle = Mesocycle.Create(SingleUser.Id, plan, startedAtUtc).Value;

        var repository = new MesocycleRepository(_dbContext);
        repository.Add(mesocycle);
        await _dbContext.SaveChangesAsync();

        var reloaded = await new MesocycleRepository(CreateContext())
            .GetByIdAsync(mesocycle.Id, SingleUser.Id, CancellationToken.None);

        Assert.NotNull(reloaded);
        Assert.Equal(mesocycle.Id, reloaded!.Id);
        Assert.Equal(SingleUser.Id, reloaded.UserId);
        Assert.Equal("planche", reloaded.SkillId);
        Assert.Equal(3, reloaded.TrainingDays);
        Assert.Equal(MesocycleStatus.Active, reloaded.Status);
        Assert.Equal(startedAtUtc, reloaded.StartedAtUtc);
        Assert.Null(reloaded.ClosedAtUtc);

        // El snapshot sobrevive al round-trip: de la base sale el mismo plan.
        Assert.Equal(Snapshot(plan), Snapshot(reloaded.Snapshot.ToPlan()));
    }

    [Fact]
    public async Task The_snapshot_is_stored_as_a_json_column()
    {
        var mesocycle = Mesocycle.Create(SingleUser.Id, BuildPlan(), DateTimeOffset.UtcNow).Value;

        _dbContext.Mesocycles.Add(mesocycle);
        await _dbContext.SaveChangesAsync();

        await using var command = _connection.CreateCommand();
        command.CommandText = "SELECT Snapshot FROM Mesocycles";

        var raw = await command.ExecuteScalarAsync() as string;

        // El snapshot vive serializado en una única columna JSON (decisión de D7): el plan
        // completo —skill, semanas, sesiones y filas— cabe en la fila.
        Assert.NotNull(raw);
        Assert.Contains("\"SkillId\"", raw);
        Assert.Contains("\"microcycles\"", raw, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Mesocycle", raw, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task The_active_mesocycle_is_found_for_the_user_and_missing_for_others()
    {
        var mesocycle = Mesocycle.Create(SingleUser.Id, BuildPlan(), DateTimeOffset.UtcNow).Value;
        _dbContext.Mesocycles.Add(mesocycle);
        await _dbContext.SaveChangesAsync();

        var repository = new MesocycleRepository(CreateContext());
        var active = await repository.GetActiveByUserIdAsync(SingleUser.Id, CancellationToken.None);
        var foreign = await repository.GetActiveByUserIdAsync(
            Guid.Parse("33333333-3333-3333-3333-333333333333"),
            CancellationToken.None);

        Assert.NotNull(active);
        Assert.Equal(mesocycle.Id, active!.Id);
        Assert.Null(foreign);
    }

    [Fact]
    public async Task A_closed_mesocycle_is_persisted_closed_and_stops_being_active()
    {
        var mesocycle = Mesocycle.Create(SingleUser.Id, BuildPlan(), DateTimeOffset.UtcNow).Value;
        var closedAtUtc = DateTimeOffset.Parse("2026-11-01T08:00:00+00:00");
        Assert.True(mesocycle.Close(closedAtUtc).IsSuccess);

        var repository = new MesocycleRepository(_dbContext);
        repository.Add(mesocycle);
        await _dbContext.SaveChangesAsync();

        var reloaded = await new MesocycleRepository(CreateContext())
            .GetByIdAsync(mesocycle.Id, SingleUser.Id, CancellationToken.None);
        var active = await new MesocycleRepository(CreateContext())
            .GetActiveByUserIdAsync(SingleUser.Id, CancellationToken.None);

        Assert.NotNull(reloaded);
        Assert.Equal(MesocycleStatus.Closed, reloaded!.Status);
        Assert.Equal(closedAtUtc, reloaded.ClosedAtUtc);
        Assert.Null(active);
    }

    [Fact]
    public async Task Removing_a_mesocycle_makes_it_disappear_from_active_and_detail()
    {
        var mesocycle = Mesocycle.Create(SingleUser.Id, BuildPlan(), DateTimeOffset.UtcNow).Value;

        var repository = new MesocycleRepository(_dbContext);
        repository.Add(mesocycle);
        await _dbContext.SaveChangesAsync();

        repository.Remove(mesocycle);
        await _dbContext.SaveChangesAsync();

        var readContext = CreateContext();
        var reloaded = await new MesocycleRepository(readContext)
            .GetByIdAsync(mesocycle.Id, SingleUser.Id, CancellationToken.None);
        var active = await new MesocycleRepository(readContext)
            .GetActiveByUserIdAsync(SingleUser.Id, CancellationToken.None);

        Assert.Null(reloaded);
        Assert.Null(active);
    }

    private static Plan BuildPlan()
    {
        var catalog = new KnowledgeBaseCatalog(
            new KnowledgeBaseLoader(NullLogger<KnowledgeBaseLoader>.Instance).LoadEmbeddedResources());

        var profile = AthleteProfile.Create(
            SingleUser.Id,
            78,
            180,
            180,
            85,
            3,
            [
                new MaximumInput("push_up", 10),
                new MaximumInput("pull_up", 5),
                new MaximumInput("squat", 20),
            ]).Value;

        var objective = Objective.Create(SingleUser.Id, "planche", catalog).Value;

        return PlanGenerator.Generate(profile, objective, stageOrder: 1, catalog).Value;
    }

    /// <summary>Proyección canónica del plan para comparar sin depender de Equals.</summary>
    private static string Snapshot(Plan plan) =>
        string.Join(
            "|",
            plan.Microcycles.SelectMany(microcycle =>
                microcycle.Sessions.SelectMany(session =>
                    session.Items.Select(item =>
                        $"{microcycle.Number}:{session.Day}:{item.ExerciseId}:{item.Role}:{item.Pattern}:"
                        + $"{item.Sets}:{item.RepsMin}-{item.RepsMax}:{item.HoldSecondsMin}-{item.HoldSecondsMax}:{item.Note}"))));

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
