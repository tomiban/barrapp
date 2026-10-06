using Barrapp.Domain.Common;
using Barrapp.Domain.Sessions;

namespace Barrapp.Domain.UnitTests;

/// <summary>
/// El registro de sesión guarda, serie a serie, lo realmente ejecutado en un ejercicio de una
/// sesión del plan (spec 0001, US-34; decisión D5). El valor es un número —repeticiones o
/// segundos según el ejercicio— y la unidad la deriva el motor, no el cliente.
/// </summary>
public sealed class SessionLogTests
{
    private static readonly Guid UserId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly DateTimeOffset Recorded = new(2026, 10, 5, 18, 30, 0, TimeSpan.Zero);

    private static SessionLogSetInput[] Sets(params SessionLogSetInput[] sets) => sets;

    [Fact]
    public void Create_registers_each_set_in_order_with_its_value()
    {
        var result = SessionLog.Create(
            UserId,
            "push_up",
            mesocycleId: null,
            sessionDay: 2,
            Recorded,
            Sets(new SessionLogSetInput(1, 10), new SessionLogSetInput(2, 11), new SessionLogSetInput(3, 9)));

        Assert.True(result.IsSuccess);
        Assert.Equal([1, 2, 3], result.Value.Sets.Select(set => set.SetNumber));
        Assert.Equal([10, 11, 9], result.Value.Sets.Select(set => set.Value));
    }

    [Fact]
    public void Create_keeps_the_session_identity_and_an_optional_effort()
    {
        var result = SessionLog.Create(
            UserId,
            "pull_up",
            mesocycleId: Guid.NewGuid(),
            sessionDay: 1,
            Recorded,
            Sets(new SessionLogSetInput(1, 5, null), new SessionLogSetInput(2, 6, 2)));

        Assert.True(result.IsSuccess);
        var log = result.Value;
        Assert.Equal(UserId, log.UserId);
        Assert.Equal("pull_up", log.ExerciseId);
        Assert.NotNull(log.MesocycleId);
        Assert.Equal(1, log.SessionDay);
        Assert.Equal(Recorded, log.RecordedAtUtc);
        Assert.Null(log.Sets.ElementAt(0).Effort);
        Assert.Equal(2, log.Sets.ElementAt(1).Effort);
    }

    [Fact]
    public void Create_accepts_a_zero_actual_value()
    {
        var result = SessionLog.Create(
            UserId,
            "push_up",
            null,
            1,
            Recorded,
            Sets(new SessionLogSetInput(1, 0)));

        Assert.True(result.IsSuccess);
        Assert.Equal(0, result.Value.Sets.Single().Value);
    }

    [Fact]
    public void Create_registers_the_seconds_held_per_set_in_a_hold()
    {
        var result = SessionLog.Create(
            UserId,
            "hollow-body-hold",
            null,
            1,
            Recorded,
            Sets(new SessionLogSetInput(1, 20), new SessionLogSetInput(2, 25), new SessionLogSetInput(3, 30)));

        Assert.True(result.IsSuccess);
        Assert.Equal([20, 25, 30], result.Value.Sets.Select(set => set.Value));
    }

    [Fact]
    public void Create_rejects_a_session_without_sets()
    {
        var result = SessionLog.Create(UserId, "push_up", null, 1, Recorded, []);

        Assert.True(result.IsFailure);
        Assert.Equal(DomainErrors.SessionLog.SetsRequired, result.Error);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(int.MinValue)]
    public void Create_rejects_a_negative_actual_value(int value)
    {
        var result = SessionLog.Create(
            UserId,
            "push_up",
            null,
            1,
            Recorded,
            Sets(new SessionLogSetInput(1, value)));

        Assert.True(result.IsFailure);
        Assert.Equal(DomainErrors.SessionLog.ValueMustBeNonNegative, result.Error);
    }

    [Fact]
    public void Create_rejects_set_numbers_that_skip_a_position()
    {
        var result = SessionLog.Create(
            UserId,
            "push_up",
            null,
            1,
            Recorded,
            Sets(new SessionLogSetInput(1, 8), new SessionLogSetInput(3, 8)));

        Assert.True(result.IsFailure);
        Assert.Equal(DomainErrors.SessionLog.SetNumbersNotConsecutive, result.Error);
    }

