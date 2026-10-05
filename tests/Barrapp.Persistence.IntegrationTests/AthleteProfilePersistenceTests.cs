using Barrapp.Application.Common;
using Barrapp.Domain.Athlete;
using Barrapp.Persistence.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Barrapp.Persistence.IntegrationTests;

/// <summary>
/// Round-trip real contra SQLite (en memoria, una conexión por test): escribe un perfil por el
/// repositorio y lo vuelve a leer limpio del contexto para comprobar que se conserva.
/// </summary>
public sealed class AthleteProfilePersistenceTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly ApplicationDbContext _dbContext;

    public AthleteProfilePersistenceTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(_connection)
            .Options;

        _dbContext = new ApplicationDbContext(options);
        _dbContext.Database.EnsureCreated();
    }

    [Fact]
    public async Task There_is_no_profile_before_one_is_saved()
    {
        var repository = new AthleteProfileRepository(_dbContext);

        var loaded = await repository.GetByUserIdAsync(SingleUser.Id, CancellationToken.None);

        Assert.Null(loaded);
    }

    [Fact]
    public async Task A_new_profile_is_written_and_read_back_with_the_same_measurements()
    {
        var repository = new AthleteProfileRepository(_dbContext);
        var profile = AthleteProfile.Create(SingleUser.Id, 78.5, 181).Value;

        repository.Add(profile);
        await _dbContext.SaveChangesAsync();
        _dbContext.ChangeTracker.Clear();

        var loaded = await repository.GetByUserIdAsync(SingleUser.Id, CancellationToken.None);

        Assert.NotNull(loaded);
        Assert.Equal(profile.Id, loaded!.Id);
        Assert.Equal(78.5, loaded.WeightKilograms);
        Assert.Equal(181, loaded.HeightCentimeters);
    }

    [Fact]
    public async Task Updating_a_profile_overwrites_the_stored_measurements()
    {
        var repository = new AthleteProfileRepository(_dbContext);
        var profile = AthleteProfile.Create(SingleUser.Id, 78.5, 181).Value;
        repository.Add(profile);
        await _dbContext.SaveChangesAsync();

        profile.Update(82, 181.5);
        await _dbContext.SaveChangesAsync();
        _dbContext.ChangeTracker.Clear();

        var loaded = await repository.GetByUserIdAsync(SingleUser.Id, CancellationToken.None);

        Assert.NotNull(loaded);
        Assert.Equal(82, loaded!.WeightKilograms);
        Assert.Equal(181.5, loaded.HeightCentimeters);
    }

    public void Dispose()
    {
        _dbContext.Dispose();
        _connection.Dispose();
    }
}
