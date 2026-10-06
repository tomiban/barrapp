using Barrapp.Application;
using Barrapp.Application.Abstractions;
using Barrapp.Application.Common;
using Barrapp.Application.Features.SessionLogs;
using Barrapp.Domain.Sessions;
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
/// Round-trip real del registro de sesión contra SQLite: el esquema se crea aplicando las
/// migraciones (el mismo camino que producción) y las series viajan como objetos valor owned.
/// Un test recorre el pipeline completo de Application (command + query de MediatR) con el
/// catálogo real.
/// </summary>
public sealed class SessionLogPersistenceTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly ApplicationDbContext _dbContext;

    public SessionLogPersistenceTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        _dbContext = CreateContext();
        _dbContext.Database.Migrate();
    }

    [Fact]
    public async Task A_session_log_is_written_and_read_back_with_its_sets()
    {
        var log = SessionLog.Create(
            SingleUser.Id,
            "push_up",
            mesocycleId: null,
            sessionDay: 1,
            new DateTimeOffset(2026, 10, 5, 18, 30, 0, TimeSpan.Zero),
            [new SessionLogSetInput(1, 10), new SessionLogSetInput(2, 11, 2)]);
        _dbContext.SessionLogs.Add(log.Value);
        await _dbContext.SaveChangesAsync();

        var reloaded = await ReadLogsAsync();

        var loaded = Assert.Single(reloaded);
        Assert.Equal(log.Value.Id, loaded.Id);
        Assert.Equal("push_up", loaded.ExerciseId);
        Assert.Null(loaded.MesocycleId);
        Assert.Equal(1, loaded.SessionDay);
        Assert.Equal(2, loaded.Sets.Count);
        Assert.Equal([1, 2], loaded.Sets.Select(set => set.SetNumber));
        Assert.Equal([10, 11], loaded.Sets.Select(set => set.Value));
        Assert.Null(loaded.Sets.ElementAt(0).Effort);
        Assert.Equal(2, loaded.Sets.ElementAt(1).Effort);
        Assert.Equal(new DateTimeOffset(2026, 10, 5, 18, 30, 0, TimeSpan.Zero), loaded.RecordedAtUtc);
    }

    [Fact]
    public async Task Two_logs_for_the_same_day_and_exercise_do_not_share_sets()
    {
        var first = SessionLog.Create(
            SingleUser.Id,
            "push_up",
            null,
            1,
            new DateTimeOffset(2026, 10, 5, 18, 0, 0, TimeSpan.Zero),
            [new SessionLogSetInput(1, 10)]);
        var second = SessionLog.Create(
            SingleUser.Id,
            "push_up",
            null,
            1,
            new DateTimeOffset(2026, 10, 5, 19, 0, 0, TimeSpan.Zero),
            [new SessionLogSetInput(1, 12), new SessionLogSetInput(2, 13)]);
        _dbContext.SessionLogs.AddRange(first.Value, second.Value);
        await _dbContext.SaveChangesAsync();

        var reloaded = await ReadLogsAsync();

        Assert.Equal(2, reloaded.Count);
        Assert.Equal([10], reloaded.ElementAt(0).Sets.Select(set => set.Value));
        Assert.Equal([12, 13], reloaded.ElementAt(1).Sets.Select(set => set.Value));
    }

    [Fact]
    public async Task The_session_log_command_pipeline_registers_and_lists_the_log()
    {
        using (var services = CreateServices(_dbContext))
        {
            var saved = await services.GetRequiredService<ISender>().Send(
                new RegisterSessionLogCommand(
                    "push_up",
                    MesocycleId: null,
                    SessionDay: 1,
                    [new SessionLogSetInput(1, 10), new SessionLogSetInput(2, 11)]));

            Assert.True(saved.IsSuccess);
            Assert.Equal("Flexiones", saved.Value.ExerciseName);
            Assert.Equal("reps", saved.Value.Metric);
        }

        // Contexto nuevo: el registro se lee de la base, no de la memoria del contexto de escritura.
        using var readContext = CreateContext();
        using var readServices = CreateServices(readContext);
        var logs = await readServices.GetRequiredService<ISender>().Send(new GetSessionLogsQuery());

        Assert.True(logs.IsSuccess);
        var loaded = Assert.Single(logs.Value);
        Assert.Equal("push_up", loaded.ExerciseId);
        Assert.Equal("Flexiones", loaded.ExerciseName);
        Assert.Equal("reps", loaded.Metric);
        Assert.Equal([10, 11], loaded.Sets.Select(set => set.Value));
    }

    [Fact]
    public async Task An_updated_log_is_written_and_read_back_with_its_new_sets()
    {
        var log = SessionLog.Create(
            SingleUser.Id,
            "push_up",
            mesocycleId: null,
            sessionDay: 1,
            new DateTimeOffset(2026, 10, 5, 18, 30, 0, TimeSpan.Zero),
            [new SessionLogSetInput(1, 10), new SessionLogSetInput(2, 11)]);
        _dbContext.SessionLogs.Add(log.Value);
        await _dbContext.SaveChangesAsync();

        log.Value.Update([new SessionLogSetInput(1, 12), new SessionLogSetInput(2, 13, 2)]);
        await _dbContext.SaveChangesAsync();

        var reloaded = await ReadLogsAsync();

        var loaded = Assert.Single(reloaded);
        Assert.Equal([12, 13], loaded.Sets.Select(set => set.Value));
        Assert.Equal(2, loaded.Sets.ElementAt(1).Effort);
        Assert.Equal("push_up", loaded.ExerciseId);
    }

    [Fact]
    public async Task A_deleted_log_disappears_together_with_its_sets()
    {
        var log = SessionLog.Create(
            SingleUser.Id,
            "push_up",
            mesocycleId: null,
            sessionDay: 1,
            new DateTimeOffset(2026, 10, 5, 18, 30, 0, TimeSpan.Zero),
            [new SessionLogSetInput(1, 10), new SessionLogSetInput(2, 11)]);
        _dbContext.SessionLogs.Add(log.Value);
        await _dbContext.SaveChangesAsync();

        var repository = new SessionLogRepository(_dbContext);
        var loaded = await repository.GetByIdAsync(log.Value.Id);
        Assert.NotNull(loaded);
        repository.Remove(loaded!);
        await _dbContext.SaveChangesAsync();

        Assert.Empty(await ReadLogsAsync());
    }

    [Fact]
    public async Task The_update_and_delete_commands_run_through_the_pipeline()
    {
        using (var services = CreateServices(_dbContext))
        {
            var isender = services.GetRequiredService<ISender>();
            var saved = await isender.Send(
                new RegisterSessionLogCommand("push_up", null, 1, [new SessionLogSetInput(1, 10)]));
            Assert.True(saved.IsSuccess);

            var updated = await isender.Send(
                new UpdateSessionLogCommand(saved.Value.Id, [new SessionLogSetInput(1, 15), new SessionLogSetInput(2, 16, 3)]));
            Assert.True(updated.IsSuccess);
            Assert.Equal([15, 16], updated.Value.Sets.Select(set => set.Value));
            Assert.Equal(3, updated.Value.Sets.ElementAt(1).Effort);

            var deleted = await isender.Send(new DeleteSessionLogCommand(saved.Value.Id));
            Assert.True(deleted.IsSuccess);
        }

        using var readContext = CreateContext();
        using var readServices = CreateServices(readContext);
        var logs = await readServices.GetRequiredService<ISender>().Send(new GetSessionLogsQuery());
        Assert.True(logs.IsSuccess);
        Assert.Empty(logs.Value);
    }

    private async Task<IReadOnlyList<SessionLog>> ReadLogsAsync()
    {
        await using var readContext = CreateContext();
        var logs = await readContext.SessionLogs
            .AsNoTracking()
            .ToListAsync();

        // SQLite no ordena por DateTimeOffset en SQL; se ordena en memoria.
        return logs.OrderBy(log => log.RecordedAtUtc).ToList();
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
        services.AddSingleton<ISessionLogRepository, SessionLogRepository>();
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
