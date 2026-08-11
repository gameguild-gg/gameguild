using FluentAssertions;
using GameGuild.CQRS;
using GameGuild.Economy.Contracts;
using GameGuild.Economy.Payouts.Commands;
using GameGuild.Economy.Payouts.Queries;
using GameGuild.Economy.Queries;
using GameGuild.Identity.Context.Actors;

namespace GameGuild.Economy.Payouts.UnitTests;

public sealed class SelfServicePayoutRequestTests
{
    [Fact]
    public async Task CreateBindsTheRequestToTheAuthenticatedActorsWalletAndReplaysIdempotently()
    {
        var actorId = Guid.Parse("b1000000-0000-0000-0000-000000000001");
        var walletId = Guid.Parse("b1000000-0000-0000-0000-000000000002");
        var accessor = AuthenticatedAccessor(actorId);
        var store = new InMemoryPayoutRequestStore();
        var handler = new CreateMyPayoutRequestCommandHandler(
            new WalletSender(CreateWallet(walletId)), accessor, store);
        var command = new CreateMyPayoutRequestCommand(new CreateMyPayoutRequestRequest(250, "request-1"));

        var created = await handler.Handle(command, CancellationToken.None);
        var replay = await handler.Handle(command, CancellationToken.None);

        created.HardCoinUnits.Should().Be(250);
        created.State.Should().Be(PayoutRequestState.Submitted);
        replay.Id.Should().Be(created.Id);
        store.Items.Should().ContainSingle(item => item.PayeeId == actorId && item.WalletId.Value == walletId);
    }

    [Fact]
    public async Task CreateRejectsAReusedIdempotencyKeyWithDifferentValue()
    {
        var actorId = Guid.NewGuid();
        var accessor = AuthenticatedAccessor(actorId);
        var store = new InMemoryPayoutRequestStore();
        var handler = new CreateMyPayoutRequestCommandHandler(
            new WalletSender(CreateWallet(Guid.NewGuid())), accessor, store);

        await handler.Handle(
            new CreateMyPayoutRequestCommand(new CreateMyPayoutRequestRequest(250, "request-1")),
            CancellationToken.None);

        await FluentActions.Invoking(() => handler.Handle(
                new CreateMyPayoutRequestCommand(new CreateMyPayoutRequestRequest(251, "request-1")),
                CancellationToken.None))
            .Should().ThrowAsync<PayoutRequestReplayConflictException>();
    }

    [Fact]
    public async Task CreateAllowsDifferentPayeesToUseTheSameIdempotencyKey()
    {
        var store = new InMemoryPayoutRequestStore();
        var firstActorId = Guid.NewGuid();
        var secondActorId = Guid.NewGuid();
        var first = new CreateMyPayoutRequestCommandHandler(
            new WalletSender(CreateWallet(Guid.NewGuid())), AuthenticatedAccessor(firstActorId), store);
        var command = new CreateMyPayoutRequestCommand(new CreateMyPayoutRequestRequest(250, "request-1"));

        var firstRequest = await first.Handle(command, CancellationToken.None);
        var second = new CreateMyPayoutRequestCommandHandler(
            new WalletSender(CreateWallet(Guid.NewGuid())), AuthenticatedAccessor(secondActorId), store);
        var secondRequest = await second.Handle(command, CancellationToken.None);

        secondRequest.Id.Should().NotBe(firstRequest.Id);
        store.Items.Should().HaveCount(2);
    }

    [Fact]
    public async Task CreateRejectsValueAboveTheConfirmedWithdrawableBalance()
    {
        var handler = new CreateMyPayoutRequestCommandHandler(
            new WalletSender(CreateWallet(Guid.NewGuid(), withdrawableHard: 249)),
            AuthenticatedAccessor(Guid.NewGuid()),
            new InMemoryPayoutRequestStore());

        await FluentActions.Invoking(() => handler.Handle(
                new CreateMyPayoutRequestCommand(new CreateMyPayoutRequestRequest(250, "request-1")),
                CancellationToken.None))
            .Should().ThrowAsync<PayoutRequestInsufficientWithdrawableFundsException>();
    }

