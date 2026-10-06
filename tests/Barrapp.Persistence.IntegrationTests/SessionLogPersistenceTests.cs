using Barrapp.Application;
using Barrapp.Application.Abstractions;
using Barrapp.Application.Common;
using Barrapp.Application.Features.SessionLogs;
using Barrapp.Domain.Knowledge;
using Barrapp.Domain.Planning;
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
/// migraciones (el mismo camino que producción) y la cabecera viaja con sus ítems (foto por ítem,
/// ADR-0014) y sus series como objetos valor owned. Un test recorre el pipeline completo de
/// Application (commands y queries de MediatR) con el catálogo real.
/// </summary>
public sealed class SessionLogPersistenceTests : IDisposable
{
    private static readonly DateOnly SessionDate = new(2026, 10, 5);
    private static readonly DateTimeOffset Recorded = new(2026, 10, 5, 18, 30, 0, TimeSpan.Zero);

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
    public async Task A_session_log_is_written_and_read_back_with_its_header_items_and_sets()
    {
        var log = Session(SessionDate);
        log.UpsertItem(Item("push_up", "Flexiones", sets: [Set(1, 10), Set(2, 11, actualRir: 2, loadKg: 5)]));
        log.Complete(new DateTimeOffset(2026, 10, 5, 19, 0, 0, TimeSpan.Zero));
        _dbContext.SessionLogs.Add(log);
        await _dbContext.SaveChangesAsync();

        var reloaded = await ReadLogsAsync();

        var loaded = Assert.Single(reloaded);
        Assert.Equal(log.Id, loaded.Id);
        Assert.Equal(SessionLogKind.Mesocycle, loaded.Kind);
        Assert.Equal(SessionDate, loaded.SessionDate);
        Assert.Equal(MesocycleId, loaded.MesocycleId);
        Assert.Equal(2, loaded.MicrocycleNumber);
        Assert.Equal(1, loaded.SessionDay);
        Assert.Equal(Recorded, loaded.RecordedAtUtc);
        Assert.True(loaded.IsCompleted);

        var item = Assert.Single(loaded.Items);
        Assert.Equal("push_up", item.ExerciseId);
        Assert.Equal("Flexiones", item.ExerciseName);
        Assert.Equal(SessionItemRole.Strength, item.Role);
        Assert.Equal(ExerciseGroup.Push, item.Pattern!.Value);
        Assert.Equal(Metric.Reps, item.Metric);
        Assert.Equal(3, item.PrescribedSets);
        Assert.Equal([8, 12], [item.RepsMin, item.RepsMax]);
        Assert.Equal([1, 2], item.Sets.Select(set => set.SetNumber));
        Assert.Equal([10, 11], item.Sets.Select(set => set.Value));
        Assert.Null(item.Sets.ElementAt(0).ActualRir);
        Assert.Equal(2, item.Sets.ElementAt(1).ActualRir);
        Assert.Equal(5, item.Sets.ElementAt(1).LoadKg);
    }

    [Fact]
    public async Task A_hold_item_keeps_its_seconds_photo_after_the_round_trip()
    {
        var log = Session(SessionDate);
        log.UpsertItem(Item(
            "hollow-body-hold",
            "Cuerpo hueco",
            role: SessionItemRole.Core,
            pattern: null,
            metric: Metric.Seconds,
            repsMin: null,
            repsMax: null,
            holdSecondsMin: 20,
            holdSecondsMax: 30,
            sets: [Set(1, 25), Set(2, 30)]));
        _dbContext.SessionLogs.Add(log);
        await _dbContext.SaveChangesAsync();

        var loaded = Assert.Single(await ReadLogsAsync());
        var item = Assert.Single(loaded.Items);

        Assert.Equal(Metric.Seconds, item.Metric);
        Assert.Equal([20, 30], [item.HoldSecondsMin, item.HoldSecondsMax]);
        Assert.Null(item.RepsMin);
        Assert.Equal([25, 30], item.Sets.Select(set => set.Value));
    }

    [Fact]
    public async Task Two_items_of_the_same_session_keep_their_own_sets()
    {
        var log = Session(SessionDate);
        log.UpsertItem(Item("push_up", "Flexiones", sets: [Set(1, 10)]));
        log.UpsertItem(Item("pull_up", "Dominadas", sets: [Set(1, 5), Set(2, 6)]));
        _dbContext.SessionLogs.Add(log);
        await _dbContext.SaveChangesAsync();

        var loaded = Assert.Single(await ReadLogsAsync());

        Assert.Equal(2, loaded.Items.Count);
        Assert.Equal([1, 2], loaded.Items.Select(item => item.Position));
        Assert.Equal([10], loaded.Items.ElementAt(0).Sets.Select(set => set.Value));
        Assert.Equal([5, 6], loaded.Items.ElementAt(1).Sets.Select(set => set.Value));
    }

