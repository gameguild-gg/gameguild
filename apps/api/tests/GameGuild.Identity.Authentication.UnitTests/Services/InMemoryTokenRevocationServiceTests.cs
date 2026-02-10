using FluentAssertions;
using GameGuild.Identity.Authentication;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace GameGuild.Identity.Authentication.UnitTests.Services;

public class InMemoryTokenRevocationServiceTests
{
    private readonly InMemoryTokenRevocationService _sut;

    public InMemoryTokenRevocationServiceTests()
    {
        _sut = new InMemoryTokenRevocationService(
            NullLogger<InMemoryTokenRevocationService>.Instance
        );
    }

    // ── RevokeTokenAsync / IsRevokedAsync ─────────────────────

    [Fact]
    public async Task RevokeTokenAsync_TokenBecomesRevoked()
    {
        await _sut.RevokeTokenAsync("jti-123", DateTime.UtcNow.AddHours(1));
        var isRevoked = await _sut.IsRevokedAsync("jti-123");
        isRevoked.Should().BeTrue();
    }

    [Fact]
    public async Task IsRevokedAsync_UnknownToken_ReturnsFalse()
    {
        var isRevoked = await _sut.IsRevokedAsync("unknown-jti");
        isRevoked.Should().BeFalse();
    }

    [Fact]
    public async Task RevokeTokenAsync_WithReason_StillRevoked()
    {
        await _sut.RevokeTokenAsync("jti-456", DateTime.UtcNow.AddHours(1), "suspicious activity");
        var isRevoked = await _sut.IsRevokedAsync("jti-456");
        isRevoked.Should().BeTrue();
    }

    // ── RevokeAllUserTokensAsync / IsUserTokenRevokedAsync ────

    [Fact]
    public async Task RevokeAllUserTokensAsync_OldTokensBecomRevoked()
    {
        var userId = Guid.NewGuid();
        var tokenIssuedAt = DateTime.UtcNow.AddMinutes(-5);

        await _sut.RevokeAllUserTokensAsync(userId);
        var isRevoked = await _sut.IsUserTokenRevokedAsync(userId, tokenIssuedAt);

        isRevoked.Should().BeTrue();
    }

    [Fact]
    public async Task IsUserTokenRevokedAsync_TokenIssuedAfterRevocation_ReturnsFalse()
    {
        var userId = Guid.NewGuid();
        await _sut.RevokeAllUserTokensAsync(userId);

        // Token issued after revocation
        var tokenIssuedAt = DateTime.UtcNow.AddSeconds(1);
        var isRevoked = await _sut.IsUserTokenRevokedAsync(userId, tokenIssuedAt);

        isRevoked.Should().BeFalse();
    }

    [Fact]
    public async Task IsUserTokenRevokedAsync_NoRevocation_ReturnsFalse()
    {
        var isRevoked = await _sut.IsUserTokenRevokedAsync(Guid.NewGuid(), DateTime.UtcNow);
        isRevoked.Should().BeFalse();
    }

    // ── CleanupExpiredAsync ───────────────────────────────────

    [Fact]
    public async Task CleanupExpiredAsync_RemovesExpiredTokens()
    {
        // Add an already-expired token
        await _sut.RevokeTokenAsync("expired-jti", DateTime.UtcNow.AddHours(-2));
        // Add a valid token
        await _sut.RevokeTokenAsync("valid-jti", DateTime.UtcNow.AddHours(2));

        var cleaned = await _sut.CleanupExpiredAsync();

        cleaned.Should().BeGreaterOrEqualTo(1);
        // Expired token should be cleaned up
        var expiredRevoked = await _sut.IsRevokedAsync("expired-jti");
        expiredRevoked.Should().BeFalse();
        // Valid token remains
        var validRevoked = await _sut.IsRevokedAsync("valid-jti");
        validRevoked.Should().BeTrue();
    }

    [Fact]
    public async Task CleanupExpiredAsync_NoExpiredTokens_ReturnsZero()
    {
        await _sut.RevokeTokenAsync("valid-jti", DateTime.UtcNow.AddHours(2));
        var cleaned = await _sut.CleanupExpiredAsync();
        cleaned.Should().Be(0);
    }
}
