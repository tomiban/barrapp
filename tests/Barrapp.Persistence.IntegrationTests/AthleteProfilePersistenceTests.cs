using System.Globalization;
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

    private static MaximumInput[] Maximums(int pushUp = 10, int pullUp = 0, int squat = 20) =>
    [
        new MaximumInput("push_up", pushUp),
        new MaximumInput("pull_up", pullUp),
        new MaximumInput("squat", squat),
    ];

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
        var profile = AthleteProfile.Create(SingleUser.Id, 78.5, 181, 180, 85, 4, Maximums()).Value;
        _dbContext.AthleteProfiles.Add(profile);
        await _dbContext.SaveChangesAsync();

        var reloaded = await ReadProfileAsync();

        Assert.NotNull(reloaded);
        Assert.Equal(profile.Id, reloaded!.Id);
        Assert.Equal(78.5, reloaded.WeightKilograms);
        Assert.Equal(181, reloaded.HeightCentimeters);
        Assert.Equal(180, reloaded.ArmSpanCentimeters);
        Assert.Equal(85, reloaded.InseamCentimeters);
        Assert.Equal(4, reloaded.TrainingDays);
    }

    [Fact]
    public async Task A_new_profile_is_written_and_read_back_with_the_same_maximums()
    {
        var profile = AthleteProfile.Create(SingleUser.Id, 78.5, 181, 180, 85, 4, Maximums(pushUp: 12, pullUp: 3, squat: 20)).Value;
        _dbContext.AthleteProfiles.Add(profile);
        await _dbContext.SaveChangesAsync();

        var reloaded = await ReadProfileAsync();

        Assert.NotNull(reloaded);
        var maximums = reloaded!.Maximums.ToDictionary(maximum => maximum.ExerciseCode);
        Assert.Equal(3, maximums.Count);
        Assert.Equal(12, maximums["push_up"].Repetitions);
        Assert.Equal(3, maximums["pull_up"].Repetitions);
        Assert.Equal(20, maximums["squat"].Repetitions);
    }

    [Fact]
    public async Task A_zero_maximum_round_trips_through_sqlite()
    {
        var profile = AthleteProfile.Create(SingleUser.Id, 78.5, 181, 180, 85, 4, Maximums(pushUp: 0)).Value;
        _dbContext.AthleteProfiles.Add(profile);
        await _dbContext.SaveChangesAsync();

        var reloaded = await ReadProfileAsync();

        Assert.NotNull(reloaded);
        Assert.Equal(0, reloaded!.Maximums.Single(maximum => maximum.ExerciseCode == "push_up").Repetitions);
    }

    [Fact]
    public async Task The_training_days_column_defaults_to_the_domain_minimum()
    {
        await using var command = _connection.CreateCommand();
        command.CommandText =
            "SELECT dflt_value FROM pragma_table_info('AthleteProfiles') WHERE name = 'TrainingDays'";

        var defaultValue = await command.ExecuteScalarAsync();

        Assert.Equal(AthleteProfile.MinTrainingDays.ToString(), Convert.ToString(defaultValue));
    }

    [Fact]
    public async Task The_lever_measurement_columns_default_to_the_domain_minimums()
    {
        await using var command = _connection.CreateCommand();
        command.CommandText =
            "SELECT name, dflt_value FROM pragma_table_info('AthleteProfiles') "
            + "WHERE name IN ('ArmSpanCentimeters', 'InseamCentimeters')";

        var defaults = new Dictionary<string, double>(StringComparer.Ordinal);
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            defaults[reader.GetString(0)] = double.Parse(
                reader.GetString(1),
                CultureInfo.InvariantCulture);
        }

        Assert.Equal(AthleteProfile.MinArmSpanCentimeters, defaults["ArmSpanCentimeters"]);
        Assert.Equal(AthleteProfile.MinInseamCentimeters, defaults["InseamCentimeters"]);
    }

    [Fact]
    public async Task Updating_a_profile_overwrites_the_stored_measurements_and_maximums()
    {
        var profile = AthleteProfile.Create(SingleUser.Id, 78.5, 181, 180, 85, 4, Maximums()).Value;
        _dbContext.AthleteProfiles.Add(profile);
        await _dbContext.SaveChangesAsync();

        profile.Update(82, 181.5, 190, 90, 5, Maximums(pushUp: 15, pullUp: 1, squat: 0));
        await _dbContext.SaveChangesAsync();

        var reloaded = await ReadProfileAsync();

        Assert.NotNull(reloaded);
        Assert.Equal(82, reloaded!.WeightKilograms);
        Assert.Equal(181.5, reloaded.HeightCentimeters);
        Assert.Equal(190, reloaded.ArmSpanCentimeters);
        Assert.Equal(90, reloaded.InseamCentimeters);
        Assert.Equal(5, reloaded.TrainingDays);
        var maximums = reloaded.Maximums.ToDictionary(maximum => maximum.ExerciseCode);
        Assert.Equal(15, maximums["push_up"].Repetitions);
        Assert.Equal(1, maximums["pull_up"].Repetitions);
        Assert.Equal(0, maximums["squat"].Repetitions);
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
