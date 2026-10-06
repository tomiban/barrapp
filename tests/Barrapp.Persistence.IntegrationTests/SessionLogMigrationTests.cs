using Barrapp.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Barrapp.Persistence.IntegrationTests;

public sealed class SessionLogMigrationTests : IDisposable
{
    private const string PreviousMigration = "20261006043121_AddSessionLogClientIdAndIndexes";
    private const string AggregateMigration = "20261006173010_SessionLogSessionAggregate";
    private readonly SqliteConnection _connection = new("Data Source=:memory:");
    private readonly ApplicationDbContext _dbContext;

    public SessionLogMigrationTests()
    {
        _connection.Open();
        _dbContext = new ApplicationDbContext(
            new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseSqlite(_connection)
                .Options);
    }

    [Fact]
    public async Task Aggregate_migration_preserves_legacy_rows_and_downgrade_restores_original_tables()
    {
        var migrator = _dbContext.Database.GetMigrations();
        Assert.Contains(PreviousMigration, migrator);

        await _dbContext.Database.MigrateAsync(PreviousMigration);
        await _dbContext.Database.ExecuteSqlRawAsync(
            "INSERT INTO SessionLogs (Id, UserId, ExerciseId, MesocycleId, SessionDay, RecordedAtUtc, ClientId) " +
            "VALUES ('11111111-1111-1111-1111-111111111111', '00000000-0000-0000-0000-000000000001', 'push_up', NULL, 2, '2026-10-05T18:30:00+00:00', '22222222-2222-2222-2222-222222222222');");
        await _dbContext.Database.ExecuteSqlRawAsync(
            "INSERT INTO SessionLogSets (SessionLogId, SetNumber, Value, Effort) " +
            "VALUES ('11111111-1111-1111-1111-111111111111', 1, 10, 2);");

        await _dbContext.Database.MigrateAsync();

        Assert.Equal(1, await CountAsync("LegacySessionLogs"));
        Assert.Equal(1, await CountAsync("LegacySessionLogSets"));
        Assert.Equal(0, await CountAsync("SessionLogs"));

        await _dbContext.Database.ExecuteSqlRawAsync(
            "INSERT INTO SessionLogs (Id, UserId, Kind, SessionDate, MesocycleId, MicrocycleNumber, SessionDay, RecordedAtUtc, CompletedAtUtc) " +
            "VALUES ('33333333-3333-3333-3333-333333333333', '00000000-0000-0000-0000-000000000001', 0, '2026-10-06', NULL, 1, 2, '2026-10-06T18:30:00+00:00', NULL);");
        await _dbContext.Database.ExecuteSqlRawAsync(
            "INSERT INTO SessionLogItems (Id, SessionLogId, Position, ExerciseId, ExerciseName, Role, Pattern, Metric, PrescribedSets, RepsMin, RepsMax, HoldSecondsMin, HoldSecondsMax, Note, ClientId) " +
            "VALUES ('44444444-4444-4444-4444-444444444444', '33333333-3333-3333-3333-333333333333', 1, 'pull_up', 'Dominadas', 1, 1, 0, 3, 5, 8, NULL, NULL, NULL, NULL);");
        await _dbContext.Database.ExecuteSqlRawAsync(
            "INSERT INTO SessionLogSets (SessionLogItemId, SetNumber, Value, ActualRir, LoadKg) " +
            "VALUES ('44444444-4444-4444-4444-444444444444', 1, 6, 2, NULL);");

        await _dbContext.Database.MigrateAsync(PreviousMigration);

        Assert.Equal(2, await CountAsync("SessionLogs"));
        Assert.Equal(2, await CountAsync("SessionLogSets"));
        Assert.Equal(10, await _dbContext.Database.SqlQueryRaw<int>(
            "SELECT Value AS Value FROM SessionLogSets WHERE SessionLogId = '11111111-1111-1111-1111-111111111111'").SingleAsync());
        Assert.Equal(6, await _dbContext.Database.SqlQueryRaw<int>(
            "SELECT Value AS Value FROM SessionLogSets WHERE SessionLogId = '44444444-4444-4444-4444-444444444444'").SingleAsync());
    }

    private Task<int> CountAsync(string tableName) => tableName switch
    {
        "LegacySessionLogs" => _dbContext.Database.SqlQueryRaw<int>("SELECT COUNT(*) AS Value FROM LegacySessionLogs").SingleAsync(),
        "LegacySessionLogSets" => _dbContext.Database.SqlQueryRaw<int>("SELECT COUNT(*) AS Value FROM LegacySessionLogSets").SingleAsync(),
        "SessionLogs" => _dbContext.Database.SqlQueryRaw<int>("SELECT COUNT(*) AS Value FROM SessionLogs").SingleAsync(),
        "SessionLogSets" => _dbContext.Database.SqlQueryRaw<int>("SELECT COUNT(*) AS Value FROM SessionLogSets").SingleAsync(),
        _ => throw new ArgumentOutOfRangeException(nameof(tableName)),
    };

    public void Dispose()
    {
        _dbContext.Dispose();
        _connection.Dispose();
    }
}