using Barrapp.Domain.Common;
using Barrapp.Domain.Knowledge;
using Barrapp.Domain.Planning;
using Barrapp.Domain.Sessions;

namespace Barrapp.Domain.UnitTests;

/// <summary>
/// El registro de sesión es la anotación set a set de lo realmente ejecutado en una sesión
/// (spec 0001, US-34; decisión D5). Se ancla al plan en lectura por clave de sesión determinista
/// (ADR-0014): cabecera con mesociclo, microciclo y día —sin claves foráneas al plan—, una foto
/// por ítem (ejercicio, papel y objetivo) y una fila por serie con su valor real, su unidad
/// derivada del ejercicio, el RIR real y el lastre opcionales.
/// </summary>
public sealed class SessionLogTests
{
    private static readonly Guid UserId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid MesocycleId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly DateOnly SessionDate = new(2026, 10, 5);
    private static readonly DateTimeOffset Recorded = new(2026, 10, 5, 18, 30, 0, TimeSpan.Zero);

    private static SessionLogSetInput[] Sets(params SessionLogSetInput[] sets) => sets;

    private static SessionLogItemInput Item(
        string exerciseId = "push_up",
        string? exerciseName = "Flexiones",
        SessionItemRole role = SessionItemRole.Strength,
        ExerciseGroup? pattern = ExerciseGroup.Push,
        Metric metric = Metric.Reps,
        int prescribedSets = 3,
        int? repsMin = 8,
        int? repsMax = 12,
        int? holdSecondsMin = null,
        int? holdSecondsMax = null,
        IReadOnlyCollection<SessionLogSetInput>? sets = null,
        Guid? clientId = null) =>
        new(
            exerciseId,
            exerciseName ?? exerciseId,
            role,
            pattern,
            metric,
            prescribedSets,
            repsMin,
            repsMax,
            holdSecondsMin,
            holdSecondsMax,
            Note: null,
            sets ?? [new SessionLogSetInput(1, 10), new SessionLogSetInput(2, 11)],
            clientId);

    private static SessionLog MesocycleSession(params SessionLogItemInput[] items)
    {
        var log = SessionLog.Create(
            UserId,
            SessionLogKind.Mesocycle,
            SessionDate,
            MesocycleId,
            microcycleNumber: 2,
            sessionDay: 1,
            Recorded).Value;

        foreach (var input in items)
        {
            log.UpsertItem(input);
        }

        return log;
    }

    [Fact]
    public void Create_keeps_the_session_key_and_starts_pending()
    {
        var result = SessionLog.Create(
            UserId,
            SessionLogKind.Mesocycle,
            SessionDate,
            MesocycleId,
            microcycleNumber: 2,
            sessionDay: 1,
            Recorded);

        Assert.True(result.IsSuccess);
        var log = result.Value;
        Assert.Equal(UserId, log.UserId);
        Assert.Equal(SessionLogKind.Mesocycle, log.Kind);
        Assert.Equal(SessionDate, log.SessionDate);
        Assert.Equal(MesocycleId, log.MesocycleId);
        Assert.Equal(2, log.MicrocycleNumber);
        Assert.Equal(1, log.SessionDay);
        Assert.Equal(Recorded, log.RecordedAtUtc);
        Assert.False(log.IsCompleted);
        Assert.Null(log.CompletedAtUtc);
        Assert.Empty(log.Items);
    }

    [Fact]
    public void Create_accepts_a_suelta_session_without_a_mesocycle()
    {
        var result = SessionLog.Create(
            UserId,
            SessionLogKind.Suelta,
            SessionDate,
            mesocycleId: null,
            microcycleNumber: null,
            sessionDay: null,
            Recorded);

        Assert.True(result.IsSuccess);
        Assert.Equal(SessionLogKind.Suelta, result.Value.Kind);
        Assert.Null(result.Value.MesocycleId);
        Assert.Null(result.Value.MicrocycleNumber);
        Assert.Null(result.Value.SessionDay);
    }

