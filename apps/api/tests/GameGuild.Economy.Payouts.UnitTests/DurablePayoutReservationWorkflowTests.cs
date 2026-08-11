using FluentAssertions;
using GameGuild;
using GameGuild.Economy.Contracts;
using GameGuild.Economy.Ledger;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace GameGuild.Economy.Payouts.UnitTests;

public sealed class DurablePayoutReservationWorkflowTests
{
    private static readonly DateTimeOffset Time = new(2026, 8, 11, 7, 0, 0, TimeSpan.Zero);

    public static IEnumerable<object[]> InvalidOperations()
    {
        yield return ["missing operation ID", (Func<PayoutOperation, PayoutOperation>)(operation => operation with { Id = Guid.Empty }), typeof(ArgumentException)];
        yield return ["non-reserved state", (Func<PayoutOperation, PayoutOperation>)(operation => operation with { State = PayoutOperationState.Dispatching }), typeof(InvalidOperationException)];
        yield return ["non-initial version", (Func<PayoutOperation, PayoutOperation>)(operation => operation with { Version = 2 }), typeof(InvalidOperationException)];
        yield return ["non-hard currency", (Func<PayoutOperation, PayoutOperation>)(operation => operation with { Amount = new CoinAmount(CurrencyCode.SoftCoin, 700) }), typeof(ArgumentException)];
        yield return ["zero amount", (Func<PayoutOperation, PayoutOperation>)(operation => operation with { Amount = new CoinAmount(CurrencyCode.HardCoin, 0) }), typeof(ArgumentException)];
        yield return ["missing kill switch epoch", (Func<PayoutOperation, PayoutOperation>)(operation => operation with { KillSwitchEpoch = 0 }), typeof(ArgumentException)];
        yield return ["missing fencing token", (Func<PayoutOperation, PayoutOperation>)(operation => operation with { FencingToken = 0 }), typeof(ArgumentException)];
        yield return ["missing reserve authorization epoch", (Func<PayoutOperation, PayoutOperation>)(operation => operation with { ReserveAuthorizationEpoch = 0 }), typeof(ArgumentException)];
        yield return ["missing request hash", (Func<PayoutOperation, PayoutOperation>)(operation => operation with { RequestHash = " " }), typeof(ArgumentException)];
        yield return ["missing provider account", (Func<PayoutOperation, PayoutOperation>)(operation => operation with { ProviderAccountId = " " }), typeof(ArgumentException)];
        yield return ["missing destination", (Func<PayoutOperation, PayoutOperation>)(operation => operation with { DestinationHash = " " }), typeof(ArgumentException)];
        yield return ["missing provider binding", (Func<PayoutOperation, PayoutOperation>)(operation => operation with { ProviderBindingHash = " " }), typeof(ArgumentException)];
        yield return ["missing eligibility binding", (Func<PayoutOperation, PayoutOperation>)(operation => operation with { EligibilityHash = " " }), typeof(ArgumentException)];
    }

    [Fact]
    public async Task Reserve_CreatesTheFifoReservationAndImmutableLedgerPosting()
    {
        var operation = CreateOperation();
        var context = new RecordingContext();
        var reservations = new RecordingReservations(operation);
        var postings = new RecordingPostings();
        var store = new InMemoryPayoutOperationStore();
        var workflow = CreateWorkflow(context, store, reservations, postings);

        var reserved = await workflow.ReserveAsync(new DurablePayoutReservationRequest(
            operation,
            CreateAuthority(operation)));

        reserved.Should().BeSameAs(operation);
        store.Get(operation.Id).Should().BeEquivalentTo(operation);
        reservations.Requests.Should().ContainSingle().Which.Should().Be(new FifoFragmentReservationRequest(
            operation.Id,
            operation.WalletId,
            CurrencyCode.HardCoin,
            ProvenanceKind.EarnedHard,
            operation.Amount,
            PersistedFragmentReservationPurpose.Payout,
            operation.CreatedAt));
        postings.Requests.Should().ContainSingle();
        var posting = postings.Requests[0];
        posting.Posting.Id.Should().Be(new PostingId(operation.Id));
        posting.Posting.Template.Kind.Should().Be(PostingTemplateKind.PayoutReservation);
        posting.Posting.Lines.Should().SatisfyRespectively(
            line => line.Should().Be(new PostingLine(1, EntrySide.Debit, EconomyAccountCode.EarnedHardLiability, operation.Amount, operation.WalletId, null, ProvenanceKind.EarnedHard)),
            line => line.Should().Be(new PostingLine(2, EntrySide.Credit, EconomyAccountCode.PayoutPayableHard, operation.Amount, null, null, null)));
        posting.Allocations.Should().ContainSingle().Which.Should().Match<RegisteredPostingAllocation>(
            allocation => allocation.LineSequence == 1 && allocation.AmountUnits == operation.Amount.Units);
        context.Transactions.Should().ContainSingle().Which.CommitCalled.Should().BeTrue();
    }

