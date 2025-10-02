using FluentAssertions;
using GameGuild.Modules.Authentication;
using Xunit;

namespace GameGuild.Tests.Authentication.Unit.Entities;

/// <summary>
/// Unit tests for the AuthenticationAttempt entity
/// Tests the properties and behavior of authentication attempt tracking
/// </summary>
public class AuthenticationAttemptTests
{
    [Fact]
    public void AuthenticationAttempt_ShouldInitializeWithDefaults()
    {
        // Arrange & Act
        var attempt = new AuthenticationAttempt();

        // Assert
        attempt.Email.Should().BeEmpty();
        attempt.IpAddress.Should().BeEmpty();
        attempt.UserId.Should().BeNull();
        attempt.UserAgent.Should().BeNull();
        attempt.IsSuccessful.Should().BeFalse();
        attempt.IsSuspicious.Should().BeFalse();
        attempt.RiskScore.Should().Be(0);
    }

    [Fact]
    public void AuthenticationAttempt_ShouldSetPropertiesCorrectly()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var attemptedAt = DateTime.UtcNow;
        var processingTime = TimeSpan.FromMilliseconds(150);

        // Act
        var attempt = new AuthenticationAttempt
        {
            Email = "test@example.com",
            UserId = userId,
            IpAddress = "192.168.1.1",
            UserAgent = "Mozilla/5.0",
            IsSuccessful = true,
            FailureReason = null,
            AttemptedAt = attemptedAt,
            ProcessingTime = processingTime,
            Location = "San Francisco, CA",
            DeviceFingerprint = "device-hash-123",
            SessionId = sessionId,
            TenantId = tenantId,
            IsSuspicious = false,
            RiskScore = 15,
            Metadata = "{\"ip_version\":\"IPv4\"}",
            CorrelationId = "correlation-123"
        };

        // Assert
        attempt.Email.Should().Be("test@example.com");
        attempt.UserId.Should().Be(userId);
        attempt.IpAddress.Should().Be("192.168.1.1");
        attempt.UserAgent.Should().Be("Mozilla/5.0");
        attempt.IsSuccessful.Should().BeTrue();
        attempt.FailureReason.Should().BeNull();
        attempt.AttemptedAt.Should().Be(attemptedAt);
        attempt.ProcessingTime.Should().Be(processingTime);
        attempt.Location.Should().Be("San Francisco, CA");
        attempt.DeviceFingerprint.Should().Be("device-hash-123");
        attempt.SessionId.Should().Be(sessionId);
        attempt.TenantId.Should().Be(tenantId);
        attempt.IsSuspicious.Should().BeFalse();
        attempt.RiskScore.Should().Be(15);
        attempt.Metadata.Should().Be("{\"ip_version\":\"IPv4\"}");
        attempt.CorrelationId.Should().Be("correlation-123");
    }

    [Fact]
    public void AuthenticationAttempt_ShouldHandleFailedAttempt()
    {
        // Arrange & Act
        var attempt = new AuthenticationAttempt
        {
            Email = "hacker@example.com",
            UserId = null,
            IpAddress = "10.0.0.1",
            IsSuccessful = false,
            FailureReason = "InvalidCredentials",
            IsSuspicious = true,
            RiskScore = 85,
            AttemptedAt = DateTime.UtcNow
        };

        // Assert
        attempt.IsSuccessful.Should().BeFalse();
        attempt.FailureReason.Should().Be("InvalidCredentials");
        attempt.IsSuspicious.Should().BeTrue();
        attempt.RiskScore.Should().Be(85);
        attempt.UserId.Should().BeNull();
    }
}
