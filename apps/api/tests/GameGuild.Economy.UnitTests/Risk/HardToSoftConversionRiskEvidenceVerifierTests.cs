using FluentAssertions;
using GameGuild.Economy.Risk;

namespace GameGuild.Economy.UnitTests.Risk;

public sealed class HardToSoftConversionRiskEvidenceVerifierTests
{
    [Fact]
    public async Task VerifyAsync_RequiresFreshAuditableAllowFromBothIndependentSources()
    {
        var actorId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var observed = DateTimeOffset.UtcNow;
        var financialCrime = new FinancialCrimeSource(AllowFinancialCrime(observed));
        var trustSafety = new TrustSafetySource(AllowTrustSafety(observed));
        var verifier = new HardToSoftConversionRiskEvidenceVerifier(financialCrime, trustSafety);

        await verifier.VerifyAsync(actorId, tenantId, CancellationToken.None);

        financialCrime.SubjectReferences.Should().ContainSingle();
        trustSafety.SubjectReferences.Should().Equal(financialCrime.SubjectReferences);
        financialCrime.SubjectReferences[0].Should().HaveLength(64).And.NotContain(actorId.ToString("N"));
    }

    [Fact]
    public async Task VerifyAsync_FailsClosedWhenEitherSourceDoesNotAllowTheOperation()
    {
        var observed = DateTimeOffset.UtcNow;
        var verifier = new HardToSoftConversionRiskEvidenceVerifier(
            new FinancialCrimeSource(AllowFinancialCrime(observed)),
            new TrustSafetySource(AllowTrustSafety(observed) with { Outcome = ExternalRiskOutcome.Review }));

        var act = () => verifier.VerifyAsync(Guid.NewGuid(), Guid.NewGuid(), CancellationToken.None);

        await act.Should().ThrowAsync<ExternalRiskEvidenceException>()
            .WithMessage("*TrustSafety*");
    }

    [Fact]
    public async Task VerifyAsync_RejectsExpiredEvidenceAndHonorsCancellation()
    {
        var observed = DateTimeOffset.UtcNow;
        var verifier = new HardToSoftConversionRiskEvidenceVerifier(
            new FinancialCrimeSource(AllowFinancialCrime(observed) with { ExpiresAt = observed }),
            new TrustSafetySource(AllowTrustSafety(observed)));

        var expired = () => verifier.VerifyAsync(Guid.NewGuid(), Guid.NewGuid(), CancellationToken.None);
        await expired.Should().ThrowAsync<ExternalRiskEvidenceException>()
            .WithMessage("*FinancialCrime*");

        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var cancelled = () => verifier.VerifyAsync(Guid.NewGuid(), Guid.NewGuid(), cancellation.Token);
        await cancelled.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public void OpaqueReference_IsStablePerActorAndTenant_AndRejectsMissingIdentity()
    {
        var actorId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();

        HardToSoftConversionRiskEvidenceVerifier.CreateOpaqueSubjectReference(actorId, tenantId)
            .Should().Be(HardToSoftConversionRiskEvidenceVerifier.CreateOpaqueSubjectReference(actorId, tenantId));
        HardToSoftConversionRiskEvidenceVerifier.CreateOpaqueSubjectReference(actorId, tenantId)
            .Should().NotBe(HardToSoftConversionRiskEvidenceVerifier.CreateOpaqueSubjectReference(Guid.NewGuid(), tenantId));
        Action noActor = () => HardToSoftConversionRiskEvidenceVerifier.CreateOpaqueSubjectReference(Guid.Empty, tenantId);
        Action noTenant = () => HardToSoftConversionRiskEvidenceVerifier.CreateOpaqueSubjectReference(actorId, Guid.Empty);
        noActor.Should().Throw<ArgumentException>();
        noTenant.Should().Throw<ArgumentException>();
    }

    private static FinancialCrimeRiskInput AllowFinancialCrime(DateTimeOffset observed) => new(
        1, observed.AddMinutes(-1), observed.AddMinutes(5), ExternalRiskOutcome.Allow, "financial-evidence", true);

    private static TrustSafetyRiskInput AllowTrustSafety(DateTimeOffset observed) => new(
        1, observed.AddMinutes(-1), observed.AddMinutes(5), ExternalRiskOutcome.Allow, "trust-evidence", true);

    private sealed class FinancialCrimeSource(FinancialCrimeRiskInput result) : IFinancialCrimeRiskInputSource
    {
        public List<string> SubjectReferences { get; } = [];

        public ValueTask<FinancialCrimeRiskInput> ReadAsync(
            string opaqueSubjectReference,
            DateTimeOffset observedAt,
            CancellationToken cancellationToken = default)
        {
            SubjectReferences.Add(opaqueSubjectReference);
            return ValueTask.FromResult(result);
        }
    }

    private sealed class TrustSafetySource(TrustSafetyRiskInput result) : ITrustSafetyRiskInputSource
    {
        public List<string> SubjectReferences { get; } = [];

        public ValueTask<TrustSafetyRiskInput> ReadAsync(
            string opaqueSubjectReference,
            DateTimeOffset observedAt,
            CancellationToken cancellationToken = default)
        {
            SubjectReferences.Add(opaqueSubjectReference);
            return ValueTask.FromResult(result);
        }
    }
}
