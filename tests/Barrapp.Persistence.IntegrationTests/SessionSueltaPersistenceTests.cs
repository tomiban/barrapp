using Barrapp.Application.Abstractions;
using Barrapp.Application.Common;
using Barrapp.Domain.Knowledge;
using Barrapp.Domain.Planning;
using Barrapp.Domain.Sessions;
using Barrapp.Persistence.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Barrapp.Persistence.IntegrationTests;

/// <summary>
/// Round-trip real de la sesión suelta contra SQLite (#29): el esquema se crea aplicando las
/// migraciones (el mismo camino que producción) y el snapshot viaja como objetos valor owned en su
/// propia tabla. El aislamiento proclamado en el dominio se comprueba aquí abajo: guardar una
/// suelta no toca la tabla de registros de sesión.
/// </summary>
public sealed class SessionSueltaPersistenceTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly ApplicationDbContext _dbContext;

    public SessionSueltaPersistenceTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        _dbContext = CreateContext();
        _dbContext.Database.Migrate();
    }

    [Fact]
    public async Task A_session_suelta_is_written_and_read_back_with_its_items_in_order()
    {
        var created = SessionSuelta.Create(
            SingleUser.Id,
            timeMinutes: 30,
            SoloSessionEnergy.Medium,
            SoloSessionFocus.Pattern,
            ExerciseGroup.Push,
            skillId: null,
            new DateTimeOffset(2026, 10, 5, 18, 30, 0, TimeSpan.Zero),
            Items());

        _dbContext.SessionSuelta.Add(created.Value);
        await _dbContext.SaveChangesAsync();
        var suelta = created.Value;

        await using var readContext = CreateContext();
        var repository = new SessionSueltaRepository(readContext);
        var reloaded = await repository.GetByIdAsync(suelta.Id, SingleUser.Id, CancellationToken.None);

        Assert.NotNull(reloaded);
        Assert.Equal(suelta.Id, reloaded!.Id);
        Assert.Equal(30, reloaded.TimeMinutes);
        Assert.Equal(SoloSessionEnergy.Medium, reloaded.Energy);
        Assert.Equal(SoloSessionFocus.Pattern, reloaded.Focus);
        Assert.Equal(ExerciseGroup.Push, reloaded.Pattern);
        Assert.Null(reloaded.SkillId);
        Assert.Equal(SessionSueltaStatus.Generated, reloaded.Status);
        Assert.Null(reloaded.RecordedAtUtc);
        Assert.Equal(new DateTimeOffset(2026, 10, 5, 18, 30, 0, TimeSpan.Zero), reloaded.CreatedAtUtc);

        // El snapshot conserva las filas y su orden (la posición es la clave junto a la suelta).
        Assert.Equal(2, reloaded.Items.Count);
        Assert.Equal("push_up", reloaded.Items.ElementAt(0).ExerciseId);
        Assert.Equal(SessionItemRole.Strength, reloaded.Items.ElementAt(0).Role);
        Assert.Equal(ExerciseGroup.Push, reloaded.Items.ElementAt(0).Pattern);
        Assert.Equal(8, reloaded.Items.ElementAt(0).RepsMax);
        Assert.Equal("hollow-body-hold", reloaded.Items.ElementAt(1).ExerciseId);
        Assert.Equal(30, reloaded.Items.ElementAt(1).HoldSecondsMax);
    }

    [Fact]
    public async Task A_recorded_session_suelta_keeps_its_status_after_reload()
    {
        var created = SessionSuelta.Create(
            SingleUser.Id,
            timeMinutes: 45,
            SoloSessionEnergy.High,
            SoloSessionFocus.Skill,
            pattern: null,
            skillId: "planche",
            new DateTimeOffset(2026, 10, 5, 18, 30, 0, TimeSpan.Zero),
            Items());
        var recorded = created.Value.MarkRecorded(new DateTimeOffset(2026, 10, 5, 19, 45, 0, TimeSpan.Zero));
        Assert.True(recorded.IsSuccess);

        _dbContext.SessionSuelta.Add(created.Value);
        await _dbContext.SaveChangesAsync();

        await using var readContext = CreateContext();
        var repository = new SessionSueltaRepository(readContext);
        var reloaded = await repository.GetByIdAsync(created.Value.Id, SingleUser.Id, CancellationToken.None);

        Assert.NotNull(reloaded);
        Assert.Equal(SessionSueltaStatus.Recorded, reloaded!.Status);
        Assert.Equal(new DateTimeOffset(2026, 10, 5, 19, 45, 0, TimeSpan.Zero), reloaded.RecordedAtUtc);
    }

    [Fact]
    public async Task Saving_a_session_suelta_does_not_touch_the_session_log_table()
    {
        var created = SessionSuelta.Create(
            SingleUser.Id,
            timeMinutes: 30,
            SoloSessionEnergy.Medium,
            SoloSessionFocus.Pattern,
            ExerciseGroup.Push,
            skillId: null,
            new DateTimeOffset(2026, 10, 5, 18, 30, 0, TimeSpan.Zero),
            Items());

        _dbContext.SessionSuelta.Add(created.Value);
        await _dbContext.SaveChangesAsync();

        await using var readContext = CreateContext();
        var logs = await readContext.SessionLogs.ToListAsync();

        Assert.Empty(logs);
    }

    private static IReadOnlyCollection<SessionSueltaItemInput> Items() =>
    [
        new("push_up", SessionItemRole.Strength, ExerciseGroup.Push, 3, 5, 8, null, null),
        new("hollow-body-hold", SessionItemRole.Core, null, 3, null, null, 20, 30),
    ];

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
