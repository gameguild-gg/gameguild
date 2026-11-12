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
/// Issue #1291 option C wiring: the economy hook (and KYC enrichment) feed the control plane
/// through the module's own outbox, the worker turns signals into screenings/cases, and the
/// decision flows back to the risk gate the Economy money path reads.
/// </summary>
public sealed class FinancialCrimeIngestionWiringTests
{
    private static readonly Guid TenantId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid ActorId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid ReviewerId = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private static readonly Guid SourceWallet = Guid.Parse("44444444-4444-4444-4444-444444444444");
    private static readonly Guid DestinationWallet = Guid.Parse("55555555-5555-5555-5555-555555555555");

    [Fact]
    public async Task AlertEconomySignalOpensCaseAndDecisionFlowsBackToTheRiskGate()
    {
        await using var database = await EconomyPostgreSqlTestDatabase.CreateAsync("fc_ingestion_e2e");
        await using var context = CreateContext(database.ConnectionString);
        await context.Database.EnsureCreatedAsync();
        var fixture = new Fixture(context);
        var subjectHash = EconomySubjectReference.ForUser(TenantId, ActorId);
        var observedAt = DateTimeOffset.UtcNow.AddSeconds(-5);

        // Money path: the adapter enqueues the alert-level operation (fire-and-forget).
        await fixture.Queue.EnqueueEconomyOperationAsync(EconomySignal("alert-operation", 1_500_000, observedAt));

        // Outbox worker: the signal is ingested asynchronously — an AML case opens and the
        // subject is held; the signal itself publishes no screening evidence (alerts are
        // analyst work, not routine screening).
        (await fixture.IngestionWorker.ProcessDueAsync()).Should().Be(1);
        var cases = await fixture.Control.ReadCasesAsync(TenantId, null, 10, CancellationToken.None);
        cases.Should().ContainSingle().Which.State.Should().Be(FinancialCrimeCaseState.Open);
        var opened = cases[0];
        opened.SubjectHash.Should().Be(subjectHash);
        (await context.Set<FinancialCrimeTransactionSignalRow>().CountAsync()).Should().Be(1);
        (await new PostgreSqlComplianceHoldStore(context).IsActiveAsync(
            new ComplianceHoldScope(TenantId, subjectHash, null), DateTimeOffset.UtcNow, CancellationToken.None))
            .Should().BeTrue();

        // Gate before a decision: an alert-level subject has no screening evidence yet, so the
        // risk input stays Unavailable — the protected operation risk issuer keeps denying
        // fail-closed (ComplianceUnavailable) until the case is decided or a routine screening
        // synthesizes evidence for the subject.
        (await fixture.RiskInput.ReadAsync(TenantId, subjectHash, DateTimeOffset.UtcNow)).Outcome
            .Should().Be(ExternalRiskOutcome.Unavailable);

        // Analyst decides the case Approved: the hold releases transactionally and the
        // decision publishes Approved evidence for the same subject.
        var assigned = await fixture.Control.AssignCaseAsync(
            TenantId, opened.Id, ReviewerId, opened.Version, DateTimeOffset.UtcNow, CancellationToken.None);
        var decision = new FinancialCrimeCaseDecision(
            Guid.NewGuid(), opened.Id, TenantId, subjectHash, 1, FinancialCrimeOutcome.Approved,
            1, "cleared", "decision-evidence", "internal://case/decision", ReviewerId,
            DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddHours(4));
        await fixture.Control.DecideCaseAsync(decision, assigned.Version, CancellationToken.None);
        (await new PostgreSqlComplianceHoldStore(context).IsActiveAsync(
            new ComplianceHoldScope(TenantId, subjectHash, null), DateTimeOffset.UtcNow, CancellationToken.None))
            .Should().BeFalse();

        // Gate after the decision: the same evidence read now feeds Allow — this is the exact
        // input the protected operation risk issuer consumes for the money path.
        (await fixture.RiskInput.ReadAsync(TenantId, subjectHash, DateTimeOffset.UtcNow.AddMinutes(1))).Outcome
            .Should().Be(ExternalRiskOutcome.Allow);

        // Decision consumption: the read exposes the unconsumed Approved decision, and the
        // single-use consumption binds it to the operation fingerprint exactly once.
        var readable = await fixture.Control.ReadLatestApprovedDecisionAsync(
            TenantId, subjectHash, DateTimeOffset.UtcNow.AddMinutes(1), CancellationToken.None);
        readable!.Id.Should().Be(decision.Id);
        await FluentActions.Awaiting(() => fixture.Control.ReadLatestApprovedDecisionAsync(
                Guid.Empty, subjectHash, DateTimeOffset.UtcNow, CancellationToken.None).AsTask())
            .Should().ThrowAsync<ArgumentException>();
        await FluentActions.Awaiting(() => fixture.Control.ReadLatestApprovedDecisionAsync(
                TenantId, " ", DateTimeOffset.UtcNow, CancellationToken.None).AsTask())
            .Should().ThrowAsync<ArgumentException>();
        await fixture.Control.ConsumeDecisionAsync(
            TenantId, decision.Id, "protected-operation:1", DateTimeOffset.UtcNow.AddMinutes(1), CancellationToken.None);
        (await fixture.Control.ReadLatestApprovedDecisionAsync(
            TenantId, subjectHash, DateTimeOffset.UtcNow.AddMinutes(1), CancellationToken.None)).Should().BeNull();
    }

