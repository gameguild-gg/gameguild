using FluentAssertions;
using GameGuild.Finance.Economy.Contracts;
using GameGuild.Finance.Economy.Persistence;
using GameGuild.Finance.Economy.Risk;
using GameGuild.TestSupport.Finance.Economy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace GameGuild.TrustSafety.UnitTests;

/// <summary>
/// Coverage completion for the issue #1319 T1 ingestion surface: every branch of
/// <see cref="EconomyRiskDecisionTrustSafetyIngestion"/> (outcome mapping, steady-state
/// suppression, version stamping, signing, ingestion statuses and fault containment) and
/// every registration shape handled by
/// <see cref="EconomyRiskTrustSafetyIngestionComposition"/> (the economy CI lane enforces
/// full line and branch coverage of this assembly).
/// </summary>
public sealed class EconomyRiskIngestionCoverageTests
{
    private static readonly Guid TenantId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid ActorId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    [Fact]
    public async Task IngestionMapsEveryVerdictSuppressesSteadyStateAndContainsFaults()
    {
        await using var database = await EconomyPostgreSqlTestDatabase.CreateAsync("ts_risk_ingestion");
        await using var context = CreateContext(database.ConnectionString);
        await context.Database.EnsureCreatedAsync();
        var plane = new RecordingControlPlane(ComplianceEvidenceIngestionStatus.Published);
        var signer = new ScriptedSigner();
        var unavailableSigner = new ScriptedSigner(new EconomyCryptographyUnavailableException("no KMS key"));
        var logger = NullLogger<EconomyRiskDecisionTrustSafetyIngestion>.Instance;

        // Ready -> Approved, no prior state: version 1 event forwarded with the signature stamped.
        var ready = await IssueAsync(context, plane, signer, EconomyProtectedOperationState.Ready, "subject-ready");
        ready.State.Should().Be(EconomyProtectedOperationState.Ready);
        var forwarded = plane.Events.Should().ContainSingle().Which;
        forwarded.Outcome.Should().Be(TrustSafetyOutcome.Approved);
        forwarded.Version.Should().Be(1);
        forwarded.EventId.Should().Be($"economy-risk:{ready.Id:N}");
        forwarded.KeyId.Should().Be("test-key");
        forwarded.Signature.Should().Be("test-signature");
        forwarded.Kind.Should().Be(TrustSafetyEventKind.AccountRestriction);
        forwarded.PolicyVersion.Should().Be(EconomyRiskDecisionTrustSafetyIngestion.EconomyRiskPolicyVersion);

        // Every remaining subject-verdict mapping arm: review-family states -> NeedsReview, Denied -> Rejected.
        await IssueAsync(context, plane, signer, EconomyProtectedOperationState.ReviewRequired, "subject-review");
        await IssueAsync(context, plane, signer, EconomyProtectedOperationState.Hold, "subject-hold");
        await IssueAsync(context, plane, signer, EconomyProtectedOperationState.Challenge, "subject-challenge");
        await IssueAsync(context, plane, signer, EconomyProtectedOperationState.Denied, "subject-denied");
        plane.Events.Select(e => e.Outcome).Should().Equal(
            TrustSafetyOutcome.Approved,
            TrustSafetyOutcome.NeedsReview,
            TrustSafetyOutcome.NeedsReview,
            TrustSafetyOutcome.NeedsReview,
            TrustSafetyOutcome.Rejected);

        // Non-verdict pipeline states describe the pipeline, not the subject: nothing is forwarded.
        await IssueAsync(context, plane, signer, EconomyProtectedOperationState.ComplianceUnavailable, "subject-pipeline");
        await IssueAsync(context, plane, signer, EconomyProtectedOperationState.ComplianceStale, "subject-pipeline");
        await IssueAsync(context, plane, signer, EconomyProtectedOperationState.InvalidPolicy, "subject-pipeline");
        await IssueAsync(context, plane, signer, EconomyProtectedOperationState.ReserveInsufficient, "subject-pipeline");
        // A blank subject reference cannot be attributed: nothing is forwarded.
        await IssueAsync(context, plane, signer, EconomyProtectedOperationState.Ready, " ");
        plane.Events.Should().HaveCount(5);

        // A differing recorded outcome re-forwards at the recorded version + 1...
        context.Set<TrustSafetySubjectStateRow>().Add(new TrustSafetySubjectStateRow
        {
            TenantId = TenantId,
            SubjectHash = "subject-versioned",
            Version = 4,
            Outcome = TrustSafetyOutcome.Rejected
        });
        await context.SaveChangesAsync();
        await IssueAsync(context, plane, signer, EconomyProtectedOperationState.Ready, "subject-versioned");
        plane.Events.Last().Version.Should().Be(5);

        // ...while an identical recorded outcome is steady state: no event, no signer call.
        var signerCallsBeforeSteadyState = signer.Calls;
        await IssueAsync(context, plane, signer, EconomyProtectedOperationState.Denied, "subject-versioned");
        plane.Events.Should().HaveCount(6);
        signer.Calls.Should().Be(signerCallsBeforeSteadyState);

        // A non-Published/Duplicate ingestion status only logs.
        var deferredPlane = new RecordingControlPlane(ComplianceEvidenceIngestionStatus.Deferred);
        await IssueAsync(context, deferredPlane, signer, EconomyProtectedOperationState.Ready, "subject-deferred");
        deferredPlane.Events.Should().ContainSingle();

        // Fault containment: an unavailable signer (the default without KMS) and any other
        // failure are swallowed and the decorated decision is returned unchanged.
        var unavailable = await IssueAsync(context, plane, unavailableSigner, EconomyProtectedOperationState.Ready, "subject-unavailable");
        unavailable.State.Should().Be(EconomyProtectedOperationState.Ready);
        var throwingPlane = new RecordingControlPlane(
            ComplianceEvidenceIngestionStatus.Published, new InvalidOperationException("store down"));
        var contained = await IssueAsync(context, throwingPlane, signer, EconomyProtectedOperationState.Ready, "subject-throwing");
        contained.State.Should().Be(EconomyProtectedOperationState.Ready);

        // A non-relational context cannot be queried: contained like any other ingestion fault.
        var nonRelational = new EconomyRiskDecisionTrustSafetyIngestion(
            new FixedIssuer(Decision(EconomyProtectedOperationState.Ready)), plane, signer, new NonDbContext(), logger);
        var nonRelationalDecision = await nonRelational.IssueAsync(Request("subject-nonrelational"), CancellationToken.None);
        nonRelationalDecision.State.Should().Be(EconomyProtectedOperationState.Ready);
        plane.Events.Should().HaveCount(6);
    }