    [Fact]
    public void Create_rejects_a_mesocycle_session_without_its_key()
    {
        var result = SessionLog.Create(
            UserId,
            SessionLogKind.Mesocycle,
            SessionDate,
            mesocycleId: null,
            microcycleNumber: 1,
            sessionDay: 1,
            Recorded);

        Assert.True(result.IsFailure);
        Assert.Equal(DomainErrors.SessionLog.MesocycleKeyRequired, result.Error);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(5)]
    public void Create_rejects_a_microcycle_outside_one_to_four(int microcycle)
    {
        var result = SessionLog.Create(
            UserId,
            SessionLogKind.Mesocycle,
            SessionDate,
            MesocycleId,
            microcycle,
            1,
            Recorded);

        Assert.True(result.IsFailure);
        Assert.Equal(DomainErrors.SessionLog.MicrocycleOutOfRange, result.Error);
    }

    [Fact]
    public void Create_rejects_a_mesocycle_session_day_below_one()
    {
        var result = SessionLog.Create(
            UserId,
            SessionLogKind.Mesocycle,
            SessionDate,
            MesocycleId,
            1,
            sessionDay: 0,
            Recorded);

        Assert.True(result.IsFailure);
        Assert.Equal(DomainErrors.SessionLog.SessionDayOutOfRange, result.Error);
    }

    [Fact]
    public void Create_rejects_a_suelta_session_that_carries_a_mesocycle_key()
    {
        var result = SessionLog.Create(
            UserId,
            SessionLogKind.Suelta,
            SessionDate,
            MesocycleId,
            microcycleNumber: 1,
            sessionDay: 1,
            Recorded);

        Assert.True(result.IsFailure);
        Assert.Equal(DomainErrors.SessionLog.SueltaKeyNotAllowed, result.Error);
    }

    [Fact]
    public void UpsertItem_registers_each_set_in_order_with_its_value_and_unit()
    {
        var log = MesocycleSession();

        var added = log.UpsertItem(Item());

        Assert.True(added.IsSuccess);
        var item = added.Value;
        Assert.Equal("push_up", item.ExerciseId);
        Assert.Equal("Flexiones", item.ExerciseName);
        Assert.Equal(SessionItemRole.Strength, item.Role);
        Assert.Equal(ExerciseGroup.Push, item.Pattern);
        Assert.Equal(Metric.Reps, item.Metric);
        Assert.Equal(3, item.PrescribedSets);
        Assert.Equal([8, 12], [item.RepsMin, item.RepsMax]);
        Assert.Equal([1, 2], item.Sets.Select(set => set.SetNumber));
        Assert.Equal([10, 11], item.Sets.Select(set => set.Value));
        Assert.Single(log.Items);
    }

    [Fact]
    public void UpsertItem_keeps_the_photo_of_a_hold_in_seconds()
    {
        var log = MesocycleSession();

        var added = log.UpsertItem(Item(
            exerciseId: "hollow-body-hold",
            exerciseName: "Cuerpo hueco",
            role: SessionItemRole.Core,
            pattern: null,
            metric: Metric.Seconds,
            repsMin: null,
            repsMax: null,
            holdSecondsMin: 20,
            holdSecondsMax: 30,
            sets: Sets(new SessionLogSetInput(1, 25), new SessionLogSetInput(2, 30))));

        Assert.True(added.IsSuccess);
        var item = added.Value;
        Assert.Equal(Metric.Seconds, item.Metric);
        Assert.Equal([20, 30], [item.HoldSecondsMin, item.HoldSecondsMax]);
        Assert.Null(item.RepsMin);
        Assert.Equal([25, 30], item.Sets.Select(set => set.Value));
    }

    [Fact]
    public void UpsertItem_replaces_the_item_of_the_same_exercise_in_place()
    {
        var log = MesocycleSession();
        var first = log.UpsertItem(Item(sets: Sets(new SessionLogSetInput(1, 10)))).Value;

        var again = log.UpsertItem(Item(sets: Sets(new SessionLogSetInput(1, 12), new SessionLogSetInput(2, 13))));

        Assert.True(again.IsSuccess);
        Assert.Equal(first.Id, again.Value.Id);
        Assert.Single(log.Items);
        Assert.Equal([12, 13], log.Items.Single().Sets.Select(set => set.Value));
    }

