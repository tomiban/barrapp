using Barrapp.Domain.Common;
using Barrapp.Domain.Knowledge;
using Barrapp.Domain.Planning;
using Barrapp.Domain.Sessions;

namespace Barrapp.Domain.UnitTests;

/// <summary>
/// Historia de la sesión suelta (#29): cada generación deja una <see cref="SessionSuelta"/> con los
/// parámetros con los que se pidió, el foco ya resuelto (qué ha tocado el motor, sobre todo en
/// «sorpréndeme») y la sesión generada como snapshot. La suelta vive en su propio agregado, fuera
/// de los flujos de <see cref="SessionLog"/>: registrar una suelta no crea registros de sesión y
/// no puede tocar ni el mesociclo ni los máximos. Solo se prueba la fábrica por su interfaz
/// pública.
/// </summary>
public sealed class SessionSueltaTests
{
    private static readonly Guid UserId = Guid.Parse("33333333-3333-3333-3333-333333333333");

    [Fact]
    public void Create_with_a_pattern_composition_keeps_parameters_and_snapshot()
    {
        var created = SessionSuelta.Create(
            UserId,
            timeMinutes: 30,
            SoloSessionEnergy.Medium,
            SoloSessionFocus.Pattern,
            ExerciseGroup.Push,
            skillId: null,
            new DateTimeOffset(2026, 10, 5, 18, 30, 0, TimeSpan.Zero),
            Items());

        Assert.True(created.IsSuccess);
        var suelta = created.Value;
        Assert.NotEqual(Guid.Empty, suelta.Id);
        Assert.Equal(UserId, suelta.UserId);
        Assert.Equal(30, suelta.TimeMinutes);
        Assert.Equal(SoloSessionEnergy.Medium, suelta.Energy);
        Assert.Equal(SoloSessionFocus.Pattern, suelta.Focus);
        Assert.Equal(ExerciseGroup.Push, suelta.Pattern);
        Assert.Null(suelta.SkillId);
        Assert.Equal(SessionSueltaStatus.Generated, suelta.Status);
        Assert.Null(suelta.RecordedAtUtc);
        Assert.Equal(new DateTimeOffset(2026, 10, 5, 18, 30, 0, TimeSpan.Zero), suelta.CreatedAtUtc);

        // El snapshot guarda las filas en orden, sin perder la prescripción.
        Assert.Equal(2, suelta.Items.Count);
        Assert.Equal("push_up", suelta.Items.ElementAt(0).ExerciseId);
        Assert.Equal(SessionItemRole.Strength, suelta.Items.ElementAt(0).Role);
        Assert.Equal(ExerciseGroup.Push, suelta.Items.ElementAt(0).Pattern);
        Assert.Equal(8, suelta.Items.ElementAt(0).RepsMax);
        Assert.Equal("hollow-body-hold", suelta.Items.ElementAt(1).ExerciseId);
        Assert.Equal(30, suelta.Items.ElementAt(1).HoldSecondsMax);
    }

    [Fact]
    public void Create_with_a_skill_composition_sets_the_skill()
    {
        var created = SessionSuelta.Create(
            UserId,
            timeMinutes: 45,
            SoloSessionEnergy.High,
            SoloSessionFocus.Skill,
            pattern: null,
            skillId: "planche",
            new DateTimeOffset(2026, 10, 5, 19, 0, 0, TimeSpan.Zero),
            Items());

        Assert.True(created.IsSuccess);
        Assert.Null(created.Value.Pattern);
        Assert.Equal("planche", created.Value.SkillId);
    }

    [Fact]
    public void Create_without_a_composition_fails()
    {
        var created = SessionSuelta.Create(
            UserId,
            30,
            SoloSessionEnergy.Medium,
            SoloSessionFocus.Surprise,
            pattern: null,
            skillId: null,
            new DateTimeOffset(2026, 10, 5, 18, 30, 0, TimeSpan.Zero),
            Items());

        Assert.True(created.IsFailure);
        Assert.Equal("session_suelta.composition_required", created.Error.Code);
    }

    [Fact]
    public void Create_with_both_pattern_and_skill_fails()
    {
        var created = SessionSuelta.Create(
            UserId,
            30,
            SoloSessionEnergy.Medium,
            SoloSessionFocus.Surprise,
            ExerciseGroup.Push,
            "planche",
            new DateTimeOffset(2026, 10, 5, 18, 30, 0, TimeSpan.Zero),
            Items());

        Assert.True(created.IsFailure);
        Assert.Equal("session_suelta.composition_conflict", created.Error.Code);
    }