    [Fact]
    public async Task CompositionDecoratesEveryRegistrationShapeAndFailsClosedWithoutAnIssuer()
    {
        await using var database = await EconomyPostgreSqlTestDatabase.CreateAsync("ts_risk_composition");
        await using var context = CreateContext(database.ConnectionString);
        await context.Database.EnsureCreatedAsync();

        Action nullServices = () => EconomyRiskTrustSafetyIngestionComposition.AddEconomyRiskTrustSafetyIngestion(null!);
        nullServices.Should().Throw<ArgumentNullException>();
        Action missingIssuer = () => EconomyRiskTrustSafetyIngestionComposition
            .AddEconomyRiskTrustSafetyIngestion(new ServiceCollection());
        missingIssuer.Should().Throw<InvalidOperationException>();

        // Type-registered issuer: decorated, inner created through ActivatorUtilities.
        var services = new ServiceCollection();
        AddDecoratorDependencies(services, context);
        services.AddScoped<IEconomyProtectedOperationRiskDecisionIssuer, FixedIssuer>();
        services.AddEconomyRiskTrustSafetyIngestion().Should().BeSameAs(services);
        using (var provider = services.BuildServiceProvider())
        {
            var decorated = provider.GetRequiredService<IEconomyProtectedOperationRiskDecisionIssuer>();
            decorated.Should().BeOfType<EconomyRiskDecisionTrustSafetyIngestion>();
            (await decorated.IssueAsync(Request("subject-composition-type"), CancellationToken.None))
                .State.Should().Be(EconomyProtectedOperationState.Ready);
        }

        // Factory-registered issuer: decorated, inner created through the original factory.
        var factoryServices = new ServiceCollection();
        AddDecoratorDependencies(factoryServices, context);
        factoryServices.AddSingleton(_ => new FixedIssuer(Decision(EconomyProtectedOperationState.Denied)));
        factoryServices.AddEconomyRiskTrustSafetyIngestion();
        using (var factoryProvider = factoryServices.BuildServiceProvider())
        {
            var decorated = factoryProvider.GetRequiredService<IEconomyProtectedOperationRiskDecisionIssuer>();
            decorated.Should().BeOfType<EconomyRiskDecisionTrustSafetyIngestion>();
            (await decorated.IssueAsync(Request("subject-composition-factory"), CancellationToken.None))
                .State.Should().Be(EconomyProtectedOperationState.Denied);
        }

        // Instance-registered issuer: neither a type nor a factory — decoration fails closed.
        var instanceServices = new ServiceCollection();
        AddDecoratorDependencies(instanceServices, context);
        instanceServices.AddSingleton<IEconomyProtectedOperationRiskDecisionIssuer>(new FixedIssuer(Decision(EconomyProtectedOperationState.Ready)));
        instanceServices.AddEconomyRiskTrustSafetyIngestion();
        using var instanceProvider = instanceServices.BuildServiceProvider();
        var resolve = () => instanceProvider.GetRequiredService<IEconomyProtectedOperationRiskDecisionIssuer>();
        resolve.Should().Throw<InvalidOperationException>();
    }

