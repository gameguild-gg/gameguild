using FluentAssertions;
using GameGuild.Economy.Contracts;

namespace GameGuild.Economy.Payouts.UnitTests;

public sealed class InMemoryPayoutOperationStoreTests
{
    private static readonly DateTimeOffset Time = DateTimeOffset.Parse("2026-08-03T12:00:00Z");

    [Fact]
    public void Store_ProvidesIdempotentReplayAndCompareAndSwapUpdates()
    {
        var store = new InMemoryPayoutOperationStore();
        var operation = Operation();

        store.FindReplay(operation.IdempotencyKey.Value, operation.RequestHash).Should().BeNull();
        store.Add(operation);
        store.Get(operation.Id).Should().BeSameAs(operation);
        store.FindReplay(operation.IdempotencyKey.Value, operation.RequestHash).Should().BeSameAs(operation);
        store.Operations.Should().ContainSingle();

        var dispatching = operation.Transition(PayoutOperationState.Dispatching, Time.AddMinutes(1));
        store.Update(dispatching, operation.Version).Should().BeSameAs(dispatching);
        store.Get(operation.Id).State.Should().Be(PayoutOperationState.Dispatching);
    }

    [Fact]
    public void Store_RejectsMissingDuplicateMutatedAndStaleOperations()
    {
        var store = new InMemoryPayoutOperationStore();
        var operation = Operation();
        store.Add(operation);

        FluentActions.Invoking(() => store.Get(Guid.NewGuid())).Should().Throw<KeyNotFoundException>();
        FluentActions.Invoking(() => store.Add(operation)).Should().Throw<PayoutReplayConflictException>();
        FluentActions.Invoking(() => store.Add(Operation() with { IdempotencyKey = operation.IdempotencyKey }))
            .Should().Throw<PayoutReplayConflictException>();
        FluentActions.Invoking(() => store.FindReplay(operation.IdempotencyKey.Value, "mutated"))
            .Should().Throw<PayoutReplayConflictException>();
        FluentActions.Invoking(() => store.Update(operation with { Version = 3 }, 2))
            .Should().Throw<PayoutStaleCommandException>();
        FluentActions.Invoking(() => store.Update(operation with { Version = 2 }, 2))
            .Should().Throw<PayoutStaleCommandException>();
        FluentActions.Invoking(() => store.Update(Operation() with { Version = 2 }, 1))
            .Should().Throw<KeyNotFoundException>();
        FluentActions.Invoking(() => store.Add(null!)).Should().Throw<ArgumentNullException>();
        FluentActions.Invoking(() => store.Update(null!, 1)).Should().Throw<ArgumentNullException>();
        FluentActions.Invoking(() => store.FindReplay("", "hash")).Should().Throw<ArgumentException>();
        FluentActions.Invoking(() => store.FindReplay("key", "")).Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Store_RecordsProviderEventsAtomicallyAndRejectsMutatedReplay()
    {
        var store = new InMemoryPayoutOperationStore();
        var operation = Operation(PayoutOperationState.Dispatching);
        store.Add(operation);
        var succeeded = operation.Transition(PayoutOperationState.Succeeded, Time.AddMinutes(1));

        var record = store.RecordProviderEvent("evt_1", "hash", succeeded, 1, Time.AddMinutes(1));

        record.ResultingState.Should().Be(PayoutOperationState.Succeeded);
        store.ProviderEvents.Should().ContainSingle();
        store.FindProviderEvent("evt_1", "hash").Should().BeSameAs(record);
        store.RecordProviderEvent("evt_1", "hash", succeeded, 1, Time.AddMinutes(1)).Should().BeSameAs(record);
        store.FindProviderEvent("missing", "hash").Should().BeNull();
        FluentActions.Invoking(() => store.FindProviderEvent("evt_1", "mutated"))
            .Should().Throw<PayoutReplayConflictException>();
        FluentActions.Invoking(() => store.RecordProviderEvent("evt_1", "mutated", succeeded, 1, Time))
            .Should().Throw<PayoutReplayConflictException>();
    }

    [Fact]
    public void Store_ValidatesProviderEventAndReplayInputs()
    {
        var store = new InMemoryPayoutOperationStore();
        var operation = Operation(PayoutOperationState.Dispatching);
        store.Add(operation);
        var succeeded = operation.Transition(PayoutOperationState.Succeeded, Time.AddMinutes(1));

        FluentActions.Invoking(() => store.FindProviderEvent("", "hash")).Should().Throw<ArgumentException>();
        FluentActions.Invoking(() => store.FindProviderEvent("event", "")).Should().Throw<ArgumentException>();
        FluentActions.Invoking(() => store.RecordProviderEvent("", "hash", succeeded, 1, Time))
            .Should().Throw<ArgumentException>();
        FluentActions.Invoking(() => store.RecordProviderEvent("event", "", succeeded, 1, Time))
            .Should().Throw<ArgumentException>();
        FluentActions.Invoking(() => store.RecordProviderEvent("event", "hash", null!, 1, Time))
            .Should().Throw<ArgumentNullException>();
        FluentActions.Invoking(() => store.RecordProviderEvent(
                "event", "hash", succeeded with { Version = 3 }, 1, Time))
            .Should().Throw<PayoutStaleCommandException>();
        store.ProviderEvents.Should().BeEmpty();
    }

    private static PayoutOperation Operation(PayoutOperationState state = PayoutOperationState.Reserved) => new(
        Guid.NewGuid(), new IdempotencyKey(Guid.NewGuid().ToString("N")), "request", Guid.NewGuid(), Guid.NewGuid(),
        WalletId.New(), new CoinAmount(CurrencyCode.HardCoin, 10), "acct", "destination", "binding", "eligibility",
        null, null, state, 1, 1, 1, new ReserveVersion(1), 1, new PolicyVersion(1), Guid.NewGuid(), Time, Time);
}
