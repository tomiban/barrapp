using Barrapp.Domain.Athlete;
using Barrapp.Domain.Common;

namespace Barrapp.Domain.UnitTests;

/// <summary>
/// Reglas del calendario del mesociclo (#94): el perfil guarda la <b>fecha de inicio</b> que el
/// atleta elige para arrancar su mesociclo. Sin fecha elegida, el perfil queda sin anclar y es la
/// aplicación (al generar el plan) la que decide arrancar hoy. Solo se prueba por la interfaz
/// pública del perfil.
/// </summary>
public sealed class AthleteProfileStartDateTests
{
    private static readonly Guid UserId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    private static readonly DateOnly ChosenStartDate = new(2026, 10, 20);

    private static MaximumInput[] ValidMaximums() =>
    [
        new MaximumInput("push_up", 10),
        new MaximumInput("pull_up", 3),
        new MaximumInput("squat", 20),
    ];

    private static Result<AthleteProfile> Create(DateOnly? startDate) =>
        AthleteProfile.Create(UserId, 78.5, 181, 180, 85, 3, ValidMaximums(), startDate: startDate);

    [Fact]
    public void Create_keeps_the_start_date_the_athlete_chose()
    {
        var result = Create(ChosenStartDate);

        Assert.True(result.IsSuccess);
        Assert.Equal(ChosenStartDate, result.Value.StartDate);
    }

    [Fact]
    public void Create_without_a_start_date_leaves_the_mesocycle_unanchored()
    {
        var result = Create(startDate: null);

        Assert.True(result.IsSuccess);
        Assert.Null(result.Value.StartDate);
    }

    [Fact]
    public void Update_replaces_the_start_date()
    {
        var profile = Create(ChosenStartDate).Value;

        var update = profile.Update(
            78.5,
            181,
            180,
            85,
            3,
            ValidMaximums(),
            startDate: new DateOnly(2026, 11, 2));

        Assert.True(update.IsSuccess);
        Assert.Equal(new DateOnly(2026, 11, 2), profile.StartDate);
    }

    [Fact]
    public void Update_without_a_start_date_clears_the_previous_choice()
    {
        var profile = Create(ChosenStartDate).Value;

        var update = profile.Update(78.5, 181, 180, 85, 3, ValidMaximums());

        Assert.True(update.IsSuccess);
        Assert.Null(profile.StartDate);
    }
}