    [Fact]
    public async Task Two_sessions_with_the_same_day_of_different_microcycles_are_different_rows()
    {
        var firstWeek = Session(SessionDate, microcycleNumber: 1);
        firstWeek.UpsertItem(Item("push_up", "Flexiones", sets: [Set(1, 10)]));
        var secondWeek = Session(SessionDate, microcycleNumber: 2);
        secondWeek.UpsertItem(Item("push_up", "Flexiones", sets: [Set(1, 14)]));
        _dbContext.SessionLogs.AddRange(firstWeek, secondWeek);
        await _dbContext.SaveChangesAsync();

        var reloaded = await ReadLogsAsync();

        Assert.Equal(2, reloaded.Count);
        Assert.Equal([1, 2], reloaded.Select(log => log.MicrocycleNumber));
        Assert.Equal([10], reloaded.ElementAt(0).Items.Single().Sets.Select(set => set.Value));
        Assert.Equal([14], reloaded.ElementAt(1).Items.Single().Sets.Select(set => set.Value));
    }

    [Fact]
    public async Task Two_sessions_with_the_same_deterministic_key_violate_the_unique_filtered_index()
    {
        var first = Session(SessionDate);
        first.UpsertItem(Item("push_up", "Flexiones", sets: [Set(1, 10)]));
        var second = Session(SessionDate);
        second.UpsertItem(Item("push_up", "Flexiones", sets: [Set(1, 12)]));
        _dbContext.SessionLogs.AddRange(first, second);

        // El índice único filtrado (atleta, tipo, mesociclo, microciclo y día) WHERE Kind = mesociclo
        // rechaza la segunda fila con la misma clave de sesión (ADR-0014).
        await Assert.ThrowsAsync<DbUpdateException>(() => _dbContext.SaveChangesAsync());
    }

    [Fact]
    public async Task Two_suelta_sessions_of_the_same_day_are_both_saved()
    {
        var first = Session(SessionDate, kind: SessionLogKind.Suelta, microcycleNumber: null, sessionDay: null);
        first.UpsertItem(Item("push_up", "Flexiones", sets: [Set(1, 10)]));
        var second = Session(SessionDate, kind: SessionLogKind.Suelta, microcycleNumber: null, sessionDay: null);
        second.UpsertItem(Item("push_up", "Flexiones", sets: [Set(1, 12)]));
        _dbContext.SessionLogs.AddRange(first, second);

        await _dbContext.SaveChangesAsync();

        var reloaded = await ReadLogsAsync();
        Assert.Equal(2, reloaded.Count);
        Assert.All(reloaded, log => Assert.Equal(SessionLogKind.Suelta, log.Kind));
    }

    [Fact]
    public async Task Two_items_with_the_same_client_id_violate_the_unique_filtered_index()
    {
        var clientId = Guid.NewGuid();
        var log = Session(SessionDate);
        log.UpsertItem(Item("push_up", "Flexiones", sets: [Set(1, 10)], clientId: clientId));
        _dbContext.SessionLogs.Add(log);
        await _dbContext.SaveChangesAsync();

        var other = Session(SessionDate, microcycleNumber: 3);
        other.UpsertItem(Item("pull_up", "Dominadas", sets: [Set(1, 5)], clientId: clientId));
        _dbContext.SessionLogs.Add(other);

        await Assert.ThrowsAsync<DbUpdateException>(() => _dbContext.SaveChangesAsync());
    }

    [Fact]
    public async Task The_session_log_command_pipeline_registers_and_lists_the_log_with_its_photo()
    {
        using (var services = CreateServices(_dbContext))
        {
            var saved = await services.GetRequiredService<ISender>().Send(RegisterCommand(
                "push_up",
                [Set(1, 10), Set(2, 11)]));

            Assert.True(saved.IsSuccess);
            var item = Assert.Single(saved.Value.Items);
            Assert.Equal("Flexiones", item.ExerciseName);
            Assert.Equal("reps", item.Metric);
            Assert.Equal("strength", item.Role);
        }

        // Contexto nuevo: el registro se lee de la base, no de la memoria del contexto de escritura,
        // y el listado sale sin volver a consultar el catálogo (foto por ítem, ADR-0014).
        await using var readContext = CreateContext();
        using var readServices = CreateServices(readContext);
        var logs = await readServices.GetRequiredService<ISender>().Send(new GetSessionLogsQuery());

        Assert.True(logs.IsSuccess);
        var loaded = Assert.Single(logs.Value);
        var loadedItem = Assert.Single(loaded.Items);
        Assert.Equal("push_up", loadedItem.ExerciseId);
        Assert.Equal("Flexiones", loadedItem.ExerciseName);
        Assert.Equal("reps", loadedItem.Metric);
        Assert.Equal([10, 11], loadedItem.Sets.Select(set => set.Value));
    }

