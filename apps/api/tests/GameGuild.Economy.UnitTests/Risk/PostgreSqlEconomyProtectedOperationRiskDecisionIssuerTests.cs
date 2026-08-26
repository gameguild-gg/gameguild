using System.Security.Cryptography;
using System.Text;
using FluentAssertions;
using GameGuild.Economy.Contracts;
using GameGuild.Economy.Persistence;
using GameGuild.Economy.Reserves;
using GameGuild.Economy.Risk;
using GameGuild.TestSupport.Economy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Storage;
using Moq;

namespace GameGuild.Economy.UnitTests.Risk;

public sealed class PostgreSqlEconomyProtectedOperationRiskDecisionIssuerTests
{
    private static readonly Guid TenantId = Guid.Parse("99000000-0000-0000-0000-000000000001");
    private static readonly Guid ActorId = Guid.Parse("99000000-0000-0000-0000-000000000002");
    private static readonly DateTimeOffset Now = new(2026, 8, 26, 15, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task IssueAsync_PersistsAllowAndReviewButFailsClosedForUnavailableEvidence()
    {
        await using var database = await EconomyPostgreSqlTestDatabase.CreateAsync("protected_risk_issuer");
        await using var context = CreateContext(database.ConnectionString);
        await context.Database.EnsureCreatedAsync();
        var sourceWallet = Guid.NewGuid();
        var destinationWallet = Guid.NewGuid();
        await SeedControlPlaneAsync(context, sourceWallet, destinationWallet);
        var policy = Policy();
        var policyStore = new Mock<IEconomyCapabilityPolicyStore>();
        policyStore.Setup(value => value.CurrentAsync(
                TenantId, EconomyValueMovementCapability.BountyEscrow, "BRA", It.IsAny<CancellationToken>()))
            .ReturnsAsync(policy);
        var signature = new Mock<ICapabilityPolicySignatureVerifier>();
        signature.Setup(value => value.VerifyAsync(
                policy.CanonicalPayload, policy.KeyId, policy.Signature, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        var financialCrime = new MutableFinancialCrimeSource(ExternalRiskOutcome.Allow);
        var trustSafety = new MutableTrustSafetySource(ExternalRiskOutcome.Allow);
        var graph = new Mock<IEntityRiskGraphStore>();
        graph.Setup(value => value.ClusterForAsync(
                TenantId, It.IsAny<RiskEntityNode>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EntityRiskCluster("cluster", 3, "graph-evidence", []));
        var counters = new Mock<IAggregateRiskCounterStore>();
        counters.Setup(value => value.ReserveAsync(
                It.IsAny<Guid>(), TenantId, It.IsAny<Guid>(), PostingTemplateKind.BountyEscrow,
                It.IsAny<CoinAmount>(), It.IsAny<IReadOnlyCollection<AggregateRiskLimit>>(),
                It.IsAny<DateTimeOffset>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid reservationId, Guid tenantId, Guid riskDecisionId,
                PostingTemplateKind operation, CoinAmount amount, IReadOnlyCollection<AggregateRiskLimit> _,
                DateTimeOffset reservedAt, DateTimeOffset expiresAt, CancellationToken _) =>
                new DurableAggregateRiskCounterReservation(
                    reservationId, tenantId, riskDecisionId, "counter-fingerprint", operation,
                    amount, [], reservedAt, expiresAt, RiskCounterReservationStatus.Reserved));
        var reviews = new Mock<IRiskReviewStore>();
        reviews.Setup(value => value.SubmitAsync(
                TenantId, It.IsAny<Guid>(), It.IsAny<Guid>(), ActorId,
                It.IsAny<IReadOnlyList<string>>(), Now, 2, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid _, Guid reviewId, Guid decisionId, Guid submittedBy,
                IReadOnlyList<string> _, DateTimeOffset submittedAt, int approvals, CancellationToken _) =>
                new RiskReviewCase(reviewId, decisionId, submittedBy, RiskReviewStatus.Pending,
                    submittedAt, null, null, null, approvals, [], null));
        var holds = new Mock<IComplianceHoldStore>();
        var issuer = new PostgreSqlEconomyProtectedOperationRiskDecisionIssuer(
            context, policyStore.Object, signature.Object, financialCrime, trustSafety,
            graph.Object, counters.Object, reviews.Object, holds.Object);

        var allow = await issuer.IssueAsync(Request(sourceWallet, destinationWallet, "allow"), default);

        allow.State.Should().Be(EconomyProtectedOperationState.Ready);
        allow.Outcome.Should().Be(RiskOutcome.Allow);
        (await context.Set<EconomyRiskDecisionRow>().SingleAsync(row => row.Id == allow.Id))
            .OperationFingerprint.Should().Be("allow-fingerprint");
        counters.Verify(value => value.ReserveAsync(
            It.IsAny<Guid>(), TenantId, allow.Id, PostingTemplateKind.BountyEscrow,
            It.Is<CoinAmount>(amount => amount.Units == 100),
            It.Is<IReadOnlyCollection<AggregateRiskLimit>>(limits => limits.Count == 3),
            Now, It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()), Times.Once);

        trustSafety.Outcome = ExternalRiskOutcome.Review;
        var review = await issuer.IssueAsync(Request(sourceWallet, destinationWallet, "review"), default);
        review.State.Should().Be(EconomyProtectedOperationState.ReviewRequired);
        review.ReviewId.Should().NotBeNull();
        reviews.Verify(value => value.SubmitAsync(
            TenantId, review.ReviewId!.Value, review.Id, ActorId,
            It.IsAny<IReadOnlyList<string>>(), Now, 2, It.IsAny<CancellationToken>()), Times.Once);

        trustSafety.Outcome = ExternalRiskOutcome.Unavailable;
        var unavailable = await issuer.IssueAsync(Request(sourceWallet, destinationWallet, "unavailable"), default);
        unavailable.Id.Should().Be(Guid.Empty);
        unavailable.State.Should().Be(EconomyProtectedOperationState.ComplianceUnavailable);
        (await context.Set<EconomyRiskDecisionRow>().CountAsync()).Should().Be(2);
    }

    private static EconomyProtectedRiskDecisionRequest Request(
        Guid sourceWallet,
        Guid destinationWallet,
        string key) => new(
        TenantId,
        ActorId,
        EconomySubjectReference.ForUser(TenantId, ActorId),
        "BRA",
        "kyc-evidence",
        $"{key}-fingerprint",
        new EconomyProtectedOperationIntent(
            EconomyValueMovementCapability.BountyEscrow,
            PostingTemplateKind.BountyEscrow,
            new WalletId(sourceWallet),
            new WalletId(destinationWallet),
            new CoinAmount(CurrencyCode.HardCoin, 100),
            [new RiskCurrencyLeg(CurrencyCode.HardCoin, 100)],
            [new SourceStampId(Guid.NewGuid())],
            "provider-hash",
            "destination-hash",
            new IdempotencyKey(key),
            Now));

    private static EconomyCapabilityPolicy Policy()
    {
        const string payload = """
            {"riskDecisionLifetimeSeconds":300,"riskReviewRequiredApprovals":2,"complianceHoldSeconds":86400,"riskLimits":[{"dimension":"Wallet","subject":"SourceWallet","counterVersion":7,"maximumUnits":1000,"windowSeconds":86400},{"dimension":"IdentityCluster","subject":"IdentityCluster","counterVersion":7,"maximumUnits":2000,"windowSeconds":86400},{"dimension":"Tenant","subject":"Tenant","counterVersion":7,"maximumUnits":5000,"windowSeconds":86400}]}
            """;
        return new EconomyCapabilityPolicy(
            Guid.NewGuid(), $"{TenantId:N}:6:BRA", TenantId,
            EconomyValueMovementCapability.BountyEscrow, "BRA", 9, payload, Hash(payload),
            "kms-key", "signature", Guid.NewGuid(), Guid.NewGuid(), Now.AddDays(-2),
            Now.AddDays(-1), Now.AddDays(-1), Now.AddDays(1), true,
            EconomyCapabilityPolicyState.Active);
    }

    private static async Task SeedControlPlaneAsync(
        IssuerDbContext context,
        Guid sourceWallet,
        Guid destinationWallet)
    {
        context.Set<EconomyWalletRow>().AddRange(
            new EconomyWalletRow { Id = sourceWallet, OwnerId = ActorId, TenantId = TenantId, State = WalletLifecycleState.Active, CreatedAt = Now },
            new EconomyWalletRow { Id = destinationWallet, OwnerId = Guid.NewGuid(), TenantId = TenantId, State = WalletLifecycleState.Active, CreatedAt = Now });
        context.Set<EconomyReserveHeadRow>().Add(new EconomyReserveHeadRow
        {
            Version = 4, PolicyVersion = 2, AuthorizationEpoch = 3, ObservedAt = Now.AddMinutes(-1),
            ExpiresAt = Now.AddMinutes(10), Coverage = ReserveCoverageState.Covered,
            EvidenceHash = "reserve-evidence", IsActive = true, ActivatedAt = Now.AddMinutes(-1)
        });
        await context.SaveChangesAsync();
    }

    private static string Hash(string value) => Convert.ToHexStringLower(
        SHA256.HashData(Encoding.UTF8.GetBytes(value)));

    private static IssuerDbContext CreateContext(string connectionString) => new(
        new DbContextOptionsBuilder<IssuerDbContext>().UseNpgsql(connectionString)
            .ConfigureWarnings(warnings => warnings.Ignore(RelationalEventId.PendingModelChangesWarning)).Options);

    private sealed class IssuerDbContext(DbContextOptions<IssuerDbContext> options)
        : DbContext(options), IApplicationDbContext
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder) =>
            new EconomyModelConfiguration().Configure(modelBuilder);
        public Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default) =>
            Database.BeginTransactionAsync(cancellationToken);
    }

    private sealed class MutableFinancialCrimeSource(ExternalRiskOutcome outcome) : IFinancialCrimeRiskInputSource
    {
        public ExternalRiskOutcome Outcome { get; set; } = outcome;
        public ValueTask<FinancialCrimeRiskInput> ReadAsync(string subject, DateTimeOffset at, CancellationToken token = default) =>
            ValueTask.FromResult(new FinancialCrimeRiskInput(1, at.AddMinutes(-1), at.AddMinutes(5), Outcome, "financial-crime", true));
    }

    private sealed class MutableTrustSafetySource(ExternalRiskOutcome outcome) : ITrustSafetyRiskInputSource
    {
        public ExternalRiskOutcome Outcome { get; set; } = outcome;
        public ValueTask<TrustSafetyRiskInput> ReadAsync(string subject, DateTimeOffset at, CancellationToken token = default) =>
            ValueTask.FromResult(new TrustSafetyRiskInput(1, at.AddMinutes(-1), at.AddMinutes(5), Outcome, "trust-safety", true));
    }
}
