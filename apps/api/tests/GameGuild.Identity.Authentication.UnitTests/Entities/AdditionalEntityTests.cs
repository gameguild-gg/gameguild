using FluentAssertions;
using Xunit;

namespace GameGuild.Identity.Authentication.UnitTests.Entities;

public class ApiKeyTests
{
    private ApiKey CreateTestApiKey(
        string name = "Test",
        string scopes = "read,write",
        bool isActive = true,
        DateTime? expiresAt = null)
    {
        return new ApiKey
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Name = name,
            KeyHash = "abcdef1234567890abcdef1234567890abcdef1234567890abcdef1234567890ab",
            KeyPrefix = "gg_live_",
            Scopes = scopes,
            IsActive = isActive,
            ExpiresAt = expiresAt
        };
    }

    [Fact]
    public void IsValid_WhenActiveAndNotExpired_ShouldReturnTrue()
    {
        var key = CreateTestApiKey(expiresAt: DateTime.UtcNow.AddDays(30));

        key.IsValid().Should().BeTrue();
    }

    [Fact]
    public void IsValid_WhenInactive_ShouldReturnFalse()
    {
        var key = CreateTestApiKey(isActive: false);

        key.IsValid().Should().BeFalse();
    }

    [Fact]
    public void IsValid_WhenRevoked_ShouldReturnFalse()
    {
        var key = CreateTestApiKey();
        key.Revoke("security concern");

        key.IsValid().Should().BeFalse();
    }

    [Fact]
    public void IsValid_WhenExpired_ShouldReturnFalse()
    {
        var key = CreateTestApiKey(expiresAt: DateTime.UtcNow.AddDays(-1));

        key.IsValid().Should().BeFalse();
    }

    [Fact]
    public void IsValid_WithNoExpiration_ShouldReturnTrue()
    {
        var key = CreateTestApiKey(expiresAt: null);

        key.IsValid().Should().BeTrue();
    }

    [Fact]
    public void HasScope_WithExactMatch_ShouldReturnTrue()
    {
        var key = CreateTestApiKey(scopes: "read,write");

        key.HasScope("read").Should().BeTrue();
        key.HasScope("write").Should().BeTrue();
    }

    [Fact]
    public void HasScope_CaseInsensitive_ShouldMatch()
    {
        var key = CreateTestApiKey(scopes: "Read");

        key.HasScope("read").Should().BeTrue();
        key.HasScope("READ").Should().BeTrue();
    }

    [Fact]
    public void HasScope_WithWildcard_ShouldMatchAll()
    {
        var key = CreateTestApiKey(scopes: "*");

        key.HasScope("anything").Should().BeTrue();
        key.HasScope("admin").Should().BeTrue();
    }

    [Fact]
    public void HasScope_WhenMissing_ShouldReturnFalse()
    {
        var key = CreateTestApiKey(scopes: "read");

        key.HasScope("admin").Should().BeFalse();
    }

    [Fact]
    public void RecordUsage_ShouldIncrementCountAndSetLastUsed()
    {
        var key = CreateTestApiKey();
        key.UsageCount.Should().Be(0);

        key.RecordUsage();
        key.UsageCount.Should().Be(1);
        key.LastUsedAt.Should().NotBeNull();

        key.RecordUsage();
        key.UsageCount.Should().Be(2);
    }

    [Fact]
    public void Revoke_ShouldDeactivateAndSetReason()
    {
        var key = CreateTestApiKey();

        key.Revoke("compromised");

        key.IsActive.Should().BeFalse();
        key.RevokedAt.Should().NotBeNull();
        key.RevocationReason.Should().Be("compromised");
    }

    [Fact]
    public void GetScopes_ShouldReturnScopeArray()
    {
        var key = CreateTestApiKey(scopes: "read,write,admin");

        key.GetScopes().Should().BeEquivalentTo(new[] { "read", "write", "admin" });
    }

    [Fact]
    public void GetScopes_WithEmptyScopes_ShouldReturnEmpty()
    {
        var key = CreateTestApiKey(scopes: "");

        key.GetScopes().Should().BeEmpty();
    }

    [Fact]
    public void ValidateKey_WithMatchingHash_ShouldReturnTrue()
    {
        // Manually compute a hash using the same algo ApiKey uses
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var plaintext = "gg_live_testkey12345";
        var bytes = System.Text.Encoding.UTF8.GetBytes(plaintext);
        var hash = sha256.ComputeHash(bytes);
        var hashString = Convert.ToHexString(hash).ToLowerInvariant();

        var key = new ApiKey { KeyHash = hashString };

        key.ValidateKey(plaintext).Should().BeTrue();
    }

    [Fact]
    public void ValidateKey_WithWrongPlaintext_ShouldReturnFalse()
    {
        var key = CreateTestApiKey();

        key.ValidateKey("wrong_key").Should().BeFalse();
    }
}

