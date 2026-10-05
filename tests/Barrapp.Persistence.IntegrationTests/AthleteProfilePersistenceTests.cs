using Barrapp.Application.Common;
using Barrapp.Domain.Athlete;
using Barrapp.Persistence.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Barrapp.Persistence.IntegrationTests;

/// <summary>
/// Round-trip real contra SQLite. El esquema se crea aplicando las migraciones (el mismo camino
/// que producción) sobre una base en memoria, y cada lectura usa un contexto nuevo para no
/// depender de la caché del contexto que escribió.
/// </summary>
public sealed class AthleteProfilePersistenceTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly ApplicationDbContext _dbContext;

    public AthleteProfilePersistenceTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        _dbContext = CreateContext();
        _dbContext.Database.Migrate();
    }

    [Fact]
    public async Task There_is_no_profile_before_one_is_saved()
    {
        var repository = new AthleteProfileRepository(CreateContext());

        var loaded = await repository.GetByUserIdAsync(SingleUser.Id, CancellationToken.None);

        Assert.Null(loaded);
    }

    [Fact]
    public async Task A_new_profile_is_written_and_read_back_with_the_same_measurements()
    {
        var profile = AthleteProfile.Create(SingleUser.Id, 78.5, 181).Value;
        _dbContext.AthleteProfiles.Add(profile);
        await _dbContext.SaveChangesAsync();

        var reloaded = await ReadProfileAsync();

        Assert.NotNull(reloaded);
        Assert.Equal(profile.Id, reloaded!.Id);
        Assert.Equal(78.5, reloaded.WeightKilograms);
        Assert.Equal(181, reloaded.HeightCentimeters);
    }

    [Fact]
    public async Task Updating_a_profile_overwrites_the_stored_measurements()
    {
        var profile = AthleteProfile.Create(SingleUser.Id, 78.5, 181).Value;
        _dbContext.AthleteProfiles.Add(profile);
        await _dbContext.SaveChangesAsync();

        profile.Update(82, 181.5);
        await _dbContext.SaveChangesAsync();

        var reloaded = await ReadProfileAsync();

        Assert.NotNull(reloaded);
        Assert.Equal(82, reloaded!.WeightKilograms);
        Assert.Equal(181.5, reloaded.HeightCentimeters);
    }

    private async Task<AthleteProfile?> ReadProfileAsync()
    {
        await using var readContext = CreateContext();
        var repository = new AthleteProfileRepository(readContext);
        return await repository.GetByUserIdAsync(SingleUser.Id, CancellationToken.None);
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