    [Fact]
    public async Task RoutineEconomyOperationSynthesizesApprovedScreeningWithoutOpeningACase()
    {
        await using var database = await EconomyPostgreSqlTestDatabase.CreateAsync("fc_ingestion_routine");
        await using var context = CreateContext(database.ConnectionString);
        await context.Database.EnsureCreatedAsync();
        var fixture = new Fixture(context);
        var subjectHash = EconomySubjectReference.ForUser(TenantId, ActorId);

        await fixture.Queue.EnqueueEconomyOperationAsync(
            EconomySignal("routine-operation", 500, DateTimeOffset.UtcNow.AddSeconds(-5)));
        (await fixture.IngestionWorker.ProcessDueAsync()).Should().Be(1);

        (await context.Set<FinancialCrimeScreeningRow>().CountAsync()).Should().Be(1);
        var screening = await context.Set<FinancialCrimeScreeningRow>().SingleAsync();
        screening.Provider.Should().Be(FinancialCrimeInternalScreeningProviders.EconomyOperation);
        screening.Outcome.Should().Be(FinancialCrimeOutcome.Approved);
        screening.SubjectHash.Should().Be(subjectHash);
        (await context.Set<FinancialCrimeCaseRow>().CountAsync()).Should().Be(0);
        (await fixture.RiskInput.ReadAsync(TenantId, subjectHash, DateTimeOffset.UtcNow)).Outcome
            .Should().Be(ExternalRiskOutcome.Allow);
    }