    [Fact]
    public async Task CreateRejectsAnInactiveWallet()
    {
        var handler = new CreateMyPayoutRequestCommandHandler(
            new WalletSender(CreateWallet(Guid.NewGuid(), state: WalletLifecycleState.Frozen)),
            AuthenticatedAccessor(Guid.NewGuid()),
            new InMemoryPayoutRequestStore());

        await FluentActions.Invoking(() => handler.Handle(
                new CreateMyPayoutRequestCommand(new CreateMyPayoutRequestRequest(250, "request-1")),
                CancellationToken.None))
            .Should().ThrowAsync<PayoutRequestWalletUnavailableException>();
    }

    [Fact]
    public async Task CancelOnlyChangesTheAuthenticatedPayeesSubmittedRequest()
    {
        var actorId = Guid.NewGuid();
        var store = new InMemoryPayoutRequestStore();
        var request = CreateRequest(actorId, PayoutRequestState.Submitted);
        store.Add(request);
        var handler = new CancelMyPayoutRequestCommandHandler(AuthenticatedAccessor(actorId), store);

        var cancelled = await handler.Handle(new CancelMyPayoutRequestCommand(request.Id), CancellationToken.None);

        cancelled.State.Should().Be(PayoutRequestState.Cancelled);
        cancelled.UpdatedAt.Should().BeOnOrAfter(request.UpdatedAt);
        store.GetForPayee(request.Id, actorId).Version.Should().Be(2);
    }

    [Fact]
    public async Task CancelRejectsARequestThatIsNoLongerSubmitted()
    {
        var actorId = Guid.NewGuid();
        var store = new InMemoryPayoutRequestStore();
        var request = CreateRequest(actorId, PayoutRequestState.Approved);
        store.Add(request);
        var handler = new CancelMyPayoutRequestCommandHandler(AuthenticatedAccessor(actorId), store);

        await FluentActions.Invoking(() => handler.Handle(
                new CancelMyPayoutRequestCommand(request.Id), CancellationToken.None))
            .Should().ThrowAsync<PayoutRequestTransitionException>();
    }

