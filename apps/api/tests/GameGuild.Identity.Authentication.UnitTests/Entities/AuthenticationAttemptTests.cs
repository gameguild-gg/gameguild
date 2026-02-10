using FluentAssertions;
using GameGuild.Identity.Authentication;
using Xunit;

namespace GameGuild.Identity.Authentication.UnitTests.Entities;

public class AuthenticationAttemptTests
{
    [Fact]
    public void NewAttempt_HasDefaultValues()
    {
        var attempt = new AuthenticationAttempt
        {
            Email = "test@example.com",
            IpAddress = "192.168.1.1"
        };

        attempt.Id.Should().Be(Guid.Empty);
        attempt.Email.Should().Be("test@example.com");
        attempt.IpAddress.Should().Be("192.168.1.1");
        attempt.IsSuccessful.Should().BeFalse();
        attempt.IsSuspicious.Should().BeFalse();
        attempt.RiskScore.Should().Be(0);
    }

    [Fact]
    public void SuccessfulAttempt_PropertiesSetCorrectly()
    {
        var attempt = new AuthenticationAttempt
        {
            Id = Guid.NewGuid(),
            Email = "user@test.com",
            UserId = Guid.NewGuid(),
            IpAddress = "10.0.0.1",
            UserAgent = "Chrome/120",
            IsSuccessful = true,
            AttemptedAt = DateTime.UtcNow,
            ProcessingTime = TimeSpan.FromMilliseconds(150),
            SessionId = Guid.NewGuid(),
            TenantId = Guid.NewGuid()
        };

        attempt.IsSuccessful.Should().BeTrue();
        attempt.ProcessingTime.TotalMilliseconds.Should().Be(150);
        attempt.SessionId.Should().NotBeNull();
        attempt.FailureReason.Should().BeNull();
    }

    [Fact]
    public void FailedAttempt_HasFailureReason()
    {
        var attempt = new AuthenticationAttempt
        {
            Email = "test@test.com",
            IpAddress = "1.2.3.4",
            IsSuccessful = false,
            FailureReason = "InvalidCredentials",
            IsSuspicious = true,
            RiskScore = 75
        };

        attempt.IsSuccessful.Should().BeFalse();
        attempt.FailureReason.Should().Be("InvalidCredentials");
        attempt.IsSuspicious.Should().BeTrue();
        attempt.RiskScore.Should().Be(75);
    }

    [Fact]
    public void OptionalFields_DefaultToNull()
    {
        var attempt = new AuthenticationAttempt
        {
            Email = "test@test.com",
            IpAddress = "1.2.3.4"
        };

        attempt.UserId.Should().BeNull();
        attempt.UserAgent.Should().BeNull();
        attempt.Location.Should().BeNull();
        attempt.DeviceFingerprint.Should().BeNull();
        attempt.Metadata.Should().BeNull();
        attempt.CorrelationId.Should().BeNull();
    }
}