public class RefreshTokenHasherTests
{
    private readonly RefreshTokenHasher _hasher = new();

    [Fact]
    public void HashToken_ShouldReturnConsistentHash()
    {
        var hash1 = _hasher.HashToken("test-token");
        var hash2 = _hasher.HashToken("test-token");

        hash1.Should().Be(hash2);
    }

    [Fact]
    public void HashToken_DifferentTokens_ShouldProduceDifferentHashes()
    {
        var hash1 = _hasher.HashToken("token-a");
        var hash2 = _hasher.HashToken("token-b");

        hash1.Should().NotBe(hash2);
    }

    [Fact]
    public void HashToken_WithNullOrWhitespace_ShouldThrow()
    {
        FluentActions.Invoking(() => _hasher.HashToken(null!)).Should().Throw<ArgumentException>();
        FluentActions.Invoking(() => _hasher.HashToken("")).Should().Throw<ArgumentException>();
        FluentActions.Invoking(() => _hasher.HashToken("   ")).Should().Throw<ArgumentException>();
    }

    [Fact]
    public void VerifyToken_WithCorrectToken_ShouldReturnTrue()
    {
        var hash = _hasher.HashToken("my-refresh-token");

        _hasher.VerifyToken("my-refresh-token", hash).Should().BeTrue();
    }

    [Fact]
    public void VerifyToken_WithWrongToken_ShouldReturnFalse()
    {
        var hash = _hasher.HashToken("correct-token");

        _hasher.VerifyToken("wrong-token", hash).Should().BeFalse();
    }

    [Fact]
    public void VerifyToken_WithNullOrEmpty_ShouldReturnFalse()
    {
        _hasher.VerifyToken(null!, "hash").Should().BeFalse();
        _hasher.VerifyToken("", "hash").Should().BeFalse();
        _hasher.VerifyToken("token", null!).Should().BeFalse();
        _hasher.VerifyToken("token", "").Should().BeFalse();
        _hasher.VerifyToken("   ", "hash").Should().BeFalse();
        _hasher.VerifyToken("token", "   ").Should().BeFalse();
    }
}

public class TrustedDeviceTests
{
    [Fact]
    public void IsExpired_WithNoExpiresAt_ShouldReturnFalse()
    {
        var device = new TrustedDevice { ExpiresAt = null };

        device.IsExpired.Should().BeFalse();
    }

    [Fact]
    public void IsExpired_WhenExpired_ShouldReturnTrue()
    {
        var device = new TrustedDevice
        {
            ExpiresAt = DateTime.UtcNow.AddDays(-1)
        };

        device.IsExpired.Should().BeTrue();
    }

    [Fact]
    public void IsExpired_WhenNotExpired_ShouldReturnFalse()
    {
        var device = new TrustedDevice
        {
            ExpiresAt = DateTime.UtcNow.AddDays(30)
        };

        device.IsExpired.Should().BeFalse();
    }

