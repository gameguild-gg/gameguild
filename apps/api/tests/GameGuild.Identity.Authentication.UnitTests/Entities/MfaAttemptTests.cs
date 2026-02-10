using FluentAssertions;
using GameGuild.Identity.Authentication;
using Xunit;

namespace GameGuild.Identity.Authentication.UnitTests.Entities;

public class MfaAttemptTests
{
    [Fact]
    public void NewMfaAttempt_HasDefaultValues()
    {
        var attempt = new MfaAttempt
        {
            UserId = Guid.NewGuid(),
            Method = MfaMethod.Totp,
            IpAddress = "192.168.1.1",
            UserAgent = "Chrome/120"
        };

        attempt.IsSuccessful.Should().BeFalse();
        attempt.FailureReason.Should().BeNull();
        attempt.ProcessingTimeMs.Should().Be(0);
    }

    [Fact]
    public void SuccessfulMfaAttempt_PropertiesCorrect()
    {
        var userId = Guid.NewGuid();
        var attempt = new MfaAttempt
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Method = MfaMethod.Totp,
            IsSuccessful = true,
            IpAddress = "10.0.0.1",
            UserAgent = "Firefox/110",
            AttemptedAt = DateTime.UtcNow,
            ProcessingTimeMs = 250,
            SessionId = Guid.NewGuid(),
            TenantId = Guid.NewGuid()
        };

        attempt.UserId.Should().Be(userId);
        attempt.Method.Should().Be(MfaMethod.Totp);
        attempt.IsSuccessful.Should().BeTrue();
        attempt.ProcessingTimeMs.Should().Be(250);
    }

    [Fact]
    public void FailedMfaAttempt_HasFailureReason()
    {
        var attempt = new MfaAttempt
        {
            UserId = Guid.NewGuid(),
            Method = MfaMethod.Totp,
            IsSuccessful = false,
            FailureReason = "Invalid code",
            IpAddress = "1.2.3.4",
            UserAgent = "Chrome"
        };

        attempt.IsSuccessful.Should().BeFalse();
        attempt.FailureReason.Should().Be("Invalid code");
    }

    [Fact]
    public void OptionalFields_DefaultToNull()
    {
        var attempt = new MfaAttempt
        {
            UserId = Guid.NewGuid(),
            Method = MfaMethod.Totp,
            IpAddress = "1.2.3.4",
            UserAgent = "Chrome"
        };

        attempt.DeviceFingerprint.Should().BeNull();
        attempt.SessionId.Should().BeNull();
        attempt.TenantId.Should().BeNull();
        attempt.Metadata.Should().BeNull();
    }
}
