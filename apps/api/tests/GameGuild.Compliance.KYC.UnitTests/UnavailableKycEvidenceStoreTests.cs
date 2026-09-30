using FluentAssertions;
using Xunit;

namespace GameGuild.Compliance.KYC.Tests;

public sealed class UnavailableKycEvidenceStoreTests
{
    [Theory]
    [InlineData(KycEvidenceResult.Approved)]
    [InlineData(KycEvidenceResult.Rejected)]
    [InlineData(KycEvidenceResult.NeedsReview)]
    [InlineData(KycEvidenceResult.Unavailable)]
    public async Task Ingest_WithoutADurableLedger_RejectsEveryEvidenceOutcome(KycEvidenceResult result)
    {
        IKycEvidenceStore store = new UnavailableKycEvidenceStore();
        var receivedAt = new DateTimeOffset(2026, 9, 29, 12, 0, 0, TimeSpan.Zero);
        var evidence = new KycEvidenceSubmission(
            Provider: "sumsub",
            Environment: "sandbox",
            ProviderEventId: "unavailable-ledger-event",
            TenantId: Guid.NewGuid(),
            SubjectHash: "opaque-subject-hash",
            Version: 1,
            Result: result,
            IssuedAt: receivedAt,
            ExpiresAt: receivedAt.AddDays(30),
            PolicyVersion: 1,
            PayloadHash: "verified-payload-hash",
            SignatureVerified: true,
            RawObjectReference: "evidence://unavailable-ledger-event",
            ReceivedAt: receivedAt);

        Func<Task> ingest = () => store.IngestAsync(evidence, CancellationToken.None).AsTask();

        await ingest.Should().ThrowAsync<SumSubNotConfiguredException>()
            .WithMessage("A durable KYC evidence store must be configured before activating SumSub orchestration.");
    }
}