    [Fact]
    public void Create_without_items_fails()
    {
        var created = SessionSuelta.Create(
            UserId,
            30,
            SoloSessionEnergy.Medium,
            SoloSessionFocus.Pattern,
            ExerciseGroup.Push,
            skillId: null,
            new DateTimeOffset(2026, 10, 5, 18, 30, 0, TimeSpan.Zero),
            []);

        Assert.True(created.IsFailure);
        Assert.Equal("session_suelta.items_required", created.Error.Code);
    }

    [Theory]
    [InlineData(10)]
    [InlineData(20)]
    [InlineData(90)]
    public void Create_with_an_invalid_time_fails(int timeMinutes)
    {
        var created = SessionSuelta.Create(
            UserId,
            timeMinutes,
            SoloSessionEnergy.Medium,
            SoloSessionFocus.Pattern,
            ExerciseGroup.Push,
            skillId: null,
            new DateTimeOffset(2026, 10, 5, 18, 30, 0, TimeSpan.Zero),
            Items());

        Assert.True(created.IsFailure);
        Assert.Equal("session_suelta.time_minutes_out_of_range", created.Error.Code);
    }

    [Fact]
    public void Create_with_an_item_without_a_range_fails()
    {
        var created = SessionSuelta.Create(
            UserId,
            30,
            SoloSessionEnergy.Medium,
            SoloSessionFocus.Pattern,
            ExerciseGroup.Push,
            skillId: null,
            new DateTimeOffset(2026, 10, 5, 18, 30, 0, TimeSpan.Zero),
            [new SessionSueltaItemInput("push_up", SessionItemRole.Strength, ExerciseGroup.Push, 3, null, null, null, null)]);

        Assert.True(created.IsFailure);
        Assert.Equal("session_suelta.item_requires_range", created.Error.Code);
    }

    [Fact]
    public void Create_with_an_item_without_an_exercise_fails()
    {
        var created = SessionSuelta.Create(
            UserId,
            30,
            SoloSessionEnergy.Medium,
            SoloSessionFocus.Pattern,
            ExerciseGroup.Push,
            skillId: null,
            new DateTimeOffset(2026, 10, 5, 18, 30, 0, TimeSpan.Zero),
            [new SessionSueltaItemInput("", SessionItemRole.Strength, ExerciseGroup.Push, 3, 5, 8, null, null)]);

        Assert.True(created.IsFailure);
        Assert.Equal("session_suelta.item_exercise_required", created.Error.Code);
    }

    [Fact]
    public void Mark_recorded_changes_the_status_and_stamps_the_moment()
    {
        var suelta = SessionSuelta.Create(
            UserId,
            30,
            SoloSessionEnergy.Medium,
            SoloSessionFocus.Pattern,
            ExerciseGroup.Push,
            skillId: null,
            new DateTimeOffset(2026, 10, 5, 18, 30, 0, TimeSpan.Zero),
            Items()).Value;

        var recorded = suelta.MarkRecorded(new DateTimeOffset(2026, 10, 5, 19, 45, 0, TimeSpan.Zero));

        Assert.True(recorded.IsSuccess);
        Assert.Equal(SessionSueltaStatus.Recorded, suelta.Status);
        Assert.Equal(new DateTimeOffset(2026, 10, 5, 19, 45, 0, TimeSpan.Zero), suelta.RecordedAtUtc);
    }

    [Fact]
    public void Mark_recorded_twice_fails()
    {
        var suelta = SessionSuelta.Create(
            UserId,
            30,
            SoloSessionEnergy.Medium,
            SoloSessionFocus.Pattern,
            ExerciseGroup.Push,
            skillId: null,
            new DateTimeOffset(2026, 10, 5, 18, 30, 0, TimeSpan.Zero),
            Items()).Value;
        suelta.MarkRecorded(new DateTimeOffset(2026, 10, 5, 19, 45, 0, TimeSpan.Zero));

        var again = suelta.MarkRecorded(new DateTimeOffset(2026, 10, 5, 20, 0, 0, TimeSpan.Zero));

        Assert.True(again.IsFailure);
        Assert.Equal("session_suelta.already_recorded", again.Error.Code);
        Assert.Equal(new DateTimeOffset(2026, 10, 5, 19, 45, 0, TimeSpan.Zero), suelta.RecordedAtUtc);
    }

    private static IReadOnlyCollection<SessionSueltaItemInput> Items() =>
    [
        new("push_up", SessionItemRole.Strength, ExerciseGroup.Push, 3, 5, 8, null, null),
        new("hollow-body-hold", SessionItemRole.Core, null, 3, null, null, 20, 30),
    ];
}
