using FluentAssertions;
using GameGuild.Identity.Authentication;
using Xunit;

namespace GameGuild.Identity.Authentication.UnitTests.Entities;

public class ServiceAccountTests
{
    private static ServiceAccount CreateAccount(
        bool isActive = true,
        bool isLocked = false,
        DateTime? expiresAt = null,
        string scopes = "read,write,admin")
    {
        return new ServiceAccount
        {
            Id = Guid.NewGuid(),
            ClientId = "test-client",
            ClientSecretHash = "hash123",
            Name = "Test Service",
            IsActive = isActive,
            IsLocked = isLocked,
            ExpiresAt = expiresAt,
            Scopes = scopes
        };
    }

    // ── CanAuthenticate ───────────────────────────────────────

    [Fact]
    public void CanAuthenticate_ActiveAndNotLocked_ReturnsTrue()
    {
        var account = CreateAccount();
        account.CanAuthenticate.Should().BeTrue();
    }

    [Fact]
    public void CanAuthenticate_Inactive_ReturnsFalse()
    {
        var account = CreateAccount(isActive: false);
        account.CanAuthenticate.Should().BeFalse();
    }

    [Fact]
    public void CanAuthenticate_Locked_ReturnsFalse()
    {
        var account = CreateAccount(isLocked: true);
        account.CanAuthenticate.Should().BeFalse();
    }

    [Fact]
    public void CanAuthenticate_Expired_ReturnsFalse()
    {
        var account = CreateAccount(expiresAt: DateTime.UtcNow.AddHours(-1));
        account.CanAuthenticate.Should().BeFalse();
    }

    [Fact]
    public void CanAuthenticate_FutureExpiry_ReturnsTrue()
    {
        var account = CreateAccount(expiresAt: DateTime.UtcNow.AddDays(30));
        account.CanAuthenticate.Should().BeTrue();
    }

    [Fact]
    public void CanAuthenticate_NullExpiry_ReturnsTrue()
    {
        var account = CreateAccount(expiresAt: null);
        account.CanAuthenticate.Should().BeTrue();
    }

    // ── GetScopesSet ──────────────────────────────────────────

    [Fact]
    public void GetScopesSet_ReturnsCommaSplitScopes()
    {
        var account = CreateAccount(scopes: "read,write,admin");
        var scopes = account.GetScopesSet();
        scopes.Should().Contain("read").And.Contain("write").And.Contain("admin");
        scopes.Should().HaveCount(3);
    }

    [Fact]
    public void GetScopesSet_EmptyScopes_ReturnsEmptyOrSingleEmpty()
    {
        var account = CreateAccount(scopes: "");
        var scopes = account.GetScopesSet();
        scopes.Should().NotBeNull();
    }

    [Fact]
    public void GetScopesSet_SingleScope_ReturnsSingleItem()
    {
        var account = CreateAccount(scopes: "admin");
        var scopes = account.GetScopesSet();
        scopes.Should().Contain("admin");
    }

    // ── RecordSuccessfulAuthentication ─────────────────────────

    [Fact]
    public void RecordSuccessfulAuthentication_UpdatesLastAuthenticatedAt()
    {
        var account = CreateAccount();
        var before = account.LastAuthenticatedAt;
        account.RecordSuccessfulAuthentication("10.0.0.1");

        account.LastAuthenticatedAt.Should().NotBeNull();
        account.LastAuthenticatedFromIp.Should().Be("10.0.0.1");
    }

    [Fact]
    public void RecordSuccessfulAuthentication_IncrementsAuthCount()
    {
        var account = CreateAccount();
        account.AuthenticationCount = 5;
        account.RecordSuccessfulAuthentication("10.0.0.1");
        account.AuthenticationCount.Should().Be(6);
    }

    [Fact]
    public void RecordSuccessfulAuthentication_ResetsFailedAttempts()
    {
        var account = CreateAccount();
        account.FailedAuthenticationAttempts = 3;
        account.RecordSuccessfulAuthentication("10.0.0.1");
        account.FailedAuthenticationAttempts.Should().Be(0);
    }

    // ── RecordFailedAuthentication ────────────────────────────

    [Fact]
    public void RecordFailedAuthentication_IncrementsFailureCount()
    {
        var account = CreateAccount();
        account.RecordFailedAuthentication();
        account.FailedAuthenticationAttempts.Should().Be(1);
    }

    [Fact]
    public void RecordFailedAuthentication_LocksWhenThresholdReached()
    {
        var account = CreateAccount();
        account.FailedAuthenticationAttempts = 9;
        account.RecordFailedAuthentication(lockThreshold: 10);

        account.IsLocked.Should().BeTrue();
        account.LockedAt.Should().NotBeNull();
    }

    [Fact]
    public void RecordFailedAuthentication_DoesNotLockBelowThreshold()
    {
        var account = CreateAccount();
        account.FailedAuthenticationAttempts = 5;
        account.RecordFailedAuthentication(lockThreshold: 10);

        account.IsLocked.Should().BeFalse();
    }

    // ── Lock / Unlock ─────────────────────────────────────────

    [Fact]
    public void Lock_SetsLockedState()
    {
        var account = CreateAccount();
        account.Lock("Suspicious activity");

        account.IsLocked.Should().BeTrue();
        account.LockedAt.Should().NotBeNull();
    }

    [Fact]
    public void Unlock_ClearsLockedState()
    {
        var account = CreateAccount(isLocked: true);
        account.LockedAt = DateTime.UtcNow;
        account.FailedAuthenticationAttempts = 10;

        account.Unlock();

        account.IsLocked.Should().BeFalse();
        account.LockedAt.Should().BeNull();
        account.FailedAuthenticationAttempts.Should().Be(0);
    }

    // ── RotateSecret ──────────────────────────────────────────

    [Fact]
    public void RotateSecret_UpdatesHash()
    {
        var account = CreateAccount();
        var oldHash = account.ClientSecretHash;
        account.RotateSecret("new-secret-hash");

        account.ClientSecretHash.Should().Be("new-secret-hash");
        account.SecretRotatedAt.Should().NotBeNull();
    }

    [Fact]
    public void RotateSecret_IncrementsRotationCount()
    {
        var account = CreateAccount();
        account.SecretRotationCount = 2;
        account.RotateSecret("new-hash");
        account.SecretRotationCount.Should().Be(3);
    }
}
