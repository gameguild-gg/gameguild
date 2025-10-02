using FluentAssertions;
using GameGuild.Modules.Authentication;
using Xunit;

namespace GameGuild.Tests.Authentication.Unit.Entities;

/// <summary>
/// Unit tests for the UserSession entity
/// Tests the properties and behavior of user session management
/// </summary>
public class UserSessionTests
{
    [Fact]
    public void UserSession_ShouldInitializeWithDefaults()
    {
        // Arrange & Act
        var session = new UserSession();

        // Assert
        session.RefreshToken.Should().BeEmpty();
        session.IpAddress.Should().BeEmpty();
        session.UserAgent.Should().BeEmpty();
        session.AccessTokenHash.Should().BeNull();
        session.DeviceFingerprint.Should().BeNull();
        session.DeviceInfo.Should().BeNull();
        session.Location.Should().BeNull();
    }

    [Fact]
    public void UserSession_ShouldSetPropertiesCorrectly()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var expiresAt = DateTime.UtcNow.AddDays(7);

        // Act
        var session = new UserSession
        {
            UserId = userId,
            RefreshToken = "refresh-token-abc123",
            AccessTokenHash = "access-token-hash",
            IpAddress = "192.168.1.1",
            UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64)",
            DeviceFingerprint = "device-fingerprint-hash",
            DeviceInfo = "{\"os\":\"Windows 11\",\"browser\":\"Chrome 120\"}",
            Location = "{\"city\":\"San Francisco\",\"country\":\"US\"}",
            ExpiresAt = expiresAt
        };

        // Assert
        session.UserId.Should().Be(userId);
        session.RefreshToken.Should().Be("refresh-token-abc123");
        session.AccessTokenHash.Should().Be("access-token-hash");
        session.IpAddress.Should().Be("192.168.1.1");
        session.UserAgent.Should().Be("Mozilla/5.0 (Windows NT 10.0; Win64; x64)");
        session.DeviceFingerprint.Should().Be("device-fingerprint-hash");
        session.DeviceInfo.Should().Be("{\"os\":\"Windows 11\",\"browser\":\"Chrome 120\"}");
        session.Location.Should().Be("{\"city\":\"San Francisco\",\"country\":\"US\"}");
        session.ExpiresAt.Should().Be(expiresAt);
    }

    [Fact]
    public void UserSession_ShouldHandleMobileSession()
    {
        // Arrange & Act
        var session = new UserSession
        {
            IpAddress = "10.0.0.1",
            UserAgent = "Mobile App iOS/17.0",
            DeviceInfo = "{\"os\":\"iOS 17\",\"device\":\"iPhone 15 Pro\"}",
            Location = "{\"city\":\"New York\",\"country\":\"US\"}"
        };

        // Assert
        session.UserAgent.Should().Contain("Mobile App");
        session.DeviceInfo.Should().Contain("iOS 17");
    }

    [Fact]
    public void UserSession_ShouldTrackSessionExpiration()
    {
        // Arrange
        var expiresAt = DateTime.UtcNow.AddDays(7);

        // Act
        var session = new UserSession
        {
            RefreshToken = "token-123",
            ExpiresAt = expiresAt
        };

        // Assert
        session.ExpiresAt.Should().Be(expiresAt);
    }
}