    [Fact]
    public void UpsertItem_keeps_the_items_of_the_session_in_registration_order()
    {
        var log = MesocycleSession();

        log.UpsertItem(Item());
        log.UpsertItem(Item(exerciseId: "pull_up", exerciseName: "Dominadas"));

        Assert.Equal(["push_up", "pull_up"], log.Items.Select(item => item.ExerciseId));
        Assert.Equal([1, 2], log.Items.Select(item => item.Position));
    }

    [Fact]
    public void UpsertItem_accepts_the_actual_rir_and_the_load_of_each_set()
    {
        var log = MesocycleSession();

        var added = log.UpsertItem(Item(sets: Sets(
            new SessionLogSetInput(1, 10, ActualRir: 2, LoadKg: 5),
            new SessionLogSetInput(2, 11, ActualRir: null, LoadKg: null))));

        Assert.True(added.IsSuccess);
        Assert.Equal(2, added.Value.Sets.ElementAt(0).ActualRir);
        Assert.Equal(5, added.Value.Sets.ElementAt(0).LoadKg);
        Assert.Null(added.Value.Sets.ElementAt(1).ActualRir);
        Assert.Null(added.Value.Sets.ElementAt(1).LoadKg);
    }

    [Fact]
    public void UpsertItem_keeps_the_client_id_that_makes_the_registration_idempotent()
    {
        var clientId = Guid.NewGuid();
        var log = MesocycleSession();

        var added = log.UpsertItem(Item(clientId: clientId));

        Assert.True(added.IsSuccess);
        Assert.Equal(clientId, added.Value.ClientId);
    }

    [Fact]
    public void UpsertItem_rejects_a_session_without_sets()
    {
        var log = MesocycleSession();

        var added = log.UpsertItem(Item(sets: []));

        Assert.True(added.IsFailure);
        Assert.Equal(DomainErrors.SessionLog.SetsRequired, added.Error);
        Assert.Empty(log.Items);
    }

    [Fact]
    public void UpsertItem_rejects_an_item_without_exercise()
    {
        var log = MesocycleSession();

        var added = log.UpsertItem(Item(exerciseId: "  "));

        Assert.True(added.IsFailure);
        Assert.Equal(DomainErrors.SessionLog.ExerciseRequired, added.Error);
        Assert.Empty(log.Items);
    }

    [Fact]
    public void UpsertItem_rejects_an_item_without_a_prescription()
    {
        var log = MesocycleSession();

        var added = log.UpsertItem(Item(prescribedSets: 0));

        Assert.True(added.IsFailure);
        Assert.Equal(DomainErrors.SessionLog.PrescribedSetsOutOfRange, added.Error);
    }

    [Fact]
    public void UpsertItem_rejects_a_half_declared_prescription_range()
    {
        var log = MesocycleSession();

        var added = log.UpsertItem(Item(repsMin: 8, repsMax: null));

        Assert.True(added.IsFailure);
        Assert.Equal(DomainErrors.SessionLog.PrescriptionRangeIncomplete, added.Error);
    }

    [Fact]
    public void UpsertItem_rejects_an_inverted_prescription_range()
    {
        var log = MesocycleSession();

        var added = log.UpsertItem(Item(repsMin: 12, repsMax: 8));

        Assert.True(added.IsFailure);
        Assert.Equal(DomainErrors.SessionLog.PrescriptionRangeInverted, added.Error);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(int.MinValue)]
    public void UpsertItem_rejects_a_negative_actual_value(int value)
    {
        var log = MesocycleSession();

        var added = log.UpsertItem(Item(sets: Sets(new SessionLogSetInput(1, value))));

        Assert.True(added.IsFailure);
        Assert.Equal(DomainErrors.SessionLog.ValueMustBeNonNegative, added.Error);
    }