    [Fact]
    public async Task ReviewBandEconomyOperationScreensNeedsReview()
    {
        await using var database = await EconomyPostgreSqlTestDatabase.CreateAsync("fc_ingestion_review_band");
        await using var context = CreateContext(database.ConnectionString);
        await context.Database.EnsureCreatedAsync();
        var fixture = new Fixture(context);
        var subjectHash = EconomySubjectReference.ForUser(TenantId, ActorId);

        await fixture.Queue.EnqueueEconomyOperationAsync(
            EconomySignal("review-operation", 250_000, DateTimeOffset.UtcNow.AddSeconds(-5)));
        (await fixture.IngestionWorker.ProcessDueAsync()).Should().Be(1);

        var screening = await context.Set<FinancialCrimeScreeningRow>().SingleAsync();
        screening.Outcome.Should().Be(FinancialCrimeOutcome.NeedsReview);
        // The NeedsReview screening opens the case through the same case-opening path.
        (await context.Set<FinancialCrimeCaseRow>().CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task KycDecisionsEnrichTheAmlProfileOnBothTiers()
    {
        await using var database = await EconomyPostgreSqlTestDatabase.CreateAsync("fc_ingestion_kyc");
        await using var context = CreateContext(database.ConnectionString);
        await context.Database.EnsureCreatedAsync();
        var fixture = new Fixture(context);
        var cleared = EconomySubjectReference.ForUser(TenantId, ActorId);
        var flagged = EconomySubjectReference.ForUser(TenantId, ReviewerId);

        // GREEN: identity context enriches the profile as an Approved screening; no case.
        await fixture.Queue.EnqueueKycDecisionAsync(KycSignal(cleared, "Approved", "GREEN", "kyc-event-green"));
        // RED: the adverse decision records an identity-risk signal — case + hold.
        await fixture.Queue.EnqueueKycDecisionAsync(KycSignal(flagged, "Rejected", "RED", "kyc-event-red"));
        (await fixture.IngestionWorker.ProcessDueAsync()).Should().Be(2);

        var screening = await context.Set<FinancialCrimeScreeningRow>().SingleAsync();
        screening.SubjectHash.Should().Be(cleared);
        screening.Provider.Should().Be(FinancialCrimeInternalScreeningProviders.KycIdentityProfile);
        screening.Outcome.Should().Be(FinancialCrimeOutcome.Approved);
        (await fixture.RiskInput.ReadAsync(TenantId, cleared, DateTimeOffset.UtcNow)).Outcome
            .Should().Be(ExternalRiskOutcome.Allow);

        var flaggedCases = await fixture.Control.ReadCasesAsync(TenantId, null, 10, CancellationToken.None);
        flaggedCases.Should().ContainSingle().Which.SubjectHash.Should().Be(flagged);
        (await new PostgreSqlComplianceHoldStore(context).IsActiveAsync(
            new ComplianceHoldScope(TenantId, flagged, null), DateTimeOffset.UtcNow, CancellationToken.None))
            .Should().BeTrue();
    }

    [Fact]
    public async Task RescreeningSweepRefreshesDueInternalScreenings()
    {
        await using var database = await EconomyPostgreSqlTestDatabase.CreateAsync("fc_ingestion_rescreen");
        await using var context = CreateContext(database.ConnectionString);
        await context.Database.EnsureCreatedAsync();
        var fixture = new Fixture(context, screeningInterval: TimeSpan.FromMilliseconds(2));
        var subjectHash = EconomySubjectReference.ForUser(TenantId, ActorId);

        await fixture.Queue.EnqueueEconomyOperationAsync(
            EconomySignal("rescreen-seed", 500, DateTimeOffset.UtcNow.AddSeconds(-5)));
        await fixture.IngestionWorker.ProcessDueAsync();
        (await context.Set<FinancialCrimeScreeningRow>().CountAsync()).Should().Be(1);

        // The 1-tick screening interval is already due: the sweep re-synthesizes the subject's
        // screening at the next contiguous version through the control plane.
        (await fixture.RescreeningWorker.RunAsync()).Should().Be(1);
        var screenings = await context.Set<FinancialCrimeScreeningRow>()
            .OrderBy(row => row.Version).ToListAsync();
        screenings.Should().HaveCount(2);
        screenings[1].Version.Should().Be(2);
        screenings[1].ProviderEventId.Should().Be($"rescreen:{subjectHash}:2");
        screenings[1].Outcome.Should().Be(screenings[0].Outcome);
    }

    [Fact]
    public async Task EconomyAdapterEnqueuesSignalsAndConsumesDecisionsWithoutBlockingTheOperation()
    {
        await using var database = await EconomyPostgreSqlTestDatabase.CreateAsync("fc_ingestion_adapter");
        await using var context = CreateContext(database.ConnectionString);
        await context.Database.EnsureCreatedAsync();
        var fixture = new Fixture(context);
        var services = fixture.BuildAdapterServices();
        var subjectHash = EconomySubjectReference.ForUser(TenantId, ActorId);

        // Pre-existing Approved case decision for the subject (seeded through the plane).
        var seeded = await fixture.Control.RecordSignalAsync(new FinancialCrimeTransactionSignal(
            Guid.NewGuid(), TenantId, subjectHash, "seed:fingerprint", "seed-signal", 900_000,
            "seed-evidence", "seed-key", DateTimeOffset.UtcNow.AddMinutes(-10),
            DateTimeOffset.UtcNow.AddHours(6)), CancellationToken.None);
        var assigned = await fixture.Control.AssignCaseAsync(
            TenantId, seeded.Id, ReviewerId, seeded.Version, DateTimeOffset.UtcNow, CancellationToken.None);
        var decision = new FinancialCrimeCaseDecision(
            Guid.NewGuid(), seeded.Id, TenantId, subjectHash, 1, FinancialCrimeOutcome.Approved,
            1, "cleared", "decision-evidence", "internal://case/decision", ReviewerId,
            DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddHours(4));
        await fixture.Control.DecideCaseAsync(decision, assigned.Version, CancellationToken.None);

        using var scope = services.CreateScope();
        var adapter = scope.ServiceProvider.GetRequiredService<IEconomyTrustedProtectedOperationAuthorizer>();
        var completed = await adapter.ExecuteAsync<object>(
            TenantId, ActorId, Intent(EconomyValueMovementCapability.Transfer, 250_000),
            (_, _) => Task.FromResult<object>(new { moved = true }), CancellationToken.None);
        completed.Should().NotBeNull();

        // The hook left exactly one durable economy signal on the outbox...
        (await context.Set<FinancialCrimeSignalOutboxRow>().CountAsync()).Should().Be(1);
        var outboxRow = await context.Set<FinancialCrimeSignalOutboxRow>().SingleAsync();
        outboxRow.Kind.Should().Be(FinancialCrimeSignalKind.EconomyOperation);

        // ...and consumed the Approved decision bound to this operation's fingerprint.
        var consumption = await context.Set<FinancialCrimeDecisionConsumptionRow>().SingleAsync();
        consumption.DecisionId.Should().Be(decision.Id);
        consumption.OperationFingerprint.Should().NotBeNullOrWhiteSpace();
        (await fixture.Control.ReadLatestApprovedDecisionAsync(
            TenantId, subjectHash, DateTimeOffset.UtcNow, CancellationToken.None)).Should().BeNull();

        // The gate verdict is unchanged by the hook: the operation itself completed.
        var replay = await adapter.ExecuteAsync<object>(
            TenantId, ActorId, Intent(EconomyValueMovementCapability.Transfer, 250_000),
            (_, _) => Task.FromResult<object>(new { moved = true }), CancellationToken.None);
        replay.Should().NotBeNull();
    }

    private static FinancialCrimeEconomyOperationSignal EconomySignal(
        string idempotencyKey, long units, DateTimeOffset observedAt) => new(
        TenantId, ActorId, EconomySubjectReference.ForUser(TenantId, ActorId),
        EconomyValueMovementCapability.Transfer.ToString(), units, CurrencyCode.HardCoin.ToString(),
        DestinationWallet, "provider-hash", $"fingerprint:{idempotencyKey}", idempotencyKey, observedAt);

    private static FinancialCrimeKycDecisionSignal KycSignal(
        string subjectHash, string state, string answer, string eventId) => new(
        TenantId, subjectHash, "applicant-1", state, answer, eventId, "payload-hash", DateTimeOffset.UtcNow.AddSeconds(-5));

    private static EconomyProtectedOperationIntent Intent(
        EconomyValueMovementCapability capability, long units) => new(
        capability,
        PostingTemplateKind.Spend,
        new WalletId(SourceWallet),
        new WalletId(DestinationWallet),
        new CoinAmount(CurrencyCode.HardCoin, units),
        [new RiskCurrencyLeg(CurrencyCode.HardCoin, units)],
        [new SourceStampId(Guid.NewGuid())],
        "provider-hash",
        "destination-hash",
        new IdempotencyKey($"intent-{Guid.NewGuid():N}"),
        DateTimeOffset.UtcNow);

    private static TestDbContext CreateContext(string connectionString) => new(
        new DbContextOptionsBuilder<TestDbContext>().UseNpgsql(connectionString).Options);

    private sealed record FixtureOptions(TimeSpan ScreeningInterval);

    private sealed class Fixture
    {
        public Fixture(TestDbContext context, TimeSpan? screeningInterval = null)
        {
            Context = context;
            var evidence = new PostgreSqlComplianceEvidenceStore(context);
            Control = new PostgreSqlFinancialCrimeControlPlane(
                context, evidence, evidence, new PostgreSqlComplianceHoldStore(context));
            RiskInput = new PostgreSqlFinancialCrimeRiskInputSource(evidence);
            Queue = new PostgreSqlFinancialCrimeSignalQueue(context);
            var hook = Options.Create(new FinancialCrimeEconomyHookOptions
            {
                ScreeningInterval = screeningInterval ?? TimeSpan.FromHours(24)
            });
            var worker = Options.Create(new FinancialCrimeIngestionWorkerOptions());
            IngestionWorker = new FinancialCrimeSignalIngestionWorker(
                (IFinancialCrimeSignalOutboxStore)Queue, Control, context, hook, worker, TimeProvider.System);
            RescreeningWorker = new FinancialCrimeRescreeningWorker(
                Control, context, hook,
                Options.Create(new FinancialCrimeRescreeningWorkerOptions()), TimeProvider.System);
        }

        public TestDbContext Context { get; }
        public IFinancialCrimeControlPlane Control { get; }
        public PostgreSqlFinancialCrimeRiskInputSource RiskInput { get; }
        public IFinancialCrimeSignalQueue Queue { get; }
        public IFinancialCrimeSignalIngestionWorker IngestionWorker { get; }
        public IFinancialCrimeRescreeningWorker RescreeningWorker { get; }

        /// <summary>
        /// Builds the adapter slice the host composition wires: the real Economy trusted
        /// authorizer with stubbed authority inputs, decorated by
        /// <see cref="EconomyFinancialCrimeSignalAdapter"/> over the FinancialCrime services.
        /// The shared context registers as a singleton INSTANCE: the adapter disposes its
        /// per-operation scopes, and a factory-registered scoped context would be disposed
        /// with them.
        /// </summary>
        public ServiceProvider BuildAdapterServices()
        {
            var services = new ServiceCollection();
            services.AddSingleton(Context);
            services.AddSingleton<IApplicationDbContext>(Context);
            services.AddSingleton(new PostgreSqlComplianceEvidenceStore(Context));
            services.AddSingleton<IComplianceEvidenceStore>(provider =>
                provider.GetRequiredService<PostgreSqlComplianceEvidenceStore>());
            services.AddSingleton<IComplianceEvidenceReader>(provider =>
                provider.GetRequiredService<PostgreSqlComplianceEvidenceStore>());
            services.AddSingleton<IComplianceHoldStore>(new PostgreSqlComplianceHoldStore(Context));
            services.AddFinancialCrimeComposition();
            services.AddOptions<FinancialCrimeEconomyHookOptions>()
                .Configure(options => options.ScreeningInterval = TimeSpan.FromHours(24));
            services.AddOptions<FinancialCrimeIngestionWorkerOptions>();
            services.AddOptions<FinancialCrimeRescreeningWorkerOptions>();
            services.AddSingleton<Microsoft.Extensions.Logging.ILogger<EconomyFinancialCrimeSignalAdapter>>(
                Microsoft.Extensions.Logging.Abstractions.NullLogger<EconomyFinancialCrimeSignalAdapter>.Instance);
            // The Economy authority inputs are stubbed: the adapter test exercises the hook
            // seam, not the risk engine (covered by the Economy module's own suites).
            services.AddScoped<IEconomyJurisdictionResolver, StubJurisdictionResolver>();
            services.AddScoped<IEconomyProtectedOperationRiskDecisionIssuer, StubRiskDecisionIssuer>();
            services.AddScoped<IEconomyCapabilityAuthorizationService, StubCapabilityAuthorization>();
            services.AddScoped<IEconomyProtectedOperationTransaction, StubOperationTransaction>();
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

    private sealed class StubRiskDecisionIssuer : IEconomyProtectedOperationRiskDecisionIssuer
    {
        public ValueTask<EconomyProtectedRiskDecision> IssueAsync(
            EconomyProtectedRiskDecisionRequest request,
            CancellationToken cancellationToken) =>
            ValueTask.FromResult(new EconomyProtectedRiskDecision(
                Guid.NewGuid(), RiskOutcome.Allow, EconomyProtectedOperationState.Ready, null, []));
    }

    private sealed class StubCapabilityAuthorization : IEconomyCapabilityAuthorizationService
    {
        public ValueTask<CapabilityAuthorizationReceipt> AuthorizeAndConsumeAsync(
            EconomyCapabilityEvaluationContext context,
            CancellationToken cancellationToken) =>
            ValueTask.FromResult(new CapabilityAuthorizationReceipt(
                Guid.NewGuid(), context.TenantId, context.ActorId, context.SubjectReference,
                context.JurisdictionCode, context.Capability, context.OperationFingerprint,
                1, 1, context.RiskDecisionId, 0, context.ProviderHash, context.DestinationHash,
                context.SourceRootHashes, [], DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddHours(1),
                "receipt-hash", "key-1", "signature"));
    }

    private sealed class StubOperationTransaction : IEconomyProtectedOperationTransaction
    {
        public Task<T> ExecuteAsync<T>(
            Func<CancellationToken, Task<T>> operation,
            CancellationToken cancellationToken) => operation(cancellationToken);
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
