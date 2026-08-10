using FluentAssertions;
using GameGuild;
using GameGuild.Economy.Contracts;
using GameGuild.Economy.Ledger;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace GameGuild.Economy.Payouts.UnitTests;

public sealed class DurablePayoutSettlementWorkflowTests
{
    private static readonly DateTimeOffset Time = new(2026, 8, 10, 19, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task BeginDispatch_TransitionsAFencedReservationAndAllowsOnlyTheExactRetry()
    {
        var operation = CreateOperation();
        var store = new InMemoryPayoutOperationStore();
        store.Add(operation);
        var context = new RecordingContext();
        var workflow = CreateWorkflow(context, store);
        var request = new DurablePayoutDispatchRequest(
            operation.Id,
            operation.Version,
            operation.FencingToken,
            operation.KillSwitchEpoch,
            "dispatch-snapshot",
            Time.AddMinutes(1));

        var dispatching = await workflow.BeginDispatchAsync(request);

        dispatching.State.Should().Be(PayoutOperationState.Dispatching);
        dispatching.Version.Should().Be(2);
        dispatching.DispatchSnapshotHash.Should().Be("dispatch-snapshot");
        context.Transactions.Should().ContainSingle().Which.CommitCalled.Should().BeTrue();

        var replay = await workflow.BeginDispatchAsync(request);
        replay.Should().BeEquivalentTo(dispatching);
        context.Transactions.Should().HaveCount(2);
        context.Transactions[1].RollbackCalled.Should().BeTrue();

        var stale = request with { ExpectedVersion = operation.Version, DispatchSnapshotHash = "other-snapshot" };
        await FluentActions.Invoking(() => workflow.BeginDispatchAsync(stale))
            .Should().ThrowAsync<PayoutStaleCommandException>();
    }

    [Fact]
    public async Task ProviderEvent_ConsumesReservedFragmentsAndRecordsAnIdempotentSuccess()
    {
        var operation = CreateOperation().Transition(
            PayoutOperationState.Dispatching,
            Time.AddMinutes(1),
            dispatchSnapshotHash: "dispatch-snapshot");
        var store = new InMemoryPayoutOperationStore();
        store.Add(operation);
        var context = new RecordingContext();
        var reservations = new RecordingReservations();
        var postings = new RecordingPostings();
        var workflow = CreateWorkflow(context, store, reservations, postings);
        var providerEvent = CreateProviderEvent(operation, PayoutProviderOutcome.Succeeded);

        var settled = await workflow.ApplyProviderEventAsync(new DurablePayoutProviderEventRequest(
            providerEvent,
            CreateTerminalAuthority(operation)));

        settled.State.Should().Be(PayoutOperationState.Succeeded);
        settled.Version.Should().Be(3);
        settled.ProviderPayoutId.Should().Be(providerEvent.ProviderPayoutId);
        reservations.Transitions.Should().ContainSingle().Which.Should().Be(new ReservationTransition(
            operation.Id,
            PersistedFragmentReservationStatus.Reserved,
            PersistedFragmentReservationStatus.Consumed,
            providerEvent.ObservedAt));
        postings.Requests.Should().ContainSingle();
        postings.Requests[0].Posting.Template.Kind.Should().Be(PostingTemplateKind.PayoutSuccess);
        context.Transactions.Should().ContainSingle().Which.CommitCalled.Should().BeTrue();

        var replay = await workflow.ApplyProviderEventAsync(new DurablePayoutProviderEventRequest(
            providerEvent,
            CreateTerminalAuthority(operation)));
        replay.Should().BeEquivalentTo(settled);
        postings.Requests.Should().ContainSingle();
        reservations.Transitions.Should().ContainSingle();
    }

    [Fact]
    public async Task ProviderEvent_RejectsUnboundEvidenceBeforeAnyPostingOrFragmentMutation()
    {
        var operation = CreateOperation().Transition(
            PayoutOperationState.Dispatching,
            Time.AddMinutes(1),
            dispatchSnapshotHash: "dispatch-snapshot");
        var store = new InMemoryPayoutOperationStore();
        store.Add(operation);
        var context = new RecordingContext();
        var reservations = new RecordingReservations();
        var postings = new RecordingPostings();
        var workflow = CreateWorkflow(context, store, reservations, postings);
        var invalidEvent = CreateProviderEvent(operation, PayoutProviderOutcome.Failed) with { DestinationHash = "wrong" };

        await FluentActions.Invoking(() => workflow.ApplyProviderEventAsync(new DurablePayoutProviderEventRequest(
                invalidEvent,
                CreateTerminalAuthority(operation))))
            .Should().ThrowAsync<PayoutProviderBindingException>();

        postings.Requests.Should().BeEmpty();
        reservations.Transitions.Should().BeEmpty();
        context.Transactions.Should().ContainSingle().Which.RollbackCalled.Should().BeTrue();
    }

    [Fact]
    public async Task ProviderEvent_RequiresADistinctTerminalRiskDecision()
    {
        var operation = CreateOperation().Transition(
            PayoutOperationState.Dispatching,
            Time.AddMinutes(1),
            dispatchSnapshotHash: "dispatch-snapshot");
        var store = new InMemoryPayoutOperationStore();
        store.Add(operation);
        var context = new RecordingContext();
        var workflow = CreateWorkflow(context, store);
        var reusedAuthority = new RegisteredPostingAuthority(
            Guid.NewGuid(), operation.ActorId, Guid.NewGuid(), operation.RiskDecisionId, "terminal-fingerprint", 1);

        await FluentActions.Invoking(() => workflow.ApplyProviderEventAsync(new DurablePayoutProviderEventRequest(
                CreateProviderEvent(operation, PayoutProviderOutcome.Failed), reusedAuthority)))
            .Should().ThrowAsync<InvalidOperationException>();
    }

    private static PostgreSqlDurablePayoutSettlementWorkflow CreateWorkflow(
        RecordingContext context,
        InMemoryPayoutOperationStore store,
        RecordingReservations? reservations = null,
        RecordingPostings? postings = null) => new(
        context,
        store,
        reservations ?? new RecordingReservations(),
        postings ?? new RecordingPostings(),
        new AcceptProviderEvidence());

    private static PayoutOperation CreateOperation() => new(
        Guid.NewGuid(),
        new IdempotencyKey($"payout-{Guid.NewGuid():N}"),
        "request-hash",
        Guid.NewGuid(),
        Guid.NewGuid(),
        WalletId.New(),
        new CoinAmount(CurrencyCode.HardCoin, 700),
        "acct_1",
        "destination-hash",
        "binding-hash",
        "eligibility-hash",
        null,
        null,
        PayoutOperationState.Reserved,
        1,
        8,
        3,
        new ReserveVersion(1),
        1,
        new PolicyVersion(1),
        Guid.NewGuid(),
        Time,
        Time);

    private static PayoutProviderEvent CreateProviderEvent(PayoutOperation operation, PayoutProviderOutcome outcome) => new(
        $"evt_{Guid.NewGuid():N}",
        operation.Id,
        outcome,
        "provider-payout",
        operation.ProviderAccountId,
        operation.DestinationHash,
        "provider-evidence",
        "signature",
        Time.AddMinutes(2));

    private static RegisteredPostingAuthority CreateTerminalAuthority(PayoutOperation operation) => new(
        Guid.NewGuid(),
        Guid.NewGuid(),
        Guid.NewGuid(),
        Guid.NewGuid(),
        $"terminal:{operation.Id:N}",
        1);

    private sealed class RecordingContext : IApplicationDbContext
    {
        public List<RecordingTransaction> Transactions { get; } = [];

        public DbSet<T> Set<T>() where T : class => throw new NotSupportedException();
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => Task.FromResult(0);

        public Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
        {
            var transaction = new RecordingTransaction();
            Transactions.Add(transaction);
            return Task.FromResult<IDbContextTransaction>(transaction);
        }
    }

    private sealed class RecordingTransaction : IDbContextTransaction
    {
        public Guid TransactionId { get; } = Guid.NewGuid();
        public bool CommitCalled { get; private set; }
        public bool RollbackCalled { get; private set; }
        public void Commit() => CommitCalled = true;
        public Task CommitAsync(CancellationToken cancellationToken = default)
        {
            CommitCalled = true;
            return Task.CompletedTask;
        }
        public void Rollback() => RollbackCalled = true;
        public Task RollbackAsync(CancellationToken cancellationToken = default)
        {
            RollbackCalled = true;
            return Task.CompletedTask;
        }
        public void Dispose() { }
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }

    private sealed class RecordingReservations : IFifoFragmentReservationGateway
    {
        public List<ReservationTransition> Transitions { get; } = [];
        public IReadOnlyList<PersistedFragmentReservation> Reserve(FifoFragmentReservationRequest request) => [];
        public long Transition(Guid operationId, PersistedFragmentReservationStatus expected, PersistedFragmentReservationStatus next, DateTimeOffset terminalAt)
        {
            Transitions.Add(new ReservationTransition(operationId, expected, next, terminalAt));
            return 1;
        }
    }

    private sealed class RecordingPostings : IRegisteredPostingGateway
    {
        public List<RegisteredPostingRequest> Requests { get; } = [];
        public RegisteredPostingReceipt Post(RegisteredPostingRequest request)
        {
            Requests.Add(request);
            return new RegisteredPostingReceipt(request.Posting.Id, 1, "journal-hash", false);
        }
    }

    private sealed class AcceptProviderEvidence : IPayoutProviderEvidenceVerifier
    {
        public bool Verify(PayoutDispatchReceipt receipt) => true;
        public bool Verify(PayoutProviderEvent providerEvent) => true;
    }

    private sealed record ReservationTransition(
        Guid OperationId,
        PersistedFragmentReservationStatus Expected,
        PersistedFragmentReservationStatus Next,
        DateTimeOffset TerminalAt);
}