    [Fact]
    public void UpsertItem_accepts_a_zero_actual_value()
    {
        var log = MesocycleSession();

        var added = log.UpsertItem(Item(sets: Sets(new SessionLogSetInput(1, 0))));

        Assert.True(added.IsSuccess);
        Assert.Equal(0, added.Value.Sets.Single().Value);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(11)]
    public void UpsertItem_rejects_an_actual_rir_outside_zero_to_ten(int actualRir)
    {
        var log = MesocycleSession();

        var added = log.UpsertItem(Item(sets: Sets(new SessionLogSetInput(1, 8, actualRir))));

        Assert.True(added.IsFailure);
        Assert.Equal(DomainErrors.SessionLog.ActualRirOutOfRange, added.Error);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(10)]
    public void UpsertItem_accepts_an_actual_rir_at_the_boundaries(int actualRir)
    {
        var log = MesocycleSession();

        var added = log.UpsertItem(Item(sets: Sets(new SessionLogSetInput(1, 8, actualRir))));

        Assert.True(added.IsSuccess);
        Assert.Equal(actualRir, added.Value.Sets.Single().ActualRir);
    }

    [Fact]
    public void UpsertItem_rejects_a_negative_load()
    {
        var log = MesocycleSession();

        var added = log.UpsertItem(Item(sets: Sets(new SessionLogSetInput(1, 8, LoadKg: -2.5))));

        Assert.True(added.IsFailure);
        Assert.Equal(DomainErrors.SessionLog.LoadMustBeNonNegative, added.Error);
    }

    [Fact]
    public void UpsertItem_rejects_set_numbers_that_skip_a_position()
    {
        var log = MesocycleSession();

        var added = log.UpsertItem(Item(sets: Sets(new SessionLogSetInput(1, 8), new SessionLogSetInput(3, 8))));

        Assert.True(added.IsFailure);
        Assert.Equal(DomainErrors.SessionLog.SetNumbersNotConsecutive, added.Error);
    }

    [Fact]
    public void UpsertItem_rejects_set_numbers_that_start_below_one()
    {
        var log = MesocycleSession();

        var added = log.UpsertItem(Item(sets: Sets(new SessionLogSetInput(0, 8), new SessionLogSetInput(1, 8))));

        Assert.True(added.IsFailure);
        Assert.Equal(DomainErrors.SessionLog.SetNumberOutOfRange, added.Error);
    }

    [Fact]
    public void UpdateItemSets_replaces_the_values_of_an_existing_item()
    {
        var log = MesocycleSession();
        var item = log.UpsertItem(Item(sets: Sets(
            new SessionLogSetInput(1, 10),
            new SessionLogSetInput(2, 11),
            new SessionLogSetInput(3, 9)))).Value;

        var update = log.UpdateItemSets(
            item.Id,
            Sets(new SessionLogSetInput(1, 12, 2), new SessionLogSetInput(2, 13, LoadKg: 4), new SessionLogSetInput(3, 10)));

        Assert.True(update.IsSuccess);
        Assert.Equal([12, 13, 10], log.Items.Single().Sets.Select(set => set.Value));
        Assert.Equal(2, log.Items.Single().Sets.ElementAt(0).ActualRir);
        Assert.Equal(4, log.Items.Single().Sets.ElementAt(1).LoadKg);
        // La foto del ítem no cambia al editar los valores (spec 0001, US-36; decisión D5).
        Assert.Equal("Flexiones", log.Items.Single().ExerciseName);
    }

    [Fact]
    public void UpdateItemSets_rejects_an_item_without_sets_and_keeps_its_values()
    {
        var log = MesocycleSession();
        var item = log.UpsertItem(Item(sets: Sets(new SessionLogSetInput(1, 10), new SessionLogSetInput(2, 11)))).Value;

        var update = log.UpdateItemSets(item.Id, []);

        Assert.True(update.IsFailure);
        Assert.Equal(DomainErrors.SessionLog.SetsRequired, update.Error);
        Assert.Equal([10, 11], log.Items.Single().Sets.Select(set => set.Value));
    }

    [Fact]
    public void UpdateItemSets_rejects_a_negative_value_and_keeps_the_previous_sets()
    {
        var log = MesocycleSession();
        var item = log.UpsertItem(Item(sets: Sets(new SessionLogSetInput(1, 10)))).Value;

        var update = log.UpdateItemSets(item.Id, Sets(new SessionLogSetInput(1, -2)));

        Assert.True(update.IsFailure);
        Assert.Equal(DomainErrors.SessionLog.ValueMustBeNonNegative, update.Error);
        Assert.Equal([10], log.Items.Single().Sets.Select(set => set.Value));
    }

