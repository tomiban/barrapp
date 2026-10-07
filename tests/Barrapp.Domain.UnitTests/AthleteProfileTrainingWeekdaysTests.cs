using Barrapp.Domain.Athlete;
using Barrapp.Domain.Common;

namespace Barrapp.Domain.UnitTests;

/// <summary>
/// Reglas del calendario del mesociclo (#94): el perfil guarda los <b>días de la semana</b> que el
/// atleta entrena, no solo cuántos (3–5), y esos días son los que el motor reparte en las sesiones.
/// Solo se prueba por la interfaz pública del perfil.
/// </summary>
public sealed class AthleteProfileTrainingWeekdaysTests
{
    private static readonly Guid UserId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    private static MaximumInput[] ValidMaximums() =>
    [
        new MaximumInput("push_up", 10),
        new MaximumInput("pull_up", 3),
        new MaximumInput("squat", 20),
    ];

    private static Result<AthleteProfile> Create(int trainingDays, IReadOnlyCollection<DayOfWeek>? weekdays) =>
        AthleteProfile.Create(UserId, 78.5, 181, 180, 85, trainingDays, ValidMaximums(), weekdays);

    [Fact]
    public void Create_keeps_the_training_weekdays_the_athlete_chose()
    {
        var result = Create(
            3,
            [DayOfWeek.Tuesday, DayOfWeek.Thursday, DayOfWeek.Saturday]);

        Assert.True(result.IsSuccess);
        Assert.Equal(
            [DayOfWeek.Tuesday, DayOfWeek.Thursday, DayOfWeek.Saturday],
            result.Value.TrainingWeekdays.Select(weekday => weekday.Day));
    }

    [Fact]
    public void Create_rejects_a_repeated_weekday()
    {
        var result = Create(3, [DayOfWeek.Tuesday, DayOfWeek.Tuesday, DayOfWeek.Thursday]);

        Assert.True(result.IsFailure);
        Assert.Equal(DomainErrors.AthleteProfile.DuplicateTrainingWeekday, result.Error);
    }

    [Fact]
    public void Create_rejects_weekdays_that_are_not_as_many_as_the_training_days()
    {
        var result = Create(4, [DayOfWeek.Tuesday, DayOfWeek.Thursday]);

        Assert.True(result.IsFailure);
        Assert.Equal(DomainErrors.AthleteProfile.TrainingWeekdaysMismatch, result.Error);
    }

    [Theory]
    [InlineData(3, new[] { DayOfWeek.Monday, DayOfWeek.Wednesday, DayOfWeek.Friday })]
    [InlineData(4, new[] { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Thursday, DayOfWeek.Friday })]
    [InlineData(
        5,
        new[]
        {
            DayOfWeek.Monday,
            DayOfWeek.Tuesday,
            DayOfWeek.Wednesday,
            DayOfWeek.Thursday,
            DayOfWeek.Friday,
        })]
    public void Create_falls_back_to_the_default_weekdays_of_the_frequency(
        int trainingDays,
        DayOfWeek[] expected)
    {
        var result = Create(trainingDays, weekdays: null);

        Assert.True(result.IsSuccess);
        Assert.Equal(expected, result.Value.TrainingDaysInOrder());
    }

    [Fact]
    public void Update_replaces_the_training_weekdays()
    {
        var profile = Create(3, null).Value;

        var update = profile.Update(
            78.5,
            181,
            180,
            85,
            4,
            ValidMaximums(),
            [DayOfWeek.Monday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Saturday]);

        Assert.True(update.IsSuccess);
        Assert.Equal(
            [DayOfWeek.Monday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Saturday],
            profile.TrainingDaysInOrder());
    }
}
