using FluentAssertions;
using GameGuild.Finance.Economy.Contracts;
using GameGuild.Finance.Economy.Persistence;
using GameGuild.Finance.Economy.Risk;
using GameGuild.TestSupport.Finance.Economy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace GameGuild.Compliance.FinancialCrime.UnitTests;

/// <summary>
/// Coverage completion for the issue #1291 option-C ingestion surface: every constructor,
/// validation predicate and branch of the signal outbox, the ingestion/rescreening workers
/// and the economy adapter is exercised explicitly (the economy CI lane enforces full line
/// and branch coverage of this assembly).
/// </summary>
public sealed class FinancialCrimeIngestionCoverageTests
{
    private static readonly Guid TenantId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid ActorId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;

    [Fact]
    public async Task QueueClaimCompleteFailAndReclaimBehavioursAreExplicitlyCovered()
    {
        await using var database = await EconomyPostgreSqlTestDatabase.CreateAsync("fc_queue_coverage");
        await using var context = CreateContext(database.ConnectionString);
        await context.Database.EnsureCreatedAsync();
        var queue = new PostgreSqlFinancialCrimeSignalQueue(context);

        // Validation predicates.
        await FluentActions.Awaiting(() => queue.EnqueueEconomyOperationAsync(null!).AsTask()).Should().ThrowAsync<ArgumentNullException>();
        await FluentActions.Awaiting(() => queue.EnqueueKycDecisionAsync(null!).AsTask()).Should().ThrowAsync<ArgumentNullException>();
        await FluentActions.Awaiting(() => queue.EnqueueEconomyOperationAsync(Economy(Guid.Empty, 1)).AsTask())
            .Should().ThrowAsync<ArgumentException>();
        await FluentActions.Awaiting(() => queue.EnqueueEconomyOperationAsync(Economy(TenantId, 1) with { SubjectHash = " " }).AsTask())
            .Should().ThrowAsync<ArgumentException>();
        await FluentActions.Awaiting(() => queue.ClaimDueAsync(0, "worker", Now, TimeSpan.FromMinutes(1)).AsTask())
            .Should().ThrowAsync<ArgumentOutOfRangeException>();
        await FluentActions.Awaiting(() => queue.ClaimDueAsync(1, " ", Now, TimeSpan.FromMinutes(1)).AsTask())
            .Should().ThrowAsync<ArgumentException>();
        await FluentActions.Awaiting(() => queue.CompleteAsync(Guid.Empty, Now).AsTask()).Should().ThrowAsync<ArgumentException>();
        await FluentActions.Awaiting(() => queue.FailAsync(Guid.Empty, "error", Now, 1, TimeSpan.Zero).AsTask())
            .Should().ThrowAsync<ArgumentException>();
        await FluentActions.Awaiting(() => queue.FailAsync(Guid.NewGuid(), " ", Now, 1, TimeSpan.Zero).AsTask())
            .Should().ThrowAsync<ArgumentException>();

        // Enqueue + dedupe: the identical canonical payload never double-enqueues.
        await queue.EnqueueEconomyOperationAsync(Economy(TenantId, 10));
        await queue.EnqueueEconomyOperationAsync(Economy(TenantId, 10));
        (await context.Set<FinancialCrimeSignalOutboxRow>().CountAsync()).Should().Be(1);
        await queue.EnqueueKycDecisionAsync(Kyc("kyc-subject", "Approved", "GREEN", "kyc-coverage-event"));
        (await context.Set<FinancialCrimeSignalOutboxRow>().CountAsync()).Should().Be(2);

        // Claim (attempts increment), future NextAttemptAt is not claimable, reclaim after
        // the lease, retry re-queues with a delay, dead-letter at the attempt ceiling.
        var claimed = await queue.ClaimDueAsync(10, "worker-1", Now, TimeSpan.FromMinutes(5));
        claimed.Should().HaveCount(2).And.OnlyContain(row => row.Attempts == 1);
        (await queue.ClaimDueAsync(10, "worker-1", Now.AddMinutes(1), TimeSpan.FromMinutes(5)))
            .Should().BeEmpty("both rows are Processing inside the claim lease");

        // Reclaim after the lease expires.
        var reclaimed = await queue.ClaimDueAsync(10, "worker-2", Now.AddMinutes(6), TimeSpan.FromMinutes(5));
        reclaimed.Should().HaveCount(2).And.OnlyContain(row => row.Attempts == 2);

        // Retry path: below the ceiling the row returns to Pending behind a retry delay.
        var retryRow = reclaimed.Single(row => row.Kind == FinancialCrimeSignalKind.EconomyOperation);
        await queue.FailAsync(retryRow.Id, "transient", Now.AddMinutes(6), 5, TimeSpan.FromMinutes(1));
        (await queue.ClaimDueAsync(10, "worker-1", Now.AddMinutes(6), TimeSpan.FromMinutes(5)))
            .Should().BeEmpty("the retry delay has not elapsed yet");
        var afterDelay = await queue.ClaimDueAsync(10, "worker-1", Now.AddMinutes(8), TimeSpan.FromMinutes(5));
        afterDelay.Should().ContainSingle(row => row.Id == retryRow.Id);
        await queue.CompleteAsync(retryRow.Id, Now.AddMinutes(8));

        // Dead-letter path: at the ceiling the row terminal-fails with the error recorded.
        var deadRow = reclaimed.Single(row => row.Kind == FinancialCrimeSignalKind.KycDecision);
        await queue.FailAsync(deadRow.Id, "poison", Now.AddMinutes(6), 2, TimeSpan.FromMinutes(1));
        var dead = await context.Set<FinancialCrimeSignalOutboxRow>().SingleAsync(row => row.Id == deadRow.Id);
        dead.Status.Should().Be(FinancialCrimeSignalOutboxStatus.DeadLettered);
        dead.LastError.Should().Be("poison");
        (await queue.ClaimDueAsync(10, "worker-1", Now.AddHours(1), TimeSpan.FromMinutes(5)))
            .Should().BeEmpty("terminal rows are never claimed");
    }

