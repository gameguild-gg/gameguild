using FluentAssertions;
using GameGuild;
using GameGuild.Economy.Contracts;
using GameGuild.Economy.Ledger;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace GameGuild.Economy.Treasury.UnitTests;

public sealed class DurableAdminWithdrawalWorkflowTests
{
    private static readonly DateTimeOffset Time = new(2026, 8, 10, 20, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Workflow_ReservesApprovesDispatchesAndSettlesWithAnImmutableAuditTrail()
    {
        var run = CreateRun();
        var context = new RecordingContext();
        var operations = new InMemoryAdminWithdrawalStore();
        var audit = new AdminWithdrawalAuditTrail();
        var reservations = new RecordingReservations(run);
        var postings = new RecordingPostings();
        var workflow = new PostgreSqlDurableAdminWithdrawalWorkflow(
            context, operations, audit, reservations, postings, new AcceptEvidence());

        var reserved = await workflow.ReserveAsync(new DurableAdminWithdrawalReservationRequest(
            run, CreateAuthority(run.RequestedBy)));
        var approved = await workflow.ApproveAsync(new DurableAdminWithdrawalApprovalRequest(
            run.Id, reserved.Version, Guid.NewGuid(), Time.AddMinutes(1)));
        var dispatching = await workflow.BeginDispatchAsync(new DurableAdminWithdrawalDispatchRequest(
            run.Id, approved.Version, run.FencingToken, run.ExecutionEpoch, "custody-snapshot", Time.AddMinutes(2)));
        var providerEvent = CreateProviderEvent(dispatching, AdminWithdrawalProviderOutcome.Succeeded);
        var terminal = await workflow.ApplyProviderEventAsync(new DurableAdminWithdrawalProviderEventRequest(
            providerEvent, CreateAuthority(Guid.NewGuid())));

        terminal.State.Should().Be(AdminWithdrawalRunState.Succeeded);
        terminal.Version.Should().Be(4);
        terminal.ProviderTransferId.Should().Be(providerEvent.ProviderTransferId);
        postings.Requests.Select(request => request.Posting.Template.Kind).Should().Equal(
            PostingTemplateKind.AdminWithdrawalReservation,
            PostingTemplateKind.AdminWithdrawalSuccess);
        reservations.Transitions.Should().ContainSingle().Which.Should().Be(new ReservationTransition(
            run.Id,
            PersistedFragmentReservationStatus.Reserved,
            PersistedFragmentReservationStatus.Consumed,
            providerEvent.ObservedAt));
        audit.Events(run.Id).Select(item => item.Kind).Should().Equal("reserved", "approved", "dispatching", "succeeded");
        audit.Verify(run.Id).Should().BeTrue();
        context.Transactions.Should().HaveCount(4);
        context.Transactions.Should().OnlyContain(transaction => transaction.CommitCalled);

        var replay = await workflow.ApplyProviderEventAsync(new DurableAdminWithdrawalProviderEventRequest(
            providerEvent, CreateAuthority(Guid.NewGuid())));
        replay.Should().BeEquivalentTo(terminal);
        postings.Requests.Should().HaveCount(2);
        reservations.Transitions.Should().ContainSingle();
    }

    [Fact]
    public async Task Approval_RejectsRequesterAndProviderEvent_RejectsUnboundOrPrematureEvents()
    {
        var run = CreateRun();
        var context = new RecordingContext();
        var operations = new InMemoryAdminWithdrawalStore();
        operations.Add(run);
        var workflow = new PostgreSqlDurableAdminWithdrawalWorkflow(
            context,
            operations,
            new AdminWithdrawalAuditTrail(),
            new RecordingReservations(run),
            new RecordingPostings(),
            new AcceptEvidence());

        await FluentActions.Invoking(() => workflow.ApproveAsync(new DurableAdminWithdrawalApprovalRequest(
                run.Id, run.Version, run.RequestedBy, Time.AddMinutes(1))))
            .Should().ThrowAsync<AdminWithdrawalApprovalException>();

        var approved = run with
        {
            ApprovedBy = Guid.NewGuid(),
            State = AdminWithdrawalRunState.Dispatching,
            Version = 2,
            DispatchSnapshotHash = "snapshot",
            UpdatedAt = Time.AddMinutes(1)
        };
        operations.Update(approved, run.Version);
        var badEvent = CreateProviderEvent(approved, AdminWithdrawalProviderOutcome.Failed) with
        {
            DestinationHash = "wrong"
        };
        await FluentActions.Invoking(() => workflow.ApplyProviderEventAsync(new DurableAdminWithdrawalProviderEventRequest(
                badEvent, CreateAuthority(Guid.NewGuid()))))
            .Should().ThrowAsync<AdminWithdrawalEvidenceException>();
    }

    private static AdminWithdrawalRun CreateRun() => new(
        Guid.NewGuid(),
        new IdempotencyKey($"admin-withdrawal-{Guid.NewGuid():N}"),
        "request-hash",
        new DateOnly(2026, 8, 1),
        Guid.NewGuid(),
        null,
        WalletId.New(),
        new CoinAmount(CurrencyCode.HardCoin, 500),
        "primary-hard-reserve",
        "destination-hash",
        AdminWithdrawalRunState.PendingApproval,
        1,
        7,
        2,
        new ReserveVersion(1),
        1,
        new PolicyVersion(1),
        null,
        null,
        Time,
        Time);

    private static AdminWithdrawalProviderEvent CreateProviderEvent(
        AdminWithdrawalRun run,
        AdminWithdrawalProviderOutcome outcome) => new(
        $"evt_{Guid.NewGuid():N}",
        run.Id,
        outcome,
        "transfer-1",
        run.FencingToken,
        run.ExecutionEpoch,
        run.Amount,
        run.SourceAssetKey,
        run.DestinationHash,
        "provider-evidence",
        "signature",
        Time.AddMinutes(3));

    private static RegisteredPostingAuthority CreateAuthority(Guid actorId) => new(
        Guid.NewGuid(), actorId, Guid.NewGuid(), Guid.NewGuid(), "economy-admin-withdrawal", 1);

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

    private sealed class RecordingReservations(AdminWithdrawalRun run) : IFifoFragmentReservationGateway
    {
        private readonly PersistedFragmentReservation _fragment = new(
            Guid.NewGuid(),
            run.Id,
            CreditLotId.New(),
            SourceStampId.New(),
            0,
            new RootTraceRange(SourceStampId.New(), 0, checked(run.Amount.Units * 1000), 0),
            run.Amount);
        public List<ReservationTransition> Transitions { get; } = [];
        public IReadOnlyList<PersistedFragmentReservation> Reserve(FifoFragmentReservationRequest request) => [_fragment];
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

    private sealed class AcceptEvidence : IAdminWithdrawalProviderEvidenceVerifier
    {
        public bool Verify(AdminWithdrawalProviderReceipt receipt) => true;
        public bool Verify(AdminWithdrawalProviderEvent providerEvent) => true;
    }

    private sealed record ReservationTransition(
        Guid OperationId,
        PersistedFragmentReservationStatus Expected,
        PersistedFragmentReservationStatus Next,
        DateTimeOffset TerminalAt);
}