    private static void AddDecoratorDependencies(IServiceCollection services, TestDbContext context)
    {
        services.AddSingleton(context);
        services.AddSingleton<IApplicationDbContext>(context);
        services.AddSingleton<ITrustSafetyControlPlane>(new RecordingControlPlane(ComplianceEvidenceIngestionStatus.Published));
        services.AddSingleton<ICapabilityPolicySigner>(new ScriptedSigner());
        services.AddSingleton<ILogger<EconomyRiskDecisionTrustSafetyIngestion>>(
            NullLogger<EconomyRiskDecisionTrustSafetyIngestion>.Instance);
    }

    private static async Task<EconomyProtectedRiskDecision> IssueAsync(
        TestDbContext context,
        ITrustSafetyControlPlane plane,
        ICapabilityPolicySigner signer,
        EconomyProtectedOperationState state,
        string subjectReference)
    {
        var ingestion = new EconomyRiskDecisionTrustSafetyIngestion(
            new FixedIssuer(Decision(state)), plane, signer, context,
            NullLogger<EconomyRiskDecisionTrustSafetyIngestion>.Instance);
        var decision = await ingestion.IssueAsync(Request(subjectReference), CancellationToken.None);
        decision.State.Should().Be(state);
        return decision;
    }

    private static EconomyProtectedRiskDecision Decision(EconomyProtectedOperationState state) => new(
        Guid.NewGuid(), RiskOutcome.Allow, state, null, []);

    private static EconomyProtectedRiskDecisionRequest Request(string subjectReference) => new(
        TenantId, ActorId, subjectReference, "BRA", "jurisdiction-evidence", "fingerprint-1", Intent());

    private static EconomyProtectedOperationIntent Intent() => new(
        EconomyValueMovementCapability.Transfer,
        PostingTemplateKind.Spend,
        new WalletId(Guid.NewGuid()),
        new WalletId(Guid.NewGuid()),
        new CoinAmount(CurrencyCode.HardCoin, 750),
        [new RiskCurrencyLeg(CurrencyCode.HardCoin, 750)],
        [new SourceStampId(Guid.NewGuid())],
        "provider-hash",
        "destination-hash",
        new IdempotencyKey($"intent-{Guid.NewGuid():N}"),
        DateTimeOffset.UtcNow);

    private static TestDbContext CreateContext(string connectionString) => new(
        new DbContextOptionsBuilder<TestDbContext>().UseNpgsql(connectionString).Options);

    private sealed class FixedIssuer(EconomyProtectedRiskDecision decision)
        : IEconomyProtectedOperationRiskDecisionIssuer
    {
        public ValueTask<EconomyProtectedRiskDecision> IssueAsync(
            EconomyProtectedRiskDecisionRequest request, CancellationToken cancellationToken) =>
            ValueTask.FromResult(decision);
    }

    private sealed class ScriptedSigner(Exception? failure = null) : ICapabilityPolicySigner
    {
        public int Calls { get; private set; }

        public ValueTask<CapabilityReceiptSignature> SignAsync(string canonicalPayload, CancellationToken cancellationToken)
        {
            Calls++;
            if (failure is { } error)
            {
                return ValueTask.FromException<CapabilityReceiptSignature>(error);
            }

            return ValueTask.FromResult(new CapabilityReceiptSignature("test-key", "test-signature"));
        }
    }

    private sealed class RecordingControlPlane(
        ComplianceEvidenceIngestionStatus status,
        Exception? failure = null) : ITrustSafetyControlPlane
    {
        public List<TrustSafetyEvent> Events { get; } = [];

        public ValueTask<TrustSafetyIngestionResult> IngestAsync(
            TrustSafetyEvent evidence, CancellationToken cancellationToken)
        {
            if (failure is { } error)
            {
                return ValueTask.FromException<TrustSafetyIngestionResult>(error);
            }

            Events.Add(evidence);
            return ValueTask.FromResult(new TrustSafetyIngestionResult(
                status, status is ComplianceEvidenceIngestionStatus.Published or ComplianceEvidenceIngestionStatus.Duplicate
                    ? Guid.NewGuid()
                    : null));
        }

        public ValueTask<TrustSafetyAppeal> SubmitAppealAsync(TrustSafetyAppeal appeal, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public ValueTask<TrustSafetyAppeal> AssignAppealAsync(
            Guid tenantId, Guid appealId, Guid actorId, long expectedVersion, DateTimeOffset assignedAt,
            CancellationToken cancellationToken) => throw new NotSupportedException();

        public ValueTask<TrustSafetyAppeal> DecideAppealAsync(
            Guid tenantId, Guid appealId, Guid actorId, long expectedCaseVersion, bool overturned,
            string note, string evidenceHash, DateTimeOffset decidedAt, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public ValueTask<IReadOnlyList<TrustSafetyAppeal>> ReadAppealsAsync(
            Guid tenantId, TrustSafetyAppealState? state, int limit, CancellationToken cancellationToken) =>
            throw new NotSupportedException();
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
            new TrustSafetyModelConfiguration().Configure(modelBuilder);
        }

        public Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default) =>
            Database.BeginTransactionAsync(cancellationToken);
    }
}
