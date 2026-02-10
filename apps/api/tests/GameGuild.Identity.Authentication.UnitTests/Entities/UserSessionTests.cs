using FluentAssertions;
using GameGuild.Identity.Authentication;
using Xunit;

namespace GameGuild.Identity.Authentication.UnitTests.Entities;

public class UserSessionTests
{
    private static UserSession CreateSession(
        bool isActive = true,
        DateTime? expiresAt = null)
    {
        return new UserSession
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            RefreshToken = "token-123",
            IpAddress = "192.168.1.1",
            UserAgent = "TestBrowser/1.0",
            IsActive = isActive,
            ExpiresAt = expiresAt ?? DateTime.UtcNow.AddHours(1),
            LastUsedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    [Fact]
    public void IsExpired_FutureExpiry_ReturnsFalse()
    {
        var session = CreateSession(expiresAt: DateTime.UtcNow.AddHours(1));
        session.IsExpired.Should().BeFalse();
    }

    [Fact]
    public void IsExpired_PastExpiry_ReturnsTrue()
    {
        var session = CreateSession(expiresAt: DateTime.UtcNow.AddHours(-1));
        session.IsExpired.Should().BeTrue();
    }

    [Fact]
    public void IsValid_ActiveAndNotExpired_ReturnsTrue()
    {
        var session = CreateSession(isActive: true, expiresAt: DateTime.UtcNow.AddHours(1));
        session.IsValid.Should().BeTrue();
    }

    [Fact]
    public void IsValid_Inactive_ReturnsFalse()
    {
        var session = CreateSession(isActive: false, expiresAt: DateTime.UtcNow.AddHours(1));
        session.IsValid.Should().BeFalse();
    }

    [Fact]
    public void IsValid_Expired_ReturnsFalse()
    {
        var session = CreateSession(isActive: true, expiresAt: DateTime.UtcNow.AddHours(-1));
        session.IsValid.Should().BeFalse();
    }

    [Fact]
    public void IsValid_InactiveAndExpired_ReturnsFalse()
    {
        var session = CreateSession(isActive: false, expiresAt: DateTime.UtcNow.AddHours(-1));
        session.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Properties_SetCorrectly()
    {
        var session = CreateSession();
        session.RefreshToken.Should().Be("token-123");
        session.IpAddress.Should().Be("192.168.1.1");
        session.UserAgent.Should().Be("TestBrowser/1.0");
    }

    [Fact]
    public void TerminationFields_DefaultToNull()
    {
        var session = CreateSession();
        session.TerminationReason.Should().BeNull();
        session.TerminatedAt.Should().BeNull();
    }

    [Fact]
    public void TrustedDevice_DefaultsToFalse()
    {
        var session = CreateSession();
        session.IsTrustedDevice.Should().BeFalse();
        session.TrustedAt.Should().BeNull();
    }
}
