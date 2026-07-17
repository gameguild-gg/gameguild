using FluentAssertions;
using Xunit;

[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace GameGuild.Compliance.FERPA.UnitTests.Entities;

public sealed class FerpaEntityTests : IDisposable
{
    private static readonly DateTime Now = new(2026, 7, 17, 12, 0, 0, DateTimeKind.Utc);

    public FerpaEntityTests()
    {
        SystemClock.SetProvider(new FixedTimeProvider(Now));
    }

    public void Dispose()
    {
        SystemClock.Reset();
    }

    [Fact]
    public void DisclosureConsent_IsActiveOnlyInsideItsInclusiveWindow()
    {
        var consent = new FerpaDisclosureConsent
        {
            EffectiveFrom = Now,
            ExpiresAt = Now.AddHours(1)
        };

        consent.IsActiveAt(Now.AddTicks(-1)).Should().BeFalse();
        consent.IsActiveAt(Now).Should().BeTrue();
        consent.IsActiveAt(Now.AddHours(1)).Should().BeTrue();
        consent.IsActiveAt(Now.AddHours(1).AddTicks(1)).Should().BeFalse();
    }

    [Fact]
    public void DisclosureConsent_RevokeIsIdempotentAndDeactivatesConsent()
    {
        var consent = new FerpaDisclosureConsent
        {
            EffectiveFrom = Now.AddDays(-1),
            ExpiresAt = Now.AddDays(1),
            UpdatedAt = Now.AddHours(-1)
        };

        consent.Revoke();
        var firstRevokedAt = consent.RevokedAt;
        consent.Revoke();

        consent.RevokedAt.Should().Be(Now);
        consent.RevokedAt.Should().Be(firstRevokedAt);
        consent.UpdatedAt.Should().Be(Now);
        consent.IsActiveAt(Now).Should().BeFalse();
        consent.ToDto().IsActive.Should().BeFalse();
    }

    [Fact]
    public void InspectionRequest_CompleteCapturesProcessorAndTimestamp()
    {
        var processorId = Guid.NewGuid();
        var request = new FerpaInspectionRequest { Status = FerpaRequestStatus.InReview };

        request.Complete(processorId, "Records released");

        request.Status.Should().Be(FerpaRequestStatus.Completed);
        request.ProcessedByUserId.Should().Be(processorId);
        request.ProcessedAt.Should().Be(Now);
        request.ProcessingNotes.Should().Be("Records released");
    }

    [Fact]
    public void InspectionRequest_DenyCapturesReason()
    {
        var processorId = Guid.NewGuid();
        var request = new FerpaInspectionRequest();

        request.Deny(processorId, "Identity could not be verified");

        request.Status.Should().Be(FerpaRequestStatus.Denied);
        request.ProcessedByUserId.Should().Be(processorId);
        request.ProcessedAt.Should().Be(Now);
        request.ProcessingNotes.Should().Be("Identity could not be verified");
    }

    [Theory]
    [InlineData(FerpaRequestStatus.Completed)]
    [InlineData(FerpaRequestStatus.Denied)]
    [InlineData(FerpaRequestStatus.Expired)]
    public void InspectionRequest_TerminalStatusCannotBeProcessedAgain(FerpaRequestStatus status)
    {
        var request = new FerpaInspectionRequest { Status = status };

        var complete = () => request.Complete(Guid.NewGuid(), null);
        var deny = () => request.Deny(Guid.NewGuid(), "Denied");

        complete.Should().Throw<InvalidOperationException>()
            .WithMessage($"*{status}*");
        deny.Should().Throw<InvalidOperationException>()
            .WithMessage($"*{status}*");
    }

    [Fact]
    public void DirectoryPolicy_UpdateReplacesAllMutableValues()
    {
        var noticeSentAt = Now.AddDays(-1);
        var policy = new FerpaDirectoryInformationPolicy();

        policy.Update("[\"displayName\"]", false, noticeSentAt, "https://school.test/ferpa");

        policy.ToDto().Should().Match<FerpaDirectoryInformationPolicyDto>(dto =>
            dto.AllowedFieldsJson == "[\"displayName\"]" &&
            !dto.OptOutEnabled &&
            dto.AnnualNoticeSentAt == noticeSentAt &&
            dto.NoticeUrl == "https://school.test/ferpa");
    }

    private sealed class FixedTimeProvider(DateTime utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(utcNow, TimeSpan.Zero);
    }
}
