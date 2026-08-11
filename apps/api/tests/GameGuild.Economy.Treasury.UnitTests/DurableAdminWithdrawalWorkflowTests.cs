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

    [Fact]
    public async Task Reserve_ReplaysOrRollsBackWhenConcurrentAndIneligibleRequestsAreDetected()
    {
        var replayRun = CreateRun();
        var replayStore = new InMemoryAdminWithdrawalStore();
        replayStore.Add(replayRun);
        var replayContext = new RecordingContext();
        var replayWorkflow = CreateWorkflow(replayContext, replayStore, replayRun);

        (await replayWorkflow.ReserveAsync(new DurableAdminWithdrawalReservationRequest(
            replayRun, CreateAuthority(replayRun.RequestedBy)))).Should().BeSameAs(replayRun);
        replayContext.Transactions.Should().BeEmpty();

        var concurrentRun = CreateRun();
        var concurrentContext = new RecordingContext();
        var concurrentWorkflow = CreateWorkflow(
            concurrentContext,
            new ReplayOnSecondReservationLookupStore(concurrentRun),
            concurrentRun);
        (await concurrentWorkflow.ReserveAsync(new DurableAdminWithdrawalReservationRequest(
            concurrentRun, CreateAuthority(concurrentRun.RequestedBy)))).Should().BeSameAs(concurrentRun);
        concurrentContext.Transactions.Should().ContainSingle().Which.RollbackCalled.Should().BeTrue();

        var occupiedPeriod = CreateRun();
        var overlappingRun = CreateRun();
        var overlapStore = new InMemoryAdminWithdrawalStore();
        overlapStore.Add(occupiedPeriod);
        var overlapContext = new RecordingContext();
        var overlapWorkflow = CreateWorkflow(overlapContext, overlapStore, overlappingRun);
        await FluentActions.Invoking(() => overlapWorkflow.ReserveAsync(new DurableAdminWithdrawalReservationRequest(
                overlappingRun, CreateAuthority(overlappingRun.RequestedBy))))
            .Should().ThrowAsync<AdminWithdrawalOverlapException>();
        overlapContext.Transactions.Should().ContainSingle().Which.RollbackCalled.Should().BeTrue();

        var mismatchedRun = CreateRun();
        var mismatchContext = new RecordingContext();
        var mismatchWorkflow = CreateWorkflow(
            mismatchContext,
            new InMemoryAdminWithdrawalStore(),
            mismatchedRun,
            fragmentUnits: mismatchedRun.Amount.Units - 1);
        await FluentActions.Invoking(() => mismatchWorkflow.ReserveAsync(new DurableAdminWithdrawalReservationRequest(
                mismatchedRun, CreateAuthority(mismatchedRun.RequestedBy))))
            .Should().ThrowAsync<AdminWithdrawalEligibilityException>();
        mismatchContext.Transactions.Should().ContainSingle().Which.RollbackCalled.Should().BeTrue();
    }

    [Fact]
    public async Task ApprovalAndDispatch_ReplayExactCommandsAndRejectStaleLifecycleTransitions()
    {
        var approvedRun = CreateRun() with
        {
            State = AdminWithdrawalRunState.Approved,
            Version = 2,
            ApprovedBy = Guid.NewGuid()
        };
        var approvalStore = new InMemoryAdminWithdrawalStore();
        approvalStore.Add(approvedRun);
        var approvalContext = new RecordingContext();
        var approvalWorkflow = CreateWorkflow(approvalContext, approvalStore, approvedRun);
        (await approvalWorkflow.ApproveAsync(new DurableAdminWithdrawalApprovalRequest(
            approvedRun.Id, 1, approvedRun.ApprovedBy!.Value, Time.AddMinutes(1)))).Should().BeSameAs(approvedRun);
        approvalContext.Transactions.Should().ContainSingle().Which.RollbackCalled.Should().BeTrue();

        await FluentActions.Invoking(() => approvalWorkflow.ApproveAsync(new DurableAdminWithdrawalApprovalRequest(
                approvedRun.Id, 1, Guid.NewGuid(), Time.AddMinutes(1))))
            .Should().ThrowAsync<AdminWithdrawalStaleCommandException>();
        var versionStaleRun = CreateRun() with { Version = 2 };
        var versionStaleStore = new InMemoryAdminWithdrawalStore();
        versionStaleStore.Add(versionStaleRun);
        var versionStaleWorkflow = CreateWorkflow(new RecordingContext(), versionStaleStore, versionStaleRun);
        await FluentActions.Invoking(() => versionStaleWorkflow.ApproveAsync(new DurableAdminWithdrawalApprovalRequest(
                versionStaleRun.Id, 1, Guid.NewGuid(), Time.AddMinutes(1))))
            .Should().ThrowAsync<AdminWithdrawalStaleCommandException>();

        var dispatchingRun = approvedRun with
        {
            State = AdminWithdrawalRunState.Dispatching,
            Version = 3,
            DispatchSnapshotHash = "snapshot"
        };
        var dispatchStore = new InMemoryAdminWithdrawalStore();
        dispatchStore.Add(dispatchingRun);
        var dispatchContext = new RecordingContext();
        var dispatchWorkflow = CreateWorkflow(dispatchContext, dispatchStore, dispatchingRun);
        (await dispatchWorkflow.BeginDispatchAsync(new DurableAdminWithdrawalDispatchRequest(
            dispatchingRun.Id, 2, dispatchingRun.FencingToken, dispatchingRun.ExecutionEpoch, "snapshot", Time.AddMinutes(2))))
            .Should().BeSameAs(dispatchingRun);
        dispatchContext.Transactions.Should().ContainSingle().Which.RollbackCalled.Should().BeTrue();

        var staleRun = CreateRun();
        var staleStore = new InMemoryAdminWithdrawalStore();
        staleStore.Add(staleRun);
        var staleContext = new RecordingContext();
        var staleWorkflow = CreateWorkflow(staleContext, staleStore, staleRun);
        await FluentActions.Invoking(() => staleWorkflow.BeginDispatchAsync(new DurableAdminWithdrawalDispatchRequest(
                staleRun.Id, staleRun.Version, staleRun.FencingToken, staleRun.ExecutionEpoch, "snapshot", Time)))
            .Should().ThrowAsync<AdminWithdrawalStaleCommandException>();
        staleContext.Transactions.Should().ContainSingle().Which.RollbackCalled.Should().BeTrue();
    }

    [Fact]
    public async Task BeginDispatch_RejectsEveryStaleOrUnapprovedRunShape()
    {
        var approved = CreateRun() with { State = AdminWithdrawalRunState.Approved, ApprovedBy = Guid.NewGuid() };

        await AssertDispatchStaleAsync(approved with { State = AdminWithdrawalRunState.PendingApproval }, 1, approved.FencingToken, approved.ExecutionEpoch);
        await AssertDispatchStaleAsync(approved with { Version = 2 }, 1, approved.FencingToken, approved.ExecutionEpoch);
        await AssertDispatchStaleAsync(approved with { FencingToken = approved.FencingToken + 1 }, 1, approved.FencingToken, approved.ExecutionEpoch);
        await AssertDispatchStaleAsync(approved with { ExecutionEpoch = approved.ExecutionEpoch + 1 }, 1, approved.FencingToken, approved.ExecutionEpoch);
        await AssertDispatchStaleAsync(approved with { ApprovedBy = null }, 1, approved.FencingToken, approved.ExecutionEpoch);
        await AssertDispatchStaleAsync(approved with { ApprovedBy = approved.RequestedBy }, 1, approved.FencingToken, approved.ExecutionEpoch);
    }

    [Fact]
    public async Task Workflow_RejectsInvalidReservationApprovalDispatchAndProviderEventShapesBeforePersistence()
    {
        var validRun = CreateRun();
        var authority = CreateAuthority(validRun.RequestedBy);
        await AssertReserveRejectedAsync(validRun with { Id = Guid.Empty }, authority, typeof(ArgumentException));
        await AssertReserveRejectedAsync(validRun with { PeriodStart = validRun.PeriodStart.AddDays(1) }, authority, typeof(ArgumentException));
        await AssertReserveRejectedAsync(validRun with { State = AdminWithdrawalRunState.Approved }, authority, typeof(InvalidOperationException));
        await AssertReserveRejectedAsync(validRun with { Version = 2 }, authority, typeof(InvalidOperationException));
        await AssertReserveRejectedAsync(validRun with { ApprovedBy = Guid.NewGuid() }, authority, typeof(InvalidOperationException));
        await AssertReserveRejectedAsync(validRun with { Amount = new CoinAmount(CurrencyCode.SoftCoin, 500) }, authority, typeof(AdminWithdrawalEligibilityException));
        await AssertReserveRejectedAsync(validRun with { Amount = new CoinAmount(CurrencyCode.HardCoin, 0) }, authority, typeof(AdminWithdrawalEligibilityException));
        await AssertReserveRejectedAsync(validRun with { FencingToken = 0 }, authority, typeof(ArgumentOutOfRangeException));
        await AssertReserveRejectedAsync(validRun with { ExecutionEpoch = 0 }, authority, typeof(ArgumentOutOfRangeException));
        await AssertReserveRejectedAsync(validRun with { ReserveAuthorizationEpoch = 0 }, authority, typeof(ArgumentOutOfRangeException));
        await AssertReserveRejectedAsync(validRun, CreateAuthority(Guid.NewGuid()), typeof(InvalidOperationException));

        var shapeContext = new RecordingContext();
        var shapeWorkflow = CreateWorkflow(shapeContext, new InMemoryAdminWithdrawalStore(), validRun);
        await FluentActions.Invoking(() => shapeWorkflow.ApproveAsync(new DurableAdminWithdrawalApprovalRequest(
                Guid.Empty, 1, Guid.NewGuid(), Time)))
            .Should().ThrowAsync<ArgumentException>();
        await FluentActions.Invoking(() => shapeWorkflow.ApproveAsync(new DurableAdminWithdrawalApprovalRequest(
                validRun.Id, 1, Guid.Empty, Time)))
            .Should().ThrowAsync<ArgumentException>();
        await FluentActions.Invoking(() => shapeWorkflow.ApproveAsync(new DurableAdminWithdrawalApprovalRequest(
                validRun.Id, 0, Guid.NewGuid(), Time)))
            .Should().ThrowAsync<ArgumentOutOfRangeException>();
        await FluentActions.Invoking(() => shapeWorkflow.BeginDispatchAsync(new DurableAdminWithdrawalDispatchRequest(
                Guid.Empty, 1, 1, 1, "snapshot", Time)))
            .Should().ThrowAsync<ArgumentException>();
        await FluentActions.Invoking(() => shapeWorkflow.BeginDispatchAsync(new DurableAdminWithdrawalDispatchRequest(
                validRun.Id, 0, 1, 1, "snapshot", Time)))
            .Should().ThrowAsync<ArgumentOutOfRangeException>();
        await FluentActions.Invoking(() => shapeWorkflow.BeginDispatchAsync(new DurableAdminWithdrawalDispatchRequest(
                validRun.Id, 1, 0, 1, "snapshot", Time)))
            .Should().ThrowAsync<ArgumentOutOfRangeException>();
        await FluentActions.Invoking(() => shapeWorkflow.BeginDispatchAsync(new DurableAdminWithdrawalDispatchRequest(
                validRun.Id, 1, 1, 0, "snapshot", Time)))
            .Should().ThrowAsync<ArgumentOutOfRangeException>();
        await FluentActions.Invoking(() => shapeWorkflow.BeginDispatchAsync(new DurableAdminWithdrawalDispatchRequest(
                validRun.Id, 1, 1, 1, new string('x', 129), Time)))
            .Should().ThrowAsync<ArgumentException>();
        var missingRunId = CreateProviderEvent(validRun, AdminWithdrawalProviderOutcome.Failed) with { RunId = Guid.Empty };
        await FluentActions.Invoking(() => shapeWorkflow.ApplyProviderEventAsync(new DurableAdminWithdrawalProviderEventRequest(
                missingRunId, CreateAuthority(Guid.NewGuid()))))
            .Should().ThrowAsync<ArgumentException>();
        var nonTerminalEvent = CreateProviderEvent(validRun, AdminWithdrawalProviderOutcome.Failed) with { Outcome = AdminWithdrawalProviderOutcome.Submitted };
        await FluentActions.Invoking(() => shapeWorkflow.ApplyProviderEventAsync(new DurableAdminWithdrawalProviderEventRequest(
                nonTerminalEvent, CreateAuthority(Guid.NewGuid()))))
            .Should().ThrowAsync<AdminWithdrawalEvidenceException>();
        shapeContext.Transactions.Should().BeEmpty();
    }

    [Fact]
    public async Task ProviderEvent_RejectsInvalidEvidenceAndHandlesConcurrentOrStaleTerminalUpdates()
    {
        var dispatchingRun = CreateRun() with
        {
            State = AdminWithdrawalRunState.Dispatching,
            Version = 3,
            ApprovedBy = Guid.NewGuid(),
            DispatchSnapshotHash = "snapshot"
        };
        var rejectContext = new RecordingContext();
        var rejectWorkflow = CreateWorkflow(
            rejectContext,
            new InMemoryAdminWithdrawalStore(),
            dispatchingRun,
            evidence: new RejectEvidence());
        await FluentActions.Invoking(() => rejectWorkflow.ApplyProviderEventAsync(new DurableAdminWithdrawalProviderEventRequest(
                CreateProviderEvent(dispatchingRun, AdminWithdrawalProviderOutcome.Failed), CreateAuthority(Guid.NewGuid()))))
            .Should().ThrowAsync<AdminWithdrawalEvidenceException>();
        rejectContext.Transactions.Should().BeEmpty();

        var replayContext = new RecordingContext();
        var replayWorkflow = CreateWorkflow(
            replayContext,
            new ReplayOnSecondProviderEventLookupStore(dispatchingRun),
            dispatchingRun);
        (await replayWorkflow.ApplyProviderEventAsync(new DurableAdminWithdrawalProviderEventRequest(
            CreateProviderEvent(dispatchingRun, AdminWithdrawalProviderOutcome.Failed), CreateAuthority(Guid.NewGuid()))))
            .Should().BeSameAs(dispatchingRun);
        replayContext.Transactions.Should().ContainSingle().Which.RollbackCalled.Should().BeTrue();

        var zeroTransitionStore = new InMemoryAdminWithdrawalStore();
        zeroTransitionStore.Add(dispatchingRun);
        var zeroTransitionContext = new RecordingContext();
        var zeroTransitionWorkflow = CreateWorkflow(
            zeroTransitionContext,
            zeroTransitionStore,
            dispatchingRun,
            transitionCount: 0);
        await FluentActions.Invoking(() => zeroTransitionWorkflow.ApplyProviderEventAsync(new DurableAdminWithdrawalProviderEventRequest(
                CreateProviderEvent(dispatchingRun, AdminWithdrawalProviderOutcome.Failed), CreateAuthority(Guid.NewGuid()))))
            .Should().ThrowAsync<AdminWithdrawalStaleCommandException>();
        zeroTransitionContext.Transactions.Should().ContainSingle().Which.RollbackCalled.Should().BeTrue();

        var unorderedRun = CreateRun() with { State = AdminWithdrawalRunState.Approved, Version = 2, ApprovedBy = Guid.NewGuid() };
        var unorderedStore = new InMemoryAdminWithdrawalStore();
        unorderedStore.Add(unorderedRun);
        var unorderedContext = new RecordingContext();
        var unorderedWorkflow = CreateWorkflow(unorderedContext, unorderedStore, unorderedRun);
        await FluentActions.Invoking(() => unorderedWorkflow.ApplyProviderEventAsync(new DurableAdminWithdrawalProviderEventRequest(
                CreateProviderEvent(unorderedRun, AdminWithdrawalProviderOutcome.Failed), CreateAuthority(Guid.NewGuid()))))
            .Should().ThrowAsync<AdminWithdrawalStaleCommandException>();

        var predatedStore = new InMemoryAdminWithdrawalStore();
        predatedStore.Add(dispatchingRun);
        var predatedContext = new RecordingContext();
        var predatedWorkflow = CreateWorkflow(predatedContext, predatedStore, dispatchingRun);
        var predated = CreateProviderEvent(dispatchingRun, AdminWithdrawalProviderOutcome.Failed) with { ObservedAt = Time.AddMinutes(-1) };
        await FluentActions.Invoking(() => predatedWorkflow.ApplyProviderEventAsync(new DurableAdminWithdrawalProviderEventRequest(
                predated, CreateAuthority(Guid.NewGuid()))))
            .Should().ThrowAsync<AdminWithdrawalEvidenceException>();
    }

    [Fact]
    public async Task ProviderEvent_RejectsEveryBindingMismatchAndPersistsAValidFailure()
    {
        var dispatching = CreateRun() with
        {
            State = AdminWithdrawalRunState.Dispatching,
            Version = 3,
            ApprovedBy = Guid.NewGuid(),
            DispatchSnapshotHash = "snapshot"
        };
        var validFailure = CreateProviderEvent(dispatching, AdminWithdrawalProviderOutcome.Failed);

        await AssertTerminalEvidenceRejectedAsync(dispatching, validFailure with { FencingToken = dispatching.FencingToken + 1 });
        await AssertTerminalEvidenceRejectedAsync(dispatching, validFailure with { ExecutionEpoch = dispatching.ExecutionEpoch + 1 });
        await AssertTerminalEvidenceRejectedAsync(dispatching, validFailure with { Amount = new CoinAmount(CurrencyCode.HardCoin, 1) });
        await AssertTerminalEvidenceRejectedAsync(dispatching, validFailure with { SourceAssetKey = "other-source" });
        await AssertTerminalEvidenceRejectedAsync(dispatching, validFailure with { DestinationHash = "other-destination" });
        await AssertTerminalEvidenceRejectedAsync(
            dispatching with { ProviderTransferId = "expected-transfer" },
            validFailure);

        var failureStore = new InMemoryAdminWithdrawalStore();
        failureStore.Add(dispatching);
        var failureContext = new RecordingContext();
        var failureWorkflow = CreateWorkflow(failureContext, failureStore, dispatching);
        var failed = await failureWorkflow.ApplyProviderEventAsync(new DurableAdminWithdrawalProviderEventRequest(
            validFailure,
            CreateAuthority(Guid.NewGuid())));

        failed.State.Should().Be(AdminWithdrawalRunState.Failed);
        failureContext.Transactions.Should().ContainSingle().Which.CommitCalled.Should().BeTrue();
    }

    private static async Task AssertReserveRejectedAsync(
        AdminWithdrawalRun run,
        RegisteredPostingAuthority authority,
        Type expectedException)
    {
        var context = new RecordingContext();
        var workflow = CreateWorkflow(context, new InMemoryAdminWithdrawalStore(), CreateRun());
        var exception = await FluentActions.Invoking(() => workflow.ReserveAsync(new DurableAdminWithdrawalReservationRequest(run, authority)))
            .Should().ThrowAsync<Exception>();
        exception.Which.Should().BeOfType(expectedException);
        context.Transactions.Should().BeEmpty();
    }

    private static async Task AssertDispatchStaleAsync(
        AdminWithdrawalRun run,
        long expectedVersion,
        long fencingToken,
        long executionEpoch)
    {
        var store = new InMemoryAdminWithdrawalStore();
        store.Add(run);
        var context = new RecordingContext();
        var workflow = CreateWorkflow(context, store, run);
        await FluentActions.Invoking(() => workflow.BeginDispatchAsync(new DurableAdminWithdrawalDispatchRequest(
                run.Id, expectedVersion, fencingToken, executionEpoch, "snapshot", Time)))
            .Should().ThrowAsync<AdminWithdrawalStaleCommandException>();
        context.Transactions.Should().ContainSingle().Which.RollbackCalled.Should().BeTrue();
    }

    private static async Task AssertTerminalEvidenceRejectedAsync(
        AdminWithdrawalRun run,
        AdminWithdrawalProviderEvent providerEvent)
    {
        var store = new InMemoryAdminWithdrawalStore();
        store.Add(run);
        var context = new RecordingContext();
        var workflow = CreateWorkflow(context, store, run);
        await FluentActions.Invoking(() => workflow.ApplyProviderEventAsync(new DurableAdminWithdrawalProviderEventRequest(
                providerEvent,
                CreateAuthority(Guid.NewGuid()))))
            .Should().ThrowAsync<AdminWithdrawalEvidenceException>();
        context.Transactions.Should().ContainSingle().Which.RollbackCalled.Should().BeTrue();
    }

    private static PostgreSqlDurableAdminWithdrawalWorkflow CreateWorkflow(
        RecordingContext context,
        IAdminWithdrawalStore operations,
        AdminWithdrawalRun reservationRun,
        long? fragmentUnits = null,
        long transitionCount = 1,
        IAdminWithdrawalProviderEvidenceVerifier? evidence = null) => new(
        context,
        operations,
        new AdminWithdrawalAuditTrail(),
        new RecordingReservations(reservationRun, fragmentUnits, transitionCount),
        new RecordingPostings(),
        evidence ?? new AcceptEvidence());

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

    private sealed class RecordingReservations(
        AdminWithdrawalRun run,
        long? fragmentUnits = null,
        long transitionCount = 1) : IFifoFragmentReservationGateway
    {
        private readonly PersistedFragmentReservation _fragment = new(
            Guid.NewGuid(),
            run.Id,
            CreditLotId.New(),
            SourceStampId.New(),
            0,
            new RootTraceRange(SourceStampId.New(), 0, checked(run.Amount.Units * 1000), 0),
            new CoinAmount(run.Amount.Currency, fragmentUnits ?? run.Amount.Units));
        public List<ReservationTransition> Transitions { get; } = [];
        public IReadOnlyList<PersistedFragmentReservation> Reserve(FifoFragmentReservationRequest request) => [_fragment];
        public long Transition(Guid operationId, PersistedFragmentReservationStatus expected, PersistedFragmentReservationStatus next, DateTimeOffset terminalAt)
        {
            Transitions.Add(new ReservationTransition(operationId, expected, next, terminalAt));
            return transitionCount;
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

    private sealed class RejectEvidence : IAdminWithdrawalProviderEvidenceVerifier
    {
        public bool Verify(AdminWithdrawalProviderReceipt receipt) => false;
        public bool Verify(AdminWithdrawalProviderEvent providerEvent) => false;
    }

    private sealed class ReplayOnSecondReservationLookupStore(AdminWithdrawalRun run) : IAdminWithdrawalStore
    {
        private int _replayLookups;
        public AdminWithdrawalRun? FindReplay(string key, string requestHash) => ++_replayLookups == 2 ? run : null;
        public AdminWithdrawalRun? FindPeriod(DateOnly periodStart) => throw new NotSupportedException();
        public void Add(AdminWithdrawalRun withdrawalRun) => throw new NotSupportedException();
        public AdminWithdrawalRun Get(Guid runId) => throw new NotSupportedException();
        public AdminWithdrawalRun Update(AdminWithdrawalRun withdrawalRun, long expectedVersion) => throw new NotSupportedException();
        public Guid? FindProviderEvent(string eventId, string eventHash) => throw new NotSupportedException();
        public void RecordProviderEvent(string eventId, string eventHash, AdminWithdrawalRun withdrawalRun, long expectedVersion) => throw new NotSupportedException();
    }

    private sealed class ReplayOnSecondProviderEventLookupStore(AdminWithdrawalRun run) : IAdminWithdrawalStore
    {
        private int _providerEventLookups;
        public AdminWithdrawalRun? FindReplay(string key, string requestHash) => throw new NotSupportedException();
        public AdminWithdrawalRun? FindPeriod(DateOnly periodStart) => throw new NotSupportedException();
        public void Add(AdminWithdrawalRun withdrawalRun) => throw new NotSupportedException();
        public AdminWithdrawalRun Get(Guid runId) => run;
        public AdminWithdrawalRun Update(AdminWithdrawalRun withdrawalRun, long expectedVersion) => throw new NotSupportedException();
        public Guid? FindProviderEvent(string eventId, string eventHash) => ++_providerEventLookups == 2 ? run.Id : null;
        public void RecordProviderEvent(string eventId, string eventHash, AdminWithdrawalRun withdrawalRun, long expectedVersion) => throw new NotSupportedException();
    }

    private sealed record ReservationTransition(
        Guid OperationId,
        PersistedFragmentReservationStatus Expected,
        PersistedFragmentReservationStatus Next,
        DateTimeOffset TerminalAt);
}