    [Fact]
    public async Task Reserve_ReturnsThePriorOperationWithoutOpeningATransactionForAnIdempotentReplay()
    {
        var operation = CreateOperation();
        var context = new RecordingContext();
        var store = new InMemoryPayoutOperationStore();
        store.Add(operation);
        var workflow = CreateWorkflow(context, store, new RecordingReservations(operation), new RecordingPostings());

        var replay = await workflow.ReserveAsync(new DurablePayoutReservationRequest(operation, CreateAuthority(operation)));

        replay.Should().BeSameAs(operation);
        context.Transactions.Should().BeEmpty();
    }

    [Fact]
    public async Task Reserve_RollsBackAndReturnsTheWinnerWhenReplayAppearsInsideTheTransaction()
    {
        var operation = CreateOperation();
        var context = new RecordingContext();
        var reservations = new RecordingReservations(operation);
        var postings = new RecordingPostings();
        var store = new ReplayOnSecondLookupStore(operation);
        var workflow = CreateWorkflow(context, store, reservations, postings);

        var replay = await workflow.ReserveAsync(new DurablePayoutReservationRequest(operation, CreateAuthority(operation)));

        replay.Should().BeSameAs(operation);
        store.AddCalled.Should().BeFalse();
        reservations.Requests.Should().BeEmpty();
        postings.Requests.Should().BeEmpty();
        context.Transactions.Should().ContainSingle().Which.RollbackCalled.Should().BeTrue();
    }

    [Fact]
    public async Task Reserve_RollsBackWithoutPostingWhenFifoFragmentsDoNotEqualThePayoutAmount()
    {
        var operation = CreateOperation();
        var context = new RecordingContext();
        var reservations = new RecordingReservations(operation, operation.Amount.Units - 1);
        var postings = new RecordingPostings();
        var workflow = CreateWorkflow(context, new InMemoryPayoutOperationStore(), reservations, postings);

        await FluentActions.Invoking(() => workflow.ReserveAsync(new DurablePayoutReservationRequest(
                operation,
                CreateAuthority(operation))))
            .Should().ThrowAsync<RegisteredPostingRejectedException>();

        postings.Requests.Should().BeEmpty();
        context.Transactions.Should().ContainSingle().Which.RollbackCalled.Should().BeTrue();
    }

    [Theory]
    [MemberData(nameof(InvalidOperations))]
    public async Task Reserve_RejectsInvalidPayoutOperationsBeforeMutatingPersistence(
        string _,
        Func<PayoutOperation, PayoutOperation> mutate,
        Type expectedException)
    {
        var operation = mutate(CreateOperation());
        var context = new RecordingContext();
        var reservations = new RecordingReservations(operation);
        var postings = new RecordingPostings();
        var workflow = CreateWorkflow(context, new InMemoryPayoutOperationStore(), reservations, postings);

        var exception = await FluentActions.Invoking(() => workflow.ReserveAsync(new DurablePayoutReservationRequest(
                operation,
                CreateAuthority(operation))))
            .Should().ThrowAsync<Exception>();

        exception.Which.Should().BeOfType(expectedException);
        context.Transactions.Should().BeEmpty();
        reservations.Requests.Should().BeEmpty();
        postings.Requests.Should().BeEmpty();
    }

