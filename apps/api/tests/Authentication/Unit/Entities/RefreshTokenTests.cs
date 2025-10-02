using FluentAssertions;
using GameGuild.Modules.Authentication;
using Xunit;

namespace GameGuild.Tests.Authentication.Unit.Entities;

/// <summary>
/// Unit tests for the RefreshToken entity
/// Tests the properties and behavior of refresh token management
/// </summary>
public class RefreshTokenTests
{
    [Fact]
    public void RefreshToken_ShouldInitializeWithDefaults()
    {
        // Arrange & Act
        var token = new RefreshToken();

        // Assert
        token.Token.Should().BeEmpty();
        token.CreatedByIp.Should().BeEmpty();
        token.IsRevoked.Should().BeFalse();
        token.RevokedAt.Should().BeNull();
        token.RevokedByIp.Should().BeNull();
        token.ReplacedByToken.Should().BeNull();
    }

    [Fact]
    public void RefreshToken_ShouldSetPropertiesCorrectly()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var expiresAt = DateTime.UtcNow.AddDays(7);

        // Act
        var token = new RefreshToken
        {
            UserId = userId,
            Token = "refresh-token-abc123",
            ExpiresAt = expiresAt,
            CreatedByIp = "192.168.1.1",
            IsRevoked = false
        };

        // Assert
        token.UserId.Should().Be(userId);
        token.Token.Should().Be("refresh-token-abc123");
        token.ExpiresAt.Should().Be(expiresAt);
        token.CreatedByIp.Should().Be("192.168.1.1");
        token.IsRevoked.Should().BeFalse();
    }

    [Fact]
    public void IsExpired_ShouldReturnTrue_WhenTokenIsExpired()
    {
        // Arrange
        var token = new RefreshToken
        {
            ExpiresAt = DateTime.UtcNow.AddDays(-1)
        };

        // Act & Assert
        token.IsExpired.Should().BeTrue();
        token.IsActive.Should().BeFalse();
    }

    [Fact]
    public void IsExpired_ShouldReturnFalse_WhenTokenIsNotExpired()
    {
        // Arrange
        var token = new RefreshToken
        {
            ExpiresAt = DateTime.UtcNow.AddDays(7)
        };

        // Act & Assert
        token.IsExpired.Should().BeFalse();
    }

    [Fact]
    public void IsActive_ShouldReturnTrue_WhenTokenIsNotRevokedAndNotExpired()
    {
        // Arrange
        var token = new RefreshToken
        {
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            IsRevoked = false
        };

        // Act & Assert
        token.IsActive.Should().BeTrue();
    }

    [Fact]
    public void IsActive_ShouldReturnFalse_WhenTokenIsRevoked()
    {
        // Arrange
        var token = new RefreshToken
        {
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            IsRevoked = true,
            RevokedAt = DateTime.UtcNow,
            RevokedByIp = "10.0.0.1"
        };

        // Act & Assert
        token.IsActive.Should().BeFalse();
    }

    [Fact]
    public void IsActive_ShouldReturnFalse_WhenTokenIsExpired()
    {
        // Arrange
        var token = new RefreshToken
        {
            ExpiresAt = DateTime.UtcNow.AddDays(-1),
            IsRevoked = false
        };

        // Act & Assert
        token.IsActive.Should().BeFalse();
    }

    [Fact]
    public void RefreshToken_ShouldHandleTokenReplacement()
    {
        // Arrange & Act
        var token = new RefreshToken
        {
            Token = "old-token",
            IsRevoked = true,
            RevokedAt = DateTime.UtcNow,
            RevokedByIp = "192.168.1.1",
            ReplacedByToken = "new-token"
        };

        // Assert
        token.IsRevoked.Should().BeTrue();
        token.ReplacedByToken.Should().Be("new-token");
        token.RevokedByIp.Should().Be("192.168.1.1");
        token.RevokedAt.Should().NotBeNull();
    }
}
