using FluentAssertions;
using Xunit;

namespace GameGuild.Identity.Authentication.UnitTests.Entities;

/// <summary>
/// Unit tests for RefreshToken entity
/// </summary>
public class RefreshTokenTests
{
    [Fact]
    public void RefreshToken_IsExpired_WhenExpired_ShouldReturnTrue()
    {
        // Arrange
        var token = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Token = "test-token",
            ExpiresAt = DateTime.UtcNow.AddDays(-1), // Expired
            CreatedByIp = "127.0.0.1",
            CreatedAt = DateTime.UtcNow.AddDays(-2)
        };

        // Act & Assert
        token.IsExpired.Should().BeTrue();
    }

    [Fact]
    public void RefreshToken_IsExpired_WhenNotExpired_ShouldReturnFalse()
    {
        // Arrange
        var token = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Token = "test-token",
            ExpiresAt = DateTime.UtcNow.AddDays(1), // Not expired
            CreatedByIp = "127.0.0.1"
        };

        // Act & Assert
        token.IsExpired.Should().BeFalse();
    }

    [Fact]
    public void RefreshToken_IsActive_WhenActiveToken_ShouldReturnTrue()
    {
        // Arrange
        var token = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Token = "test-token",
            ExpiresAt = DateTime.UtcNow.AddDays(1),
            IsRevoked = false,
            CreatedByIp = "127.0.0.1"
        };

        // Act & Assert
        token.IsActive.Should().BeTrue();
    }

    [Fact]
    public void RefreshToken_IsActive_WhenRevoked_ShouldReturnFalse()
    {
        // Arrange
        var token = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Token = "test-token",
            ExpiresAt = DateTime.UtcNow.AddDays(1),
            IsRevoked = true,
            RevokedAt = DateTime.UtcNow,
            RevokedByIp = "127.0.0.1",
            CreatedByIp = "127.0.0.1",
            CreatedAt = DateTime.UtcNow.AddDays(-1)
        };

        // Act & Assert
        token.IsActive.Should().BeFalse();
    }

    [Fact]
    public void RefreshToken_IsActive_WhenExpired_ShouldReturnFalse()
    {
        // Arrange
        var token = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Token = "test-token",
            ExpiresAt = DateTime.UtcNow.AddDays(-1), // Expired
            IsRevoked = false,
            CreatedByIp = "127.0.0.1",
            CreatedAt = DateTime.UtcNow.AddDays(-2)
        };

        // Act & Assert
        token.IsActive.Should().BeFalse();
    }

    [Fact]
    public void RefreshToken_Creation_ShouldSetPropertiesCorrectly()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var token = "test-refresh-token";
        var expiresAt = DateTime.UtcNow.AddDays(7);
        var createdByIp = "192.168.1.1";

        // Act
        var refreshToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Token = token,
            ExpiresAt = expiresAt,
            CreatedByIp = createdByIp
        };

        // Assert
        refreshToken.UserId.Should().Be(userId);
        refreshToken.Token.Should().Be(token);
        refreshToken.ExpiresAt.Should().BeCloseTo(expiresAt, TimeSpan.FromSeconds(1));
        refreshToken.CreatedByIp.Should().Be(createdByIp);
        refreshToken.IsRevoked.Should().BeFalse();
        refreshToken.IsActive.Should().BeTrue();
    }

    [Fact]
    public void RefreshToken_Revocation_ShouldSetRevokedProperties()
    {
        // Arrange
        var token = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Token = "test-token",
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            CreatedByIp = "127.0.0.1"
        };

        var revokedByIp = "192.168.1.1";
        var replacedByToken = "new-token";

        // Act
        token.IsRevoked = true;
        token.RevokedAt = DateTime.UtcNow;
        token.RevokedByIp = revokedByIp;
        token.ReplacedByToken = replacedByToken;

        // Assert
        token.IsRevoked.Should().BeTrue();
        token.RevokedAt.Should().NotBeNull();
        token.RevokedByIp.Should().Be(revokedByIp);
        token.ReplacedByToken.Should().Be(replacedByToken);
        token.IsActive.Should().BeFalse();
    }
}