    [Fact]
    public void IsValid_WhenActiveAndNotExpired_ShouldReturnTrue()
    {
        var device = new TrustedDevice
        {
            IsActive = true,
            ExpiresAt = null
        };

        device.IsValid.Should().BeTrue();
    }

    [Fact]
    public void IsValid_WhenInactive_ShouldReturnFalse()
    {
        var device = new TrustedDevice
        {
            IsActive = false,
            ExpiresAt = null
        };

        device.IsValid.Should().BeFalse();
    }

    [Fact]
    public void IsValid_WhenExpired_ShouldReturnFalse()
    {
        var device = new TrustedDevice
        {
            IsActive = true,
            ExpiresAt = DateTime.UtcNow.AddDays(-1)
        };

        device.IsValid.Should().BeFalse();
    }
}

public class RoleTests
{
    [Fact]
    public void DefaultConstructor_ShouldSetDefaults()
    {
        var role = new Role();

        role.Name.Should().BeEmpty();
        role.Description.Should().BeEmpty();
        role.Permissions.Should().Be("[]");
    }

    [Fact]
    public void Constructor_ShouldSetProperties()
    {
        var tenantId = Guid.NewGuid();
        var role = new Role("Admin", "Administrator role", tenantId);

        role.Name.Should().Be("Admin");
        role.Description.Should().Be("Administrator role");
        role.TenantId.Should().Be(tenantId);
        role.IsActive.Should().BeTrue();
        role.Permissions.Should().Be("[]");
    }

    [Fact]
    public void IsGlobalRole_WhenNoTenant_ShouldReturnTrue()
    {
        var role = new Role("GlobalAdmin", "Global admin");

        role.IsGlobalRole().Should().BeTrue();
        role.IsTenantRole().Should().BeFalse();
    }

    [Fact]
    public void IsTenantRole_WhenTenantSet_ShouldReturnTrue()
    {
        var role = new Role("TenantAdmin", "Tenant admin", Guid.NewGuid());

        role.IsTenantRole().Should().BeTrue();
        role.IsGlobalRole().Should().BeFalse();
    }
}

public class UserRoleTests
{
    [Fact]
    public void DefaultConstructor_ShouldCreateInstance()
    {
        var ur = new UserRole();

        ur.UserId.Should().Be(Guid.Empty);
        ur.RoleId.Should().Be(Guid.Empty);
    }

    [Fact]
    public void Constructor_ShouldSetProperties()
    {
        var userId = Guid.NewGuid();
        var roleId = Guid.NewGuid();
        var assignedBy = Guid.NewGuid();

        var ur = new UserRole(userId, roleId, assignedBy);

        ur.UserId.Should().Be(userId);
        ur.RoleId.Should().Be(roleId);
        ur.AssignedBy.Should().Be(assignedBy);
        ur.AssignedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void IsExpired_WhenNotExpired_ShouldReturnFalse()
    {
        var ur = new UserRole { ExpiresAt = DateTime.UtcNow.AddDays(30) };

        ur.IsExpired().Should().BeFalse();
    }

    [Fact]
    public void IsExpired_WhenExpired_ShouldReturnTrue()
    {
        var ur = new UserRole
        {
            ExpiresAt = DateTime.UtcNow.AddDays(-1)
        };

        ur.IsExpired().Should().BeTrue();
    }

    [Fact]
    public void IsExpired_WithNoExpiration_ShouldReturnFalse()
    {
        var ur = new UserRole { ExpiresAt = null };

        ur.IsExpired().Should().BeFalse();
    }

    [Fact]
    public void IsPermanent_WhenNoExpiration_ShouldReturnTrue()
    {
        var ur = new UserRole { ExpiresAt = null };

        ur.IsPermanent().Should().BeTrue();
    }

    [Fact]
    public void IsPermanent_WhenHasExpiration_ShouldReturnFalse()
    {
        var ur = new UserRole { ExpiresAt = DateTime.UtcNow.AddDays(30) };

        ur.IsPermanent().Should().BeFalse();
    }
}