    [Fact]
    public async Task Registering_twice_with_the_same_client_id_updates_in_place_without_duplicating()
    {
        var clientId = Guid.NewGuid();
        Guid firstId;
        Guid firstItemId;
        using (var services = CreateServices(_dbContext))
        {
            var isender = services.GetRequiredService<ISender>();
            var first = await isender.Send(RegisterCommand(
                "push_up",
                [Set(1, 10), Set(2, 11)],
                clientId: clientId));
            Assert.True(first.IsSuccess);
            firstId = first.Value.Id;
            firstItemId = first.Value.Items.Single().Id;

            // El cliente reenvía el mismo ítem (mismo clientId) con las series corregidas.
            var second = await isender.Send(RegisterCommand(
                "push_up",
                [Set(1, 14), Set(2, 14)],
                clientId: clientId));
            Assert.True(second.IsSuccess);
            Assert.Equal(firstId, second.Value.Id);
            Assert.Equal(firstItemId, second.Value.Items.Single().Id);
            Assert.Equal([14, 14], second.Value.Items.Single().Sets.Select(set => set.Value));
        }

        // Contexto nuevo: sigue habiendo una sola sesión y un solo ítem, con su id original.
        await using var readContext = CreateContext();
        var logs = await readContext.SessionLogs.Include(log => log.Items).ToListAsync();
        var single = Assert.Single(logs);
        Assert.Equal(firstId, single.Id);
        Assert.Equal(firstItemId, Assert.Single(single.Items).Id);
        Assert.Equal([14, 14], single.Items.Single().Sets.Select(set => set.Value));
    }

    [Fact]
    public async Task Registering_a_second_exercise_adds_it_to_the_same_session()
    {
        using var services = CreateServices(_dbContext);
        var isender = services.GetRequiredService<ISender>();

        await isender.Send(RegisterCommand("push_up"));
        var second = await isender.Send(RegisterCommand("pull_up", pattern: ExerciseGroup.Pull, sets: [Set(1, 5)]));

        Assert.True(second.IsSuccess);
        Assert.Equal(2, second.Value.Items.Count);
        Assert.Equal(["push_up", "pull_up"], second.Value.Items.Select(item => item.ExerciseId));
    }

    [Fact]
    public async Task The_update_delete_and_completion_commands_run_through_the_pipeline()
    {
        Guid sessionId;
        Guid itemId;
        using (var services = CreateServices(_dbContext))
        {
            var isender = services.GetRequiredService<ISender>();
            var saved = await isender.Send(RegisterCommand("push_up"));
            Assert.True(saved.IsSuccess);
            sessionId = saved.Value.Id;
            itemId = saved.Value.Items.Single().Id;

            var updated = await isender.Send(new UpdateSessionLogItemCommand(
                sessionId,
                itemId,
                [Set(1, 15), Set(2, 16, actualRir: 3, loadKg: 7.5)]));
            Assert.True(updated.IsSuccess);
            Assert.Equal([15, 16], updated.Value.Sets.Select(set => set.Value));
            Assert.Equal(3, updated.Value.Sets.ElementAt(1).ActualRir);
            Assert.Equal(7.5, updated.Value.Sets.ElementAt(1).LoadKg);
            // La foto del ítem sobrevive a la edición (ADR-0014).
            Assert.Equal("Flexiones", updated.Value.ExerciseName);

            var completed = await isender.Send(new SetSessionLogCompletionCommand(sessionId, Completed: true));
            Assert.True(completed.IsSuccess);
            Assert.True(completed.Value.Completed);
            Assert.NotNull(completed.Value.CompletedAtUtc);

            var uncompleted = await isender.Send(new SetSessionLogCompletionCommand(sessionId, Completed: false));
            Assert.True(uncompleted.IsSuccess);
            Assert.False(uncompleted.Value.Completed);

            var deleted = await isender.Send(new DeleteSessionLogItemCommand(sessionId, itemId));
            Assert.True(deleted.IsSuccess);
        }

        await using var readContext = CreateContext();
        using var readServices = CreateServices(readContext);
        var logs = await readServices.GetRequiredService<ISender>().Send(new GetSessionLogsQuery());
        Assert.True(logs.IsSuccess);
        Assert.Empty(logs.Value);
    }