    [Fact]
    public async Task CancelRequiresAnAuthenticatedTenantActor()
    {
        var handler = new CancelMyPayoutRequestCommandHandler(
            new ActorContextAccessor(),
            new InMemoryPayoutRequestStore());

        await FluentActions.Invoking(() => handler.Handle(
                new CancelMyPayoutRequestCommand(Guid.NewGuid()), CancellationToken.None))
            .Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public void CancelRejectsAClockThatMovesBackwards()
    {
        var timestamp = DateTimeOffset.UtcNow;
        var request = CreateRequest(Guid.NewGuid(), PayoutRequestState.Submitted, timestamp);

        FluentActions.Invoking(() => request.Cancel(timestamp.AddSeconds(-1)))
            .Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public async Task ListReturnsOnlyThePayeesRequests()
    {
        var payeeId = Guid.NewGuid();
        var store = new InMemoryPayoutRequestStore();
        var older = CreateRequest(payeeId, PayoutRequestState.Submitted, DateTimeOffset.UtcNow.AddMinutes(-1));
        var latest = CreateRequest(payeeId, PayoutRequestState.Cancelled, DateTimeOffset.UtcNow);
        store.Add(older);
        store.Add(latest);
        store.Add(CreateRequest(Guid.NewGuid(), PayoutRequestState.Submitted, DateTimeOffset.UtcNow.AddMinutes(1)));
        var handler = new ListMyPayoutRequestsQueryHandler(store);

        var result = await handler.Handle(new ListMyPayoutRequestsQuery(payeeId, 10), CancellationToken.None);

        result.Select(item => item.Id).Should().Equal(latest.Id, older.Id);
        result.Should().OnlyContain(item => item.HardCoinUnits == 250);
        result.Select(item => item.State).Should().Equal(PayoutRequestState.Cancelled, PayoutRequestState.Submitted);
        result[0].CreatedAt.Should().Be(latest.CreatedAt);
        result[0].UpdatedAt.Should().Be(latest.UpdatedAt);
    }

    [Fact]
    public void ValidatorRejectsAnEmptyIdempotencyKeyOrNonPositiveAmount()
    {
        var validator = new CreateMyPayoutRequestCommandValidator();

        var result = validator.Validate(new CreateMyPayoutRequestCommand(
            new CreateMyPayoutRequestRequest(0, string.Empty)));

        result.IsValid.Should().BeFalse();
    }

    private static ActorContextAccessor AuthenticatedAccessor(Guid actorId)
    {
        var accessor = new ActorContextAccessor();
        accessor.SetActorContext(new ActorContext
        {
            ActorKind = ActorKind.User,
            SubjectId = actorId.ToString(),
            TenantId = Guid.NewGuid(),
            Roles = new HashSet<string>(),
            Permissions = new HashSet<string>(),
            TypedAttributes = ActorAttributes.Empty,
            IsAuthenticated = true
        });
        return accessor;
    }

    private static EconomyWalletSummaryDto CreateWallet(
        Guid walletId,
        long withdrawableHard = 1_000,
        WalletLifecycleState state = WalletLifecycleState.Active) => new(
        WalletId: walletId,
        State: state,
        CreatedAt: DateTimeOffset.UtcNow,
        PendingHard: 0,
        PendingSoft: 0,
        PurchasedHard: 0,
        EarnedHard: 0,
        RestrictedHard: 0,
        Soft: 0,
        HeldHard: 0,
        HeldSoft: 0,
        AvailableHardToSpend: 0,
        AvailableSoftToSpend: 0,
        WithdrawableHard: withdrawableHard,
        OutstandingHardDebt: 0,
        ProjectionRebuiltAt: DateTimeOffset.UtcNow,
        SourceJournalSequence: 0);

    private static PayoutRequest CreateRequest(
        Guid payeeId,
        PayoutRequestState state,
        DateTimeOffset? createdAt = null)
    {
        var timestamp = createdAt ?? DateTimeOffset.UtcNow;
        return new PayoutRequest(
            Guid.NewGuid(),
            new IdempotencyKey($"request-{Guid.NewGuid():N}"),
            new string('a', 64),
            payeeId,
            new WalletId(Guid.NewGuid()),
            new CoinAmount(CurrencyCode.HardCoin, 250),
            state,
            1,
            timestamp,
            timestamp);
    }

    private sealed class WalletSender(EconomyWalletSummaryDto wallet) : ISender
    {
        public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default) =>
            request is GetMyEconomyWalletQuery
                ? Task.FromResult((TResponse)(object)wallet)
                : throw new InvalidOperationException($"Unexpected request {request.GetType().Name}.");

        public Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = default)
            where TRequest : IRequest => throw new InvalidOperationException("No void request is expected.");

        public Task<object?> Send(object request, CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("No dynamic request is expected.");
    }

    private sealed class InMemoryPayoutRequestStore : IPayoutRequestStore
    {
        private readonly List<PayoutRequest> _items = [];

        public IReadOnlyList<PayoutRequest> Items => _items;

        public PayoutRequest? FindReplay(Guid payeeId, string idempotencyKey, string requestHash)
        {
            var item = _items.SingleOrDefault(candidate =>
                candidate.PayeeId == payeeId && candidate.IdempotencyKey.Value == idempotencyKey);
            if (item is null)
                return null;
            if (!string.Equals(item.RequestHash, requestHash, StringComparison.Ordinal))
                throw new PayoutRequestReplayConflictException("Payout request idempotency key was reused with different inputs.");
            return item;
        }

        public void Add(PayoutRequest request)
        {
            if (_items.Any(item =>
                    item.PayeeId == request.PayeeId && item.IdempotencyKey.Value == request.IdempotencyKey.Value))
                throw new PayoutRequestReplayConflictException("Payout request idempotency key was reused.");
            _items.Add(request);
        }

        public PayoutRequest GetForPayee(Guid requestId, Guid payeeId) => _items.Single(item => item.Id == requestId && item.PayeeId == payeeId);

        public IReadOnlyList<PayoutRequest> ListForPayee(Guid payeeId, int take) => _items
            .Where(item => item.PayeeId == payeeId)
            .OrderByDescending(item => item.CreatedAt)
            .ThenByDescending(item => item.Id)
            .Take(take)
            .ToArray();

        public PayoutRequest Update(PayoutRequest request, long expectedVersion)
        {
            var index = _items.FindIndex(item => item.Id == request.Id);
            if (index < 0 || _items[index].Version != expectedVersion)
                throw new PayoutRequestStaleCommandException("Payout request version is stale.");
            _items[index] = request;
            return request;
        }
    }
}
