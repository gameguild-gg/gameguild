using FluentAssertions;
using GameGuild.Economy.Contracts;
using GameGuild.Economy.Payouts.Queries;

namespace GameGuild.Economy.Payouts.UnitTests;

public sealed class SelfServicePayoutQueriesTests
{
    [Fact]
    public async Task ListReturnsOnlyTheAuthenticatedPayeeOperationsInReverseChronologicalOrder()
    {
        var payeeId = Guid.Parse("a1000000-0000-0000-0000-000000000001");
        var store = new InMemoryPayoutOperationStore();
        store.Add(CreateOperation(payeeId, 100, new DateTimeOffset(2026, 8, 10, 9, 0, 0, TimeSpan.Zero)));
        var latest = CreateOperation(payeeId, 200, new DateTimeOffset(2026, 8, 10, 10, 0, 0, TimeSpan.Zero));
        store.Add(latest);
        store.Add(CreateOperation(Guid.NewGuid(), 300, new DateTimeOffset(2026, 8, 10, 11, 0, 0, TimeSpan.Zero)));
        var handler = new ListMyPayoutOperationsQueryHandler(store);

        var result = await handler.Handle(new ListMyPayoutOperationsQuery(payeeId, 10), CancellationToken.None);

        result.Should().HaveCount(2);
        result[0].Id.Should().Be(latest.Id);
        result[0].HardCoinUnits.Should().Be(200);
        result[0].State.Should().Be(PayoutOperationState.Reserved);
        result[0].CreatedAt.Should().Be(latest.CreatedAt);
        result[0].UpdatedAt.Should().Be(latest.UpdatedAt);
    }

    [Fact]
    public async Task GetReturnsNullForMissingOrForeignOperations()
    {
        var payeeId = Guid.Parse("a1000000-0000-0000-0000-000000000002");
        var foreign = CreateOperation(Guid.NewGuid(), 100, DateTimeOffset.UtcNow);
        var store = new InMemoryPayoutOperationStore();
        store.Add(foreign);
        var handler = new GetMyPayoutOperationQueryHandler(store);

        (await handler.Handle(new GetMyPayoutOperationQuery(payeeId, foreign.Id), CancellationToken.None)).Should().BeNull();
        (await handler.Handle(new GetMyPayoutOperationQuery(payeeId, Guid.NewGuid()), CancellationToken.None)).Should().BeNull();
    }

    [Fact]
    public async Task GetReturnsAStatusOnlyDtoForThePayee()
    {
        var payeeId = Guid.Parse("a1000000-0000-0000-0000-000000000003");
        var operation = CreateOperation(payeeId, 450, DateTimeOffset.UtcNow);
        var store = new InMemoryPayoutOperationStore();
        store.Add(operation);
        var handler = new GetMyPayoutOperationQueryHandler(store);

        var result = await handler.Handle(new GetMyPayoutOperationQuery(payeeId, operation.Id), CancellationToken.None);

        result.Should().NotBeNull();
        result!.Id.Should().Be(operation.Id);
        result.HardCoinUnits.Should().Be(450);
        result.State.Should().Be(PayoutOperationState.Reserved);
    }

    [Fact]
    public void StoreRejectsInvalidPayeeAndTakeValues()
    {
        var store = new InMemoryPayoutOperationStore();

        FluentActions.Invoking(() => store.ListForPayee(Guid.Empty, 10))
            .Should().Throw<ArgumentException>();
        FluentActions.Invoking(() => store.ListForPayee(Guid.NewGuid(), 0))
            .Should().Throw<ArgumentOutOfRangeException>();
        FluentActions.Invoking(() => store.ListForPayee(Guid.NewGuid(), 101))
            .Should().Throw<ArgumentOutOfRangeException>();
    }

    private static PayoutOperation CreateOperation(Guid payeeId, long hardCoinUnits, DateTimeOffset createdAt) => new(
        Guid.NewGuid(),
        new IdempotencyKey($"payout-{Guid.NewGuid():N}"),
        "request-hash",
        payeeId,
        payeeId,
        new WalletId(Guid.NewGuid()),
        new CoinAmount(CurrencyCode.HardCoin, hardCoinUnits),
        "acct_test",
        "destination-hash",
        "provider-binding-hash",
        "eligibility-hash",
        null,
        null,
        PayoutOperationState.Reserved,
        1,
        1,
        1,
        new ReserveVersion(1),
        1,
        new PolicyVersion(1),
        Guid.NewGuid(),
        createdAt,
        createdAt);
}