    [Fact]
    public async Task Reserve_RejectsAuthoritiesThatDoNotBindTheOperationActorAndRiskDecision()
    {
        var operation = CreateOperation();
        var context = new RecordingContext();
        var workflow = CreateWorkflow(context, new InMemoryPayoutOperationStore(), new RecordingReservations(operation), new RecordingPostings());
        var differentActor = new RegisteredPostingAuthority(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), operation.RiskDecisionId, "risk", 1);
        var differentRisk = new RegisteredPostingAuthority(Guid.NewGuid(), operation.ActorId, Guid.NewGuid(), Guid.NewGuid(), "risk", 1);

        await FluentActions.Invoking(() => workflow.ReserveAsync(new DurablePayoutReservationRequest(operation, differentActor)))
            .Should().ThrowAsync<InvalidOperationException>();
        await FluentActions.Invoking(() => workflow.ReserveAsync(new DurablePayoutReservationRequest(operation, differentRisk)))
            .Should().ThrowAsync<InvalidOperationException>();

        context.Transactions.Should().BeEmpty();
    }

    private static PostgreSqlDurablePayoutReservationWorkflow CreateWorkflow(
        RecordingContext context,
        IPayoutOperationStore store,
        RecordingReservations reservations,
        RecordingPostings postings) => new(context, store, reservations, postings);

    private static PayoutOperation CreateOperation() => new(
        Guid.NewGuid(),
        new IdempotencyKey($"payout-reservation-{Guid.NewGuid():N}"),
        "request-hash",
        Guid.NewGuid(),
        Guid.NewGuid(),
        WalletId.New(),
        new CoinAmount(CurrencyCode.HardCoin, 700),
        "acct_1",
        "destination-hash",
        "provider-binding-hash",
        "eligibility-hash",
        null,
        null,
        PayoutOperationState.Reserved,
        1,
        7,
        3,
        new ReserveVersion(1),
        1,
        new PolicyVersion(1),
        Guid.NewGuid(),
        Time,
        Time);

    private static RegisteredPostingAuthority CreateAuthority(PayoutOperation operation) => new(
        Guid.NewGuid(),
        operation.ActorId,
        Guid.NewGuid(),
        operation.RiskDecisionId,
        "payout-reservation",
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

    private sealed class RecordingReservations(PayoutOperation operation, long? amountUnits = null) : IFifoFragmentReservationGateway
    {
        private readonly PersistedFragmentReservation _fragment = new(
            Guid.NewGuid(),
            operation.Id,
            CreditLotId.New(),
            SourceStampId.New(),
            0,
            new RootTraceRange(SourceStampId.New(), 0, checked(Math.Max(1, operation.Amount.Units) * 1000), 0),
            new CoinAmount(operation.Amount.Currency, amountUnits ?? operation.Amount.Units));

        public List<FifoFragmentReservationRequest> Requests { get; } = [];
        public IReadOnlyList<PersistedFragmentReservation> Reserve(FifoFragmentReservationRequest request)
        {
            Requests.Add(request);
            return [_fragment];
        }
        public long Transition(Guid operationId, PersistedFragmentReservationStatus expected, PersistedFragmentReservationStatus next, DateTimeOffset terminalAt) => 1;
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

    private sealed class ReplayOnSecondLookupStore(PayoutOperation replay) : IPayoutOperationStore
    {
        private int _lookups;
        public bool AddCalled { get; private set; }
        public PayoutOperation Get(Guid operationId) => throw new NotSupportedException();
        public IReadOnlyList<PayoutOperation> ListForPayee(Guid payeeId, int take) => throw new NotSupportedException();
        public PayoutOperation? FindReplay(string idempotencyKey, string requestHash) => ++_lookups == 2 ? replay : null;
        public void Add(PayoutOperation operation) => AddCalled = true;
        public PayoutOperation Update(PayoutOperation operation, long expectedVersion) => throw new NotSupportedException();
        public PayoutProviderEventRecord? FindProviderEvent(string eventId, string eventHash) => throw new NotSupportedException();
        public PayoutProviderEventRecord RecordProviderEvent(string eventId, string eventHash, PayoutOperation resultingOperation, long expectedVersion, DateTimeOffset recordedAt) => throw new NotSupportedException();
    }
}
