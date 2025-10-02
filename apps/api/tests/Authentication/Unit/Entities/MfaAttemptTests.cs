using FluentAssertions;
using GameGuild.Modules.Authentication;
using Xunit;

namespace GameGuild.Tests.Authentication.Unit.Entities;

/// <summary>
/// Unit tests for the MfaAttempt entity
/// Tests the properties and behavior of MFA attempt logging
/// </summary>
public class MfaAttemptTests
{
    [Fact]
    public void MfaAttempt_ShouldInitializeWithDefaults()
    {
        // Arrange & Act
        var attempt = new MfaAttempt();

        // Assert
        attempt.IpAddress.Should().BeEmpty();
        attempt.UserAgent.Should().BeEmpty();
        attempt.IsSuccessful.Should().BeFalse();
        attempt.FailureReason.Should().BeNull();
        attempt.Location.Should().BeNull();
        attempt.SessionId.Should().BeNull();
    }

    [Fact]
    public void MfaAttempt_ShouldSetPropertiesCorrectly()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();

        // Act
        var attempt = new MfaAttempt
        {
            UserId = userId,
            Method = MfaMethod.Totp,
            IsSuccessful = true,
            IpAddress = "192.168.1.1",
            UserAgent = "Mozilla/5.0",
            FailureReason = null,
            Location = "New York, NY",
            SessionId = sessionId
        };

        // Assert
        attempt.UserId.Should().Be(userId);
        attempt.Method.Should().Be(MfaMethod.Totp);
        attempt.IsSuccessful.Should().BeTrue();
        attempt.IpAddress.Should().Be("192.168.1.1");
        attempt.UserAgent.Should().Be("Mozilla/5.0");
        attempt.FailureReason.Should().BeNull();
        attempt.Location.Should().Be("New York, NY");
        attempt.SessionId.Should().Be(sessionId);
    }

    [Fact]
    public void MfaAttempt_ShouldHandleFailedAttempt()
    {
        // Arrange
        var userId = Guid.NewGuid();

        // Act
        var attempt = new MfaAttempt
        {
            UserId = userId,
            Method = MfaMethod.Sms,
            IsSuccessful = false,
            FailureReason = "InvalidCode",
            IpAddress = "10.0.0.1",
            UserAgent = "Mobile App"
        };

        // Assert
        attempt.IsSuccessful.Should().BeFalse();
        attempt.FailureReason.Should().Be("InvalidCode");
        attempt.SessionId.Should().BeNull();
    }

    [Fact]
    public void MfaAttempt_ShouldSupportDifferentMethods()
    {
        // Arrange & Act
        var totpAttempt = new MfaAttempt { Method = MfaMethod.Totp };
        var smsAttempt = new MfaAttempt { Method = MfaMethod.Sms };
        var emailAttempt = new MfaAttempt { Method = MfaMethod.Email };

        // Assert
        totpAttempt.Method.Should().Be(MfaMethod.Totp);
        smsAttempt.Method.Should().Be(MfaMethod.Sms);
        emailAttempt.Method.Should().Be(MfaMethod.Email);
    }
}