    [Fact]
    public async Task An_updated_item_is_written_and_read_back_with_its_new_sets()
    {
        var log = Session(SessionDate);
        var item = log.UpsertItem(Item("push_up", "Flexiones", sets: [Set(1, 10), Set(2, 11)])).Value;
        _dbContext.SessionLogs.Add(log);
        await _dbContext.SaveChangesAsync();

        log.UpdateItemSets(item.Id, [Set(1, 12), Set(2, 13, actualRir: 2, loadKg: 2.5)]);
        await _dbContext.SaveChangesAsync();

        var loaded = Assert.Single(await ReadLogsAsync());
        var loadedItem = Assert.Single(loaded.Items);
        Assert.Equal([12, 13], loadedItem.Sets.Select(set => set.Value));
        Assert.Equal(2, loadedItem.Sets.ElementAt(1).ActualRir);
        Assert.Equal(2.5, loadedItem.Sets.ElementAt(1).LoadKg);
    }

    [Fact]
    public async Task A_deleted_session_disappears_together_with_its_items_and_sets()
    {
        var log = Session(SessionDate);
        log.UpsertItem(Item("push_up", "Flexiones", sets: [Set(1, 10), Set(2, 11)]));
        _dbContext.SessionLogs.Add(log);
        await _dbContext.SaveChangesAsync();

        var repository = new SessionLogRepository(_dbContext);
        var loaded = await repository.GetByIdAsync(log.Id);
        Assert.NotNull(loaded);
        repository.Remove(loaded!);
        await _dbContext.SaveChangesAsync();

        Assert.Empty(await ReadLogsAsync());
        Assert.Empty(await _dbContext.SessionLogItems.AsNoTracking().ToListAsync());
    }

    private static readonly Guid MesocycleId = Guid.Parse("33333333-3333-3333-3333-333333333333");

    private static SessionLog Session(
        DateOnly sessionDate,
        SessionLogKind kind = SessionLogKind.Mesocycle,
        Guid? mesocycleId = null,
        int? microcycleNumber = 2,
        int? sessionDay = 1) =>
        SessionLog.Create(
            SingleUser.Id,
            kind,
            sessionDate,
            mesocycleId ?? (kind == SessionLogKind.Mesocycle ? MesocycleId : null),
            microcycleNumber,
            sessionDay,
            Recorded).Value;

    private static SessionLogSetInput Set(
        int setNumber,
        int value,
        int? actualRir = null,
        double? loadKg = null) =>
        new(setNumber, value, actualRir, loadKg);

    private static SessionLogItemInput Item(
        string exerciseId,
        string exerciseName,
        SessionItemRole role = SessionItemRole.Strength,
        ExerciseGroup? pattern = ExerciseGroup.Push,
        Metric metric = Metric.Reps,
        int? repsMin = 8,
        int? repsMax = 12,
        int? holdSecondsMin = null,
        int? holdSecondsMax = null,
        IReadOnlyList<SessionLogSetInput>? sets = null,
        Guid? clientId = null) =>
        new(
            exerciseId,
            exerciseName,
            role,
            pattern,
            metric,
            PrescribedSets: 3,
            repsMin,
            repsMax,
            holdSecondsMin,
            holdSecondsMax,
            Note: null,
            sets ?? [Set(1, 10)],
            clientId);

    private static RegisterSessionLogCommand RegisterCommand(
        string exerciseId,
        IReadOnlyList<SessionLogSetInput>? sets = null,
        SessionItemRole role = SessionItemRole.Strength,
        ExerciseGroup? pattern = ExerciseGroup.Push,
        Guid? clientId = null) =>
        new(
            new SessionLogKeyInput(SessionLogKind.Mesocycle, SessionDate, MesocycleId, 2, 1),
            new SessionLogItemBody(exerciseId, role, pattern, PrescribedSets: 3,
                RepsMin: 8,
                RepsMax: 12,
                HoldSecondsMin: null,
                HoldSecondsMax: null,
                Note: null,
                Sets: sets ?? [Set(1, 10)]),
            clientId);

    private async Task<IReadOnlyList<SessionLog>> ReadLogsAsync()
    {
        await using var readContext = CreateContext();
        var logs = await readContext.SessionLogs
            .AsNoTracking()
            .Include(log => log.Items)
            .ToListAsync();

        // SQLite no ordena por DateOnly en SQL; se ordena en memoria.
        return logs.OrderBy(log => log.RecordedAtUtc).ThenBy(log => log.Id).ToList();
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
        GC.SuppressFinalize(this);
    }
}
