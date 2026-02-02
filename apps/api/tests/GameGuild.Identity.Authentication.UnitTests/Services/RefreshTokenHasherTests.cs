using FluentAssertions;
using Xunit;

namespace GameGuild.Identity.Authentication.UnitTests.Services;

public class RefreshTokenHasherTests
{
    private readonly RefreshTokenHasher _hasher;

    public RefreshTokenHasherTests()
    {
        _hasher = new RefreshTokenHasher();
    }

    [Fact]
    public void HashToken_WithValidToken_ReturnsHash()
    {
        // Arrange
        var token = "valid-refresh-token-12345";

        // Act
        var hash = _hasher.HashToken(token);

        // Assert
        hash.Should().NotBeNullOrEmpty();
        hash.Should().MatchRegex(@"^[A-Za-z0-9+/]+=*$");  // Base64 format
    }

    [Fact]
    public void HashToken_WithSameToken_ReturnsConsistentHash()
    {
        // Arrange
        var token = "same-token-12345";

        // Act
        var hash1 = _hasher.HashToken(token);
        var hash2 = _hasher.HashToken(token);

        // Assert
        hash1.Should().Be(hash2);
    }

    [Fact]
    public void HashToken_WithDifferentTokens_ReturnsDifferentHashes()
    {
        // Arrange
        var token1 = "token-one";
        var token2 = "token-two";

        // Act
        var hash1 = _hasher.HashToken(token1);
        var hash2 = _hasher.HashToken(token2);

        // Assert
        hash1.Should().NotBe(hash2);
    }

    [Fact]
    public void HashToken_WithEmptyToken_ThrowsArgumentException()
    {
        // Act & Assert
        _hasher
            .Invoking(x => x.HashToken(string.Empty))
            .Should()
            .Throw<ArgumentException>()
            .WithMessage("*Token cannot be empty*");
    }

    [Fact]
    public void HashToken_WithNullToken_ThrowsArgumentException()
    {
        // Act & Assert
        _hasher
            .Invoking(x => x.HashToken(null!))
            .Should()
            .Throw<ArgumentException>();
    }

    [Fact]
    public void VerifyToken_WithMatchingToken_ReturnsTrue()
    {
        // Arrange
        var token = "verify-this-token";
        var hash = _hasher.HashToken(token);

        // Act
        var result = _hasher.VerifyToken(token, hash);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void VerifyToken_WithNonMatchingToken_ReturnsFalse()
    {
        // Arrange
        var originalToken = "original-token";
        var differentToken = "different-token";
        var hash = _hasher.HashToken(originalToken);

        // Act
        var result = _hasher.VerifyToken(differentToken, hash);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void VerifyToken_WithEmptyToken_ReturnsFalse()
    {
        // Arrange
        var hash = _hasher.HashToken("some-token");

        // Act
        var result = _hasher.VerifyToken(string.Empty, hash);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void VerifyToken_WithEmptyHash_ReturnsFalse()
    {
        // Act
        var result = _hasher.VerifyToken("some-token", string.Empty);

        // Assert
        result.Should().BeFalse();
    }
}