    [Fact]
    public async Task WorkerHandlesEverySignalTierFailureModeAndCancellation()
    {
        await using var database = await EconomyPostgreSqlTestDatabase.CreateAsync("fc_worker_coverage");
        await using var context = CreateContext(database.ConnectionString);
        await context.Database.EnsureCreatedAsync();
        var fixture = new WiringFixture(context);

        // Constructor null predicates.
        Action nullHook = () => new FinancialCrimeSignalIngestionWorker(
            fixture.Store, fixture.Control, context, null!, Options.Create(new FinancialCrimeIngestionWorkerOptions()),
            TimeProvider.System);
        Action nullWorker = () => new FinancialCrimeSignalIngestionWorker(
            fixture.Store, fixture.Control, context, Options.Create(new FinancialCrimeEconomyHookOptions()), null!,
            TimeProvider.System);
        nullHook.Should().Throw<ArgumentNullException>();
        nullWorker.Should().Throw<ArgumentNullException>();

        // Adverse-by-answer (state not Rejected, review answer RED): signal + case.
        await fixture.Queue.EnqueueKycDecisionAsync(
            Kyc(EconomySubjectReference.ForUser(TenantId, ActorId), "InReview", "RED", "answer-adverse"));
        (await fixture.IngestionWorker.ProcessDueAsync()).Should().Be(1);
        (await context.Set<FinancialCrimeCaseRow>().CountAsync()).Should().Be(1);

        // Mid tier (not adverse, not approved): NeedsReview screening.
        await fixture.Queue.EnqueueKycDecisionAsync(
            Kyc(EconomySubjectReference.ForUser(TenantId, ActorId), "ApplicantPending", null, "mid-tier"));
        (await fixture.IngestionWorker.ProcessDueAsync()).Should().Be(1);
        var screening = await context.Set<FinancialCrimeScreeningRow>()
            .SingleAsync(row => row.ProviderEventId == "kyc:mid-tier");
        screening.Outcome.Should().Be(FinancialCrimeOutcome.NeedsReview);

        // Future-observed routine operation covers the MaxOf(issuedAt, now) forward branch.
        await fixture.Queue.EnqueueEconomyOperationAsync(Economy(TenantId, 10, observedAt: Now.AddHours(1)));
        (await fixture.IngestionWorker.ProcessDueAsync()).Should().Be(1);

        // Unknown signal kind: the row is faulted with the error recorded, never lost.
        context.Set<FinancialCrimeSignalOutboxRow>().Add(new FinancialCrimeSignalOutboxRow
        {
            Id = Guid.NewGuid(), Kind = (FinancialCrimeSignalKind)99, TenantId = TenantId,
            SubjectHash = "subject", PayloadJson = "{}", PayloadHash = $"unknown-{Guid.NewGuid():N}",
            Status = FinancialCrimeSignalOutboxStatus.Pending, Attempts = 0, CreatedAt = Now
        });
        await context.SaveChangesAsync();
        await fixture.IngestionWorker.ProcessDueAsync();
        var unknown = await context.Set<FinancialCrimeSignalOutboxRow>()
            .SingleAsync(row => row.PayloadHash.StartsWith("unknown-"));
        unknown.LastError.Should().Contain("Unknown signal kind");

        // Poison payload (JSON null): deserialization failure faults the row.
        context.Set<FinancialCrimeSignalOutboxRow>().Add(new FinancialCrimeSignalOutboxRow
        {
            Id = Guid.NewGuid(), Kind = FinancialCrimeSignalKind.EconomyOperation, TenantId = TenantId,
            SubjectHash = "subject", PayloadJson = "null", PayloadHash = $"null-{Guid.NewGuid():N}",
            Status = FinancialCrimeSignalOutboxStatus.Pending, Attempts = 0, CreatedAt = Now
        });
        await context.SaveChangesAsync();
        await fixture.IngestionWorker.ProcessDueAsync();
        var poison = await context.Set<FinancialCrimeSignalOutboxRow>()
            .SingleAsync(row => row.PayloadHash.StartsWith("null-"));
        poison.LastError.Should().NotBeNullOrWhiteSpace();

        // A relational context is required by the ingestion path: the fault is contained per
        // row (the claimed row is faulted, the pass keeps going).
        await fixture.Queue.EnqueueEconomyOperationAsync(Economy(TenantId, 20, observedAt: Now.AddMinutes(-1)));
        var relationalEntry = (await fixture.Store.ClaimDueAsync(10, "worker", Now, TimeSpan.FromMinutes(5)))
            .Single(row => row.Kind == FinancialCrimeSignalKind.EconomyOperation &&
                           row.PayloadJson.Contains("key-20"));
        var nonRelationalWorker = new FinancialCrimeSignalIngestionWorker(
            fixture.Store, fixture.Control, new NonDbContext(), Options.Create(new FinancialCrimeEconomyHookOptions()),
            Options.Create(new FinancialCrimeIngestionWorkerOptions()), TimeProvider.System);
        await nonRelationalWorker.ProcessOneAsync(relationalEntry, CancellationToken.None);
        (await context.Set<FinancialCrimeSignalOutboxRow>().SingleAsync(row => row.Id == relationalEntry.Id))
            .LastError.Should().NotBeNullOrWhiteSpace();

        // A cancelled pass rethrows cancellation instead of faulting the row.
        using var cancelledSource = new CancellationTokenSource();
        cancelledSource.Cancel();
        await FluentActions.Awaiting(() => fixture.IngestionWorker.ProcessOneAsync(
                new FinancialCrimeSignalOutboxEntry(Guid.NewGuid(), FinancialCrimeSignalKind.EconomyOperation, "{}", 1),
                cancelledSource.Token))
            .Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task RescreeningSkipsExternalProvidersRecoversConflictsAndStampsPolicyVersion()
    {
        await using var database = await EconomyPostgreSqlTestDatabase.CreateAsync("fc_rescreen_coverage");
        await using var context = CreateContext(database.ConnectionString);
        await context.Database.EnsureCreatedAsync();
        var evidence = new PostgreSqlComplianceEvidenceStore(context);
        var control = new PostgreSqlFinancialCrimeControlPlane(
            context, evidence, evidence, new PostgreSqlComplianceHoldStore(context));
        var hook = Options.Create(new FinancialCrimeEconomyHookOptions());
        var workerOptions = Options.Create(new FinancialCrimeRescreeningWorkerOptions());

        Action nullHook = () => new FinancialCrimeRescreeningWorker(
            control, context, null!, workerOptions, TimeProvider.System);
        Action nullWorker = () => new FinancialCrimeRescreeningWorker(
            control, context, hook, null!, TimeProvider.System);
        nullHook.Should().Throw<ArgumentNullException>();
        nullWorker.Should().Throw<ArgumentNullException>();

        // Due EXTERNAL screening: skipped (a provider ingress owns those).
        await control.IngestScreeningAsync(External("sumsub", "ext-1", Now.AddMinutes(-1)), CancellationToken.None);
        var externalWorker = new FinancialCrimeRescreeningWorker(
            control, context, hook, workerOptions, TimeProvider.System);
        (await externalWorker.RunAsync()).Should().Be(0);
        (await context.Set<FinancialCrimeScreeningRow>().CountAsync()).Should().Be(1);

        // Due INTERNAL screening with a LOWER policy version than the hook: refreshed at the
        // hook's version (Math.Max forward branch).
        var highPolicy = new FinancialCrimeEconomyHookOptions { PolicyVersion = 9 };
        await control.IngestScreeningAsync(Internal("eco-1", 5, Now.AddMinutes(-1)), CancellationToken.None);
        var internalWorker = new FinancialCrimeRescreeningWorker(
            control, context, Options.Create(highPolicy), workerOptions, TimeProvider.System);
        (await internalWorker.RunAsync()).Should().Be(1);
        var refreshed = await context.Set<FinancialCrimeScreeningRow>()
            .SingleAsync(row => row.ProviderEventId == $"rescreen:{EconomySubjectReference.ForUser(TenantId, ActorId)}:2");
        refreshed.PolicyVersion.Should().Be(9);

        // Version race: the stubbed plane reports the due screening, then conflicts on ingest.
        var conflictPlane = new StubControlPlane(Internal("eco-2", 5, Now.AddMinutes(-1)));
        var conflictWorker = new FinancialCrimeRescreeningWorker(
            conflictPlane, context, hook, workerOptions, TimeProvider.System);
        (await conflictWorker.RunAsync()).Should().Be(0);

        // A relational context is required for the version read.
        var nonRelationalWorker = new FinancialCrimeRescreeningWorker(
            new StubControlPlane(Internal("eco-3", 5, Now.AddMinutes(-1))), new NonDbContext(),
            hook, workerOptions, TimeProvider.System);
        await FluentActions.Awaiting(() => nonRelationalWorker.RunAsync())
            .Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task AdapterCoversGateTiersFaultContainmentAndCounterpartyBranches()
    {
        await using var database = await EconomyPostgreSqlTestDatabase.CreateAsync("fc_adapter_coverage");
        await using var context = CreateContext(database.ConnectionString);
        await context.Database.EnsureCreatedAsync();
        var fixture = new WiringFixture(context);
        var services = fixture.BuildAdapterServices();
        var subjectHash = EconomySubjectReference.ForUser(TenantId, ActorId);

        Action nullInner = () => new EconomyFinancialCrimeSignalAdapter(
            null!, fixture.ScopeFactory, Options.Create(new FinancialCrimeEconomyHookOptions()), TimeProvider.System);
        Action nullHook = () => new EconomyFinancialCrimeSignalAdapter(
            fixture.Inner, fixture.ScopeFactory, null!, TimeProvider.System);
        nullInner.Should().Throw<ArgumentNullException>();
        nullHook.Should().Throw<ArgumentNullException>();

        using (var scope = services.CreateScope())
        {
            var adapter = scope.ServiceProvider.GetRequiredService<IEconomyTrustedProtectedOperationAuthorizer>();
            await FluentActions.Awaiting(() => adapter.ExecuteAsync<object>(
                    TenantId, ActorId, null!, (_, _) => Task.FromResult(new object()), CancellationToken.None))
                .Should().ThrowAsync<ArgumentNullException>();
            await FluentActions.Awaiting(() => adapter.ExecuteAsync<object>(
                    TenantId, ActorId, Intent(EconomyValueMovementCapability.Transfer), null!,
                    CancellationToken.None))
                .Should().ThrowAsync<ArgumentNullException>();

            // Non-signaled capability: the operation completes, nothing is enqueued.
            await adapter.ExecuteAsync<object>(
                TenantId, ActorId, Intent(EconomyValueMovementCapability.IssueAdReward),
                (_, _) => Task.FromResult(new object()), CancellationToken.None);
            (await context.Set<FinancialCrimeSignalOutboxRow>().CountAsync()).Should().Be(0);

            // Signaled capability with an explicit protected subject and a distinct
            // destination wallet: the counterparty and protected-subject branches.
            await adapter.ExecuteAsync<object>(
                TenantId, ActorId,
                Intent(EconomyValueMovementCapability.MarketplaceSettlement) with
                {
                    SourceWalletId = new WalletId(Guid.NewGuid()),
                    DestinationWalletId = new WalletId(Guid.NewGuid()),
                    ProtectedSubjectId = ReviewerId()
                },
                (_, _) => Task.FromResult(new object()), CancellationToken.None);
            (await context.Set<FinancialCrimeSignalOutboxRow>().CountAsync()).Should().Be(1);

            // Same source and destination wallet: no counterparty is carried.
            var wallet = new WalletId(Guid.NewGuid());
            await adapter.ExecuteAsync<object>(
                TenantId, ActorId,
                Intent(EconomyValueMovementCapability.Transfer) with
                {
                    SourceWalletId = wallet,
                    DestinationWalletId = wallet
                },
                (_, _) => Task.FromResult(new object()), CancellationToken.None);
            (await context.Set<FinancialCrimeSignalOutboxRow>().CountAsync()).Should().Be(2);

            // Hook disabled: pure passthrough.
            var disabled = new EconomyFinancialCrimeSignalAdapter(
                fixture.Inner, fixture.ScopeFactory,
                Options.Create(new FinancialCrimeEconomyHookOptions { Enabled = false }), TimeProvider.System);
            await disabled.ExecuteAsync<object>(
                TenantId, ActorId, Intent(EconomyValueMovementCapability.Transfer),
                (_, _) => Task.FromResult(new object()), CancellationToken.None);
            (await context.Set<FinancialCrimeSignalOutboxRow>().CountAsync()).Should().Be(2);
        }

        // Fault containment: queue failures never fail the completed operation...
        var throwingQueue = new StubScopeFactory(new ThrowingQueue());
        var swallowing = new EconomyFinancialCrimeSignalAdapter(
            fixture.Inner, throwingQueue, Options.Create(new FinancialCrimeEconomyHookOptions()), TimeProvider.System);
        await swallowing.ExecuteAsync<object>(
            TenantId, ActorId, Intent(EconomyValueMovementCapability.Transfer),
            (_, _) => Task.FromResult(new object()), CancellationToken.None);

        // ...and cancellation still propagates.
        using var cancelled = new CancellationTokenSource();
        cancelled.Cancel();
        await FluentActions.Awaiting(() => swallowing.ExecuteAsync<object>(
                TenantId, ActorId, Intent(EconomyValueMovementCapability.Transfer),
                (_, _) => Task.FromResult(new object()), cancelled.Token))
            .Should().ThrowAsync<OperationCanceledException>();

        // An OperationCanceledException on a live token is contained like any fault.
        var cancelledAfterStart = new StubScopeFactory(new ThrowingQueue(cancelled: false));
        var contained = new EconomyFinancialCrimeSignalAdapter(
            fixture.Inner, cancelledAfterStart, Options.Create(new FinancialCrimeEconomyHookOptions()),
            TimeProvider.System);
        await contained.ExecuteAsync<object>(
            TenantId, ActorId, Intent(EconomyValueMovementCapability.Transfer),
            (_, _) => Task.FromResult(new object()), CancellationToken.None);
    }

    [Fact]
    public async Task AdapterConsumesTheUnconsumedDecisionAndSwallowsReplayConflicts()
    {
        await using var database = await EconomyPostgreSqlTestDatabase.CreateAsync("fc_adapter_decision");
        await using var context = CreateContext(database.ConnectionString);
        await context.Database.EnsureCreatedAsync();
        var fixture = new WiringFixture(context);
        var services = fixture.BuildAdapterServices();
        var subjectHash = EconomySubjectReference.ForUser(TenantId, ActorId);

        var seeded = await fixture.Control.RecordSignalAsync(new FinancialCrimeTransactionSignal(
            Guid.NewGuid(), TenantId, subjectHash, "seed:fingerprint", "seed-signal", 900_000,
            "seed-evidence", "seed-key", Now.AddMinutes(-10), Now.AddHours(6)), CancellationToken.None);
        var assigned = await fixture.Control.AssignCaseAsync(
            TenantId, seeded.Id, ReviewerId(), seeded.Version, Now, CancellationToken.None);
        var decision = new FinancialCrimeCaseDecision(
            Guid.NewGuid(), seeded.Id, TenantId, subjectHash, 1, FinancialCrimeOutcome.Approved,
            1, "cleared", "decision-evidence", "internal://case/decision", ReviewerId(), Now, Now.AddHours(4));
        await fixture.Control.DecideCaseAsync(decision, assigned.Version, CancellationToken.None);

        using var scope = services.CreateScope();
        var adapter = scope.ServiceProvider.GetRequiredService<IEconomyTrustedProtectedOperationAuthorizer>();
        // First operation consumes the single-use decision...
        await adapter.ExecuteAsync<object>(
            TenantId, ActorId, Intent(EconomyValueMovementCapability.PayoutExecution),
            (_, _) => Task.FromResult(new object()), CancellationToken.None);
        (await context.Set<FinancialCrimeDecisionConsumptionRow>().CountAsync()).Should().Be(1);
        // ...the replay conflicts and is contained; the operation still completes.
        await adapter.ExecuteAsync<object>(
            TenantId, ActorId, Intent(EconomyValueMovementCapability.PayoutExecution),
            (_, _) => Task.FromResult(new object()), CancellationToken.None);
        (await context.Set<FinancialCrimeDecisionConsumptionRow>().CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task QueueRequiresARelationalContextAndOptionDefaultsHold()
    {
        Action nullContext = () => new PostgreSqlFinancialCrimeSignalQueue(null!);
        nullContext.Should().Throw<ArgumentNullException>();
        var queue = new PostgreSqlFinancialCrimeSignalQueue(new NonDbContext());
        await FluentActions.Awaiting(() => queue.EnqueueEconomyOperationAsync(Economy(TenantId, 1)).AsTask())
            .Should().ThrowAsync<InvalidOperationException>();

        new FinancialCrimeEconomyHookOptions().Enabled.Should().BeTrue();
        new FinancialCrimeEconomyHookOptions().AlertUnitsThreshold.Should().Be(1_000_000);
        new FinancialCrimeIngestionWorkerOptions().Enabled.Should().BeTrue();
        new FinancialCrimeRescreeningWorkerOptions().Enabled.Should().BeTrue();

        FinancialCrimeInternalScreeningProviders.IsInternal(
            FinancialCrimeInternalScreeningProviders.EconomyOperation).Should().BeTrue();
        FinancialCrimeInternalScreeningProviders.IsInternal(
            FinancialCrimeInternalScreeningProviders.KycIdentityProfile).Should().BeTrue();
        FinancialCrimeInternalScreeningProviders.IsInternal("sumsub").Should().BeFalse();
    }

    private static Guid ReviewerId() => Guid.Parse("33333333-3333-3333-3333-333333333333");

    private static FinancialCrimeEconomyOperationSignal Economy(Guid tenantId, long units, DateTimeOffset? observedAt = null) => new(
        tenantId, ActorId, EconomySubjectReference.ForUser(tenantId, ActorId),
        EconomyValueMovementCapability.Transfer.ToString(), units, CurrencyCode.HardCoin.ToString(),
        Guid.NewGuid(), "provider-hash", $"fingerprint-{units}", $"key-{units}-{observedAt?.UtcTicks}", observedAt ?? Now);

    private static FinancialCrimeKycDecisionSignal Kyc(string subjectHash, string state, string? answer, string eventId) => new(
        TenantId, subjectHash, "applicant-1", state, answer, eventId, "payload-hash", Now);

    private static FinancialCrimeScreening Internal(string providerEventId, long policyVersion, DateTimeOffset nextScreenAt) => new(
        Guid.NewGuid(), FinancialCrimeInternalScreeningProviders.EconomyOperation, "production", providerEventId,
        TenantId, EconomySubjectReference.ForUser(TenantId, ActorId), 1, FinancialCrimeOutcome.Approved,
        false, false, false, policyVersion, $"payload-{providerEventId}", $"evidence-{providerEventId}",
        $"internal://screening/{providerEventId}", true, Now.AddHours(-2), Now.AddHours(-1), nextScreenAt,
        Now.AddHours(-2).AddMinutes(1));

    private static FinancialCrimeScreening External(string provider, string providerEventId, DateTimeOffset nextScreenAt) => new(
        Guid.NewGuid(), provider, "sandbox", providerEventId,
        TenantId, "external-subject", 1, FinancialCrimeOutcome.Approved,
        false, false, false, 7, $"payload-{providerEventId}", $"evidence-{providerEventId}",
        $"s3://compliance/{providerEventId}", true, Now.AddHours(-2), Now.AddHours(-1), nextScreenAt,
        Now.AddHours(-2).AddMinutes(1));

    private static EconomyProtectedOperationIntent Intent(EconomyValueMovementCapability capability) => new(
        capability,
        PostingTemplateKind.Spend,
        new WalletId(Guid.NewGuid()),
        new WalletId(Guid.NewGuid()),
        new CoinAmount(CurrencyCode.HardCoin, 750),
        [new RiskCurrencyLeg(CurrencyCode.HardCoin, 750)],
        [new SourceStampId(Guid.NewGuid())],
        "provider-hash",
        "destination-hash",
        new IdempotencyKey($"intent-{Guid.NewGuid():N}"),
        Now);

    private static TestDbContext CreateContext(string connectionString) => new(
        new DbContextOptionsBuilder<TestDbContext>().UseNpgsql(connectionString).Options);

    private sealed class WiringFixture
    {
        public WiringFixture(TestDbContext context)
        {
            Context = context;
            var evidence = new PostgreSqlComplianceEvidenceStore(context);
            Control = new PostgreSqlFinancialCrimeControlPlane(
                context, evidence, evidence, new PostgreSqlComplianceHoldStore(context));
            Queue = new PostgreSqlFinancialCrimeSignalQueue(context);
            Store = Queue;
            var hook = Options.Create(new FinancialCrimeEconomyHookOptions());
            IngestionWorker = new FinancialCrimeSignalIngestionWorker(
                Queue, Control, context, hook,
                Options.Create(new FinancialCrimeIngestionWorkerOptions()), TimeProvider.System);
            Inner = new EconomyTrustedProtectedOperationAuthorizer(
                new StubJurisdictionResolver(), new AllowAllRiskIssuer(),
                new StubCapabilityAuthorization(), new PassthroughTransaction());
            var hostServices = new ServiceCollection();
            hostServices.AddScoped<TestDbContext>(_ => context);
            hostServices.AddScoped<IApplicationDbContext>(provider => provider.GetRequiredService<TestDbContext>());
            hostServices.AddScoped<PostgreSqlComplianceEvidenceStore>(_ => new PostgreSqlComplianceEvidenceStore(context));
            hostServices.AddScoped<IComplianceEvidenceStore>(provider => provider.GetRequiredService<PostgreSqlComplianceEvidenceStore>());
            hostServices.AddScoped<IComplianceEvidenceReader>(provider => provider.GetRequiredService<PostgreSqlComplianceEvidenceStore>());
            hostServices.AddScoped<IComplianceHoldStore>(_ => new PostgreSqlComplianceHoldStore(context));
            hostServices.AddFinancialCrimeComposition();
            Provider = hostServices.BuildServiceProvider();
        }

        public TestDbContext Context { get; }
        public IFinancialCrimeControlPlane Control { get; }
        public PostgreSqlFinancialCrimeSignalQueue Queue { get; }
        public IFinancialCrimeSignalOutboxStore Store { get; }
        public FinancialCrimeSignalIngestionWorker IngestionWorker { get; }
        public EconomyTrustedProtectedOperationAuthorizer Inner { get; }
        public ServiceProvider Provider { get; }

        public IServiceScopeFactory ScopeFactory => Provider.GetRequiredService<IServiceScopeFactory>();

        public ServiceProvider BuildAdapterServices()
        {
            var services = new ServiceCollection();
            services.AddScoped<TestDbContext>(_ => Context);
            services.AddScoped<IApplicationDbContext>(provider => provider.GetRequiredService<TestDbContext>());
            services.AddScoped<PostgreSqlComplianceEvidenceStore>(_ => new PostgreSqlComplianceEvidenceStore(Context));
            services.AddScoped<IComplianceEvidenceStore>(provider => provider.GetRequiredService<PostgreSqlComplianceEvidenceStore>());
            services.AddScoped<IComplianceEvidenceReader>(provider => provider.GetRequiredService<PostgreSqlComplianceEvidenceStore>());
            services.AddScoped<IComplianceHoldStore>(_ => new PostgreSqlComplianceHoldStore(Context));
            services.AddFinancialCrimeComposition();
            services.AddScoped<IEconomyJurisdictionResolver, StubJurisdictionResolver>();
            services.AddScoped<IEconomyProtectedOperationRiskDecisionIssuer, AllowAllRiskIssuer>();
            services.AddScoped<IEconomyCapabilityAuthorizationService, StubCapabilityAuthorization>();
            services.AddScoped<IEconomyProtectedOperationTransaction, PassthroughTransaction>();
            services.AddScoped<EconomyTrustedProtectedOperationAuthorizer>();
            services.AddScoped<IEconomyTrustedProtectedOperationAuthorizer, EconomyFinancialCrimeSignalAdapter>();
            return services.BuildServiceProvider();
        }
    }

    private sealed class StubJurisdictionResolver : IEconomyJurisdictionResolver
    {
        public ValueTask<EconomyJurisdictionResolution> ResolveAsync(
            Guid tenantId, Guid actorId, string? providerJurisdiction, string? destinationJurisdiction,
            DateTimeOffset evaluatedAt, CancellationToken cancellationToken) =>
            ValueTask.FromResult(new EconomyJurisdictionResolution("BRA", 1, 1, "jurisdiction-evidence"));
    }

    private sealed class AllowAllRiskIssuer : IEconomyProtectedOperationRiskDecisionIssuer
    {
        public ValueTask<EconomyProtectedRiskDecision> IssueAsync(
            EconomyProtectedRiskDecisionRequest request, CancellationToken cancellationToken) =>
            ValueTask.FromResult(new EconomyProtectedRiskDecision(
                Guid.NewGuid(), RiskOutcome.Allow, EconomyProtectedOperationState.Ready, null, []));
    }

    private sealed class StubCapabilityAuthorization : IEconomyCapabilityAuthorizationService
    {
        public ValueTask<CapabilityAuthorizationReceipt> AuthorizeAndConsumeAsync(
            EconomyCapabilityEvaluationContext context, CancellationToken cancellationToken) =>
            ValueTask.FromResult(new CapabilityAuthorizationReceipt(
                Guid.NewGuid(), context.TenantId, context.ActorId, context.SubjectReference,
                context.JurisdictionCode, context.Capability, context.OperationFingerprint,
                1, 1, context.RiskDecisionId, 0, context.ProviderHash, context.DestinationHash,
                context.SourceRootHashes, [], Now, Now.AddHours(1), "receipt-hash", "key-1", "signature"));
    }

    private sealed class PassthroughTransaction : IEconomyProtectedOperationTransaction
    {
        public Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken cancellationToken) =>
            operation(cancellationToken);
    }

    private sealed class ThrowingQueue(bool cancelled = true) : IFinancialCrimeSignalQueue
    {
        public ValueTask EnqueueEconomyOperationAsync(
            FinancialCrimeEconomyOperationSignal signal, CancellationToken cancellationToken = default) =>
            ValueTask.FromException(cancelled
                ? new OperationCanceledException(cancellationToken)
                : new OperationCanceledException());

        public ValueTask EnqueueKycDecisionAsync(
            FinancialCrimeKycDecisionSignal signal, CancellationToken cancellationToken = default) =>
            ValueTask.FromException(new OperationCanceledException(cancellationToken));
    }

    private sealed class StubScopeFactory(IFinancialCrimeSignalQueue queue) : IServiceScopeFactory
    {
        public IServiceScope CreateScope() => new StubScope(queue);

        private sealed class StubScope(IFinancialCrimeSignalQueue queue) : IServiceScope
        {
            private readonly StubProvider _provider = new(queue);

            public IServiceProvider ServiceProvider => _provider;

            public void Dispose() { }

            private sealed class StubProvider(IFinancialCrimeSignalQueue queue) : IServiceProvider
            {
                public object? GetService(Type serviceType) =>
                    serviceType == typeof(IFinancialCrimeSignalQueue) ? queue : null;
            }
        }
    }

    /// <summary>
    /// Stub plane for rescreening fault paths: serves a fixed due list and throws the
    /// conflict the race-recovery branch expects on ingest.
    /// </summary>
    private sealed class StubControlPlane(FinancialCrimeScreening? due = null) : IFinancialCrimeControlPlane
    {
        public ValueTask<FinancialCrimeScreeningResult> IngestScreeningAsync(
            FinancialCrimeScreening screening, CancellationToken cancellationToken) =>
            throw new FinancialCrimeConflictException("The screening version was taken concurrently.");

        public ValueTask<FinancialCrimeCase> RecordSignalAsync(
            FinancialCrimeTransactionSignal signal, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public ValueTask<FinancialCrimeCase> AssignCaseAsync(
            Guid tenantId, Guid caseId, Guid actorId, long expectedVersion, DateTimeOffset assignedAt,
            CancellationToken cancellationToken) => throw new NotSupportedException();

        public ValueTask<FinancialCrimeCaseDecision> DecideCaseAsync(
            FinancialCrimeCaseDecision decision, long expectedCaseVersion, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public ValueTask ConsumeDecisionAsync(
            Guid tenantId, Guid decisionId, string operationFingerprint, DateTimeOffset consumedAt,
            CancellationToken cancellationToken) => throw new NotSupportedException();

        public ValueTask RecordRegulatoryReferenceAsync(
            Guid tenantId, Guid caseId, string kind, string jurisdictionCode, string referenceHash,
            Guid actorId, DateTimeOffset recordedAt, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public ValueTask<IReadOnlyList<FinancialCrimeScreening>> ReadDueRescreeningsAsync(
            DateTimeOffset now, int limit, CancellationToken cancellationToken) =>
            ValueTask.FromResult<IReadOnlyList<FinancialCrimeScreening>>(
                due is null ? Array.Empty<FinancialCrimeScreening>() : new[] { due });

        public ValueTask<IReadOnlyList<FinancialCrimeCase>> ReadCasesAsync(
            Guid tenantId, FinancialCrimeCaseState? state, int limit, CancellationToken cancellationToken) =>
            ValueTask.FromResult<IReadOnlyList<FinancialCrimeCase>>(Array.Empty<FinancialCrimeCase>());

        public ValueTask<FinancialCrimeCaseDetails> ReadCaseDetailsAsync(
            Guid tenantId, Guid caseId, CancellationToken cancellationToken) => throw new NotSupportedException();

        public ValueTask<FinancialCrimeCaseDecision?> ReadLatestApprovedDecisionAsync(
            Guid tenantId, string subjectHash, DateTimeOffset now, CancellationToken cancellationToken) =>
            ValueTask.FromResult<FinancialCrimeCaseDecision?>(null);
    }

    private sealed class NonDbContext : IApplicationDbContext
    {
        public DbSet<T> Set<T>() where T : class => throw new NotSupportedException();
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }

    private sealed class TestDbContext(DbContextOptions<TestDbContext> options)
        : DbContext(options), IApplicationDbContext
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            new EconomyModelConfiguration().Configure(modelBuilder);
            new FinancialCrimeModelConfiguration().Configure(modelBuilder);
        }

        public Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default) =>
            Database.BeginTransactionAsync(cancellationToken);
    }
}