    [Fact]
    public void Create_rejects_set_numbers_that_start_below_one()
    {
        var result = SessionLog.Create(
            UserId,
            "push_up",
            null,
            1,
            Recorded,
            Sets(new SessionLogSetInput(0, 8), new SessionLogSetInput(1, 8)));

        Assert.True(result.IsFailure);
        Assert.Equal(DomainErrors.SessionLog.SetNumberOutOfRange, result.Error);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(11)]
    public void Create_rejects_an_effort_that_escapes_from_zero_to_ten(int effort)
    {
        var result = SessionLog.Create(
            UserId,
            "push_up",
            null,
            1,
            Recorded,
            Sets(new SessionLogSetInput(1, 8, effort)));

        Assert.True(result.IsFailure);
        Assert.Equal(DomainErrors.SessionLog.EffortOutOfRange, result.Error);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(10)]
    public void Create_accepts_an_effort_at_the_boundaries(int effort)
    {
        var result = SessionLog.Create(
            UserId,
            "push_up",
            null,
            1,
            Recorded,
            Sets(new SessionLogSetInput(1, 8, effort)));

        Assert.True(result.IsSuccess);
        Assert.Equal(effort, result.Value.Sets.Single().Effort);
    }

    [Fact]
    public void Create_rejects_a_session_day_below_one()
    {
        var result = SessionLog.Create(
            UserId,
            "push_up",
            null,
            sessionDay: 0,
            Recorded,
            Sets(new SessionLogSetInput(1, 8)));

        Assert.True(result.IsFailure);
        Assert.Equal(DomainErrors.SessionLog.SessionDayOutOfRange, result.Error);
    }

    [Fact]
    public void Update_replaces_the_set_values_of_an_existing_log()
    {
        var result = SessionLog.Create(
            UserId,
            "push_up",
            null,
            1,
            Recorded,
            Sets(new SessionLogSetInput(1, 10), new SessionLogSetInput(2, 11), new SessionLogSetInput(3, 9)));
        var log = result.Value;

        var update = log.Update(
            Sets(new SessionLogSetInput(1, 12), new SessionLogSetInput(2, 13, 2), new SessionLogSetInput(3, 10)));

        Assert.True(update.IsSuccess);
        Assert.Equal([12, 13, 10], log.Sets.Select(set => set.Value));
        Assert.Equal([null, 2, null], log.Sets.Select(set => set.Effort).ToArray());
        Assert.Equal(1, log.SessionDay);
    }

    [Fact]
    public void Update_rejects_a_log_without_sets_and_keeps_its_values()
    {
        var result = SessionLog.Create(
            UserId,
            "push_up",
            null,
            1,
            Recorded,
            Sets(new SessionLogSetInput(1, 10), new SessionLogSetInput(2, 11)));
        var log = result.Value;

        var update = log.Update([]);

        Assert.True(update.IsFailure);
        Assert.Equal(DomainErrors.SessionLog.SetsRequired, update.Error);
        Assert.Equal([10, 11], log.Sets.Select(set => set.Value));
    }

    [Fact]
    public void Update_rejects_a_negative_value_and_keeps_the_previous_sets()
    {
        var result = SessionLog.Create(
            UserId,
            "push_up",
            null,
            1,
            Recorded,
            Sets(new SessionLogSetInput(1, 10)));
        var log = result.Value;

        var update = log.Update(Sets(new SessionLogSetInput(1, -2)));

        Assert.True(update.IsFailure);
        Assert.Equal(DomainErrors.SessionLog.ValueMustBeNonNegative, update.Error);
        Assert.Equal([10], log.Sets.Select(set => set.Value));
    }

    [Fact]
    public void Update_rejects_non_consecutive_set_numbers_and_keeps_the_previous_sets()
    {
        var result = SessionLog.Create(
            UserId,
            "push_up",
            null,
            1,
            Recorded,
            Sets(new SessionLogSetInput(1, 10), new SessionLogSetInput(2, 11)));
        var log = result.Value;

        var update = log.Update(Sets(new SessionLogSetInput(1, 12), new SessionLogSetInput(3, 13)));

        Assert.True(update.IsFailure);
        Assert.Equal(DomainErrors.SessionLog.SetNumbersNotConsecutive, update.Error);
        Assert.Equal([10, 11], log.Sets.Select(set => set.Value));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(11)]
    public void Update_rejects_an_effort_out_of_range_and_keeps_the_previous_sets(int effort)
    {
        var result = SessionLog.Create(
            UserId,
            "push_up",
            null,
            1,
            Recorded,
            Sets(new SessionLogSetInput(1, 10)));
        var log = result.Value;

        var update = log.Update(Sets(new SessionLogSetInput(1, 12, effort)));

        Assert.True(update.IsFailure);
        Assert.Equal(DomainErrors.SessionLog.EffortOutOfRange, update.Error);
        Assert.Equal([10], log.Sets.Select(set => set.Value));
    }
}