    [Fact]
    public void UpdateItemSets_rejects_non_consecutive_set_numbers_and_keeps_the_previous_sets()
    {
        var log = MesocycleSession();
        var item = log.UpsertItem(Item(sets: Sets(new SessionLogSetInput(1, 10), new SessionLogSetInput(2, 11)))).Value;

        var update = log.UpdateItemSets(item.Id, Sets(new SessionLogSetInput(1, 12), new SessionLogSetInput(3, 13)));

        Assert.True(update.IsFailure);
        Assert.Equal(DomainErrors.SessionLog.SetNumbersNotConsecutive, update.Error);
        Assert.Equal([10, 11], log.Items.Single().Sets.Select(set => set.Value));
    }

    [Fact]
    public void UpdateItemSets_rejects_an_item_that_is_not_in_the_session()
    {
        var log = MesocycleSession();
        log.UpsertItem(Item());

        var update = log.UpdateItemSets(Guid.NewGuid(), Sets(new SessionLogSetInput(1, 12)));

        Assert.True(update.IsFailure);
        Assert.Equal(DomainErrors.SessionLog.ItemNotFound, update.Error);
    }

    [Fact]
    public void RemoveItem_drops_the_item_and_unmarks_a_completed_session()
    {
        var log = MesocycleSession();
        var item = log.UpsertItem(Item()).Value;
        log.UpsertItem(Item(exerciseId: "pull_up", exerciseName: "Dominadas"));
        log.Complete(new DateTimeOffset(2026, 10, 5, 19, 0, 0, TimeSpan.Zero));
        Assert.True(log.IsCompleted);

        var removed = log.RemoveItem(item.Id);

        Assert.True(removed.IsSuccess);
        Assert.Equal(["pull_up"], log.Items.Select(candidate => candidate.ExerciseId));
        // Borrar un registro des-marca la sesión: lo que dependía de ella se recalcula (US23, US-36).
        Assert.False(log.IsCompleted);
        Assert.Null(log.CompletedAtUtc);
    }

    [Fact]
    public void RemoveItem_rejects_an_item_that_is_not_in_the_session()
    {
        var log = MesocycleSession();
        log.UpsertItem(Item());

        var removed = log.RemoveItem(Guid.NewGuid());

        Assert.True(removed.IsFailure);
        Assert.Equal(DomainErrors.SessionLog.ItemNotFound, removed.Error);
    }

    [Fact]
    public void Complete_marks_the_session_completed_with_its_timestamp()
    {
        var log = MesocycleSession();
        var completedAt = new DateTimeOffset(2026, 10, 5, 19, 15, 0, TimeSpan.Zero);

        var completion = log.Complete(completedAt);

        Assert.True(completion.IsSuccess);
        Assert.True(log.IsCompleted);
        Assert.Equal(completedAt, log.CompletedAtUtc);
    }

    [Fact]
    public void Complete_is_idempotent_and_keeps_the_first_timestamp()
    {
        var log = MesocycleSession();
        var completedAt = new DateTimeOffset(2026, 10, 5, 19, 15, 0, TimeSpan.Zero);
        log.Complete(completedAt);

        log.Complete(completedAt.AddHours(2));

        Assert.Equal(completedAt, log.CompletedAtUtc);
    }

    [Fact]
    public void Uncomplete_clears_the_completion_and_keeps_the_registered_sets()
    {
        var log = MesocycleSession();
        log.UpsertItem(Item());
        log.Complete(new DateTimeOffset(2026, 10, 5, 19, 15, 0, TimeSpan.Zero));

        var uncompleted = log.Uncomplete();

        Assert.True(uncompleted.IsSuccess);
        Assert.False(log.IsCompleted);
        Assert.Null(log.CompletedAtUtc);
        Assert.Equal([10, 11], log.Items.Single().Sets.Select(set => set.Value));
    }
}
