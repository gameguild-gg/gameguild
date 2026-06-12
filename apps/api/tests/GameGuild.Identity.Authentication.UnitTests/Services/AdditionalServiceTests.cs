using FluentAssertions;
using GameGuild.Configuration.ApplicationLayer;
using GameGuild.Identity.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Nethereum.Signer;
using Xunit;

namespace GameGuild.Identity.Authentication.UnitTests.Services;

#region PermissionService Tests

public class PermissionServiceTests
{
    [Fact]
    public async Task TenantPermissions_CanGrantQueryRevokeAndJoin()
    {
        await using var db = CreateDbContext();
        var service = new PermissionService(db);
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        await service.SetTenantDefaultPermissionsAsync(tenantId, [PermissionType.Read]);
        await service.GrantTenantPermissionAsync(userId, tenantId, [PermissionType.Edit]);

        (await service.HasTenantPermissionAsync(userId, tenantId, PermissionType.Read)).Should().BeTrue();
        (await service.HasTenantPermissionAsync(userId, tenantId, PermissionType.Edit)).Should().BeTrue();
        (await service.IsUserInTenantAsync(userId, tenantId)).Should().BeTrue();
        (await service.GetUsersWithPermissionAsync(tenantId, PermissionType.Edit)).Should().Contain(userId);

        await service.RevokeTenantPermissionAsync(userId, tenantId, [PermissionType.Edit]);

        (await service.HasTenantPermissionAsync(userId, tenantId, PermissionType.Edit)).Should().BeFalse();
        (await service.JoinTenantAsync(Guid.NewGuid(), tenantId)).Permissions.Should().Contain(PermissionType.Read.ToString());
    }

    [Fact]
    public async Task ContentTypePermissions_ResolveTenantAndContentLayers()
    {
        await using var db = CreateDbContext();
        var service = new PermissionService(db);
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        await service.SetTenantDefaultPermissionsAsync(tenantId, [PermissionType.Read]);
        await service.GrantContentTypePermissionAsync(userId, tenantId, "Listing", [PermissionType.Publish]);

        var effective = (await service.GetEffectiveContentTypePermissionsAsync(userId, tenantId, "Listing")).ToArray();

        effective.Should().Contain(PermissionType.Read);
        effective.Should().Contain(PermissionType.Publish);
        (await service.GetPermissionSourceAsync(userId, tenantId, PermissionType.Publish, "Listing")).Should().Be("ContentType");
    }

    [Fact]
    public async Task ResourcePermissions_CanShareResolveAndRevoke()
    {
        await using var db = CreateDbContext();
        var service = new PermissionService(db);
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var resourceId = Guid.NewGuid();

        await service.ShareResourceAsync<GenericResourcePermission, EntityBase>(resourceId, userId, tenantId, [PermissionType.Read, PermissionType.Share]);

        (await service.HasResourcePermissionAsync<GenericResourcePermission, EntityBase>(userId, tenantId, resourceId, PermissionType.Share)).Should().BeTrue();
        (await service.GetResourcesWithPermissionAsync(userId, tenantId, PermissionType.Read, nameof(EntityBase))).Should().Contain(resourceId);

        await service.RevokeResourceAccessAsync<GenericResourcePermission, EntityBase>(userId, tenantId, resourceId);

        (await service.HasResourcePermissionAsync<GenericResourcePermission, EntityBase>(userId, tenantId, resourceId, PermissionType.Share)).Should().BeFalse();
    }

    private static PermissionServiceDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<PermissionServiceDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        return new PermissionServiceDbContext(options);
    }

    private sealed class PermissionServiceDbContext(DbContextOptions<PermissionServiceDbContext> options) : DbContext(options), IApplicationDbContext
    {
        public Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
            => Database.BeginTransactionAsync(cancellationToken);

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<TenantPermission>().Ignore(p => p.Metadata);
            modelBuilder.Entity<ContentTypePermission>();
            modelBuilder.Entity<GenericResourcePermission>();
        }
    }
}

#endregion

#region Web3Service Tests

/// <summary>
/// Tests for Web3Service — wallet validation, challenge generation, and signature verification.
/// </summary>
public class Web3ServiceTests
{
    private readonly Web3Service _service;
    private readonly IMemoryCache _memoryCache;

    public Web3ServiceTests()
    {
        _memoryCache = new MemoryCache(new MemoryCacheOptions());
        _service = new Web3Service(NullLogger<Web3Service>.Instance, _memoryCache);
    }

    [Theory]
    [InlineData("0x742d35Cc6634C0532925a3b844Bc9e7595f2bD28", true)]
    [InlineData("0xABCDEF1234567890abcdef1234567890ABCDEF12", true)]
    [InlineData("0x0000000000000000000000000000000000000000", true)]
    [InlineData("", false)]
    [InlineData(null, false)]
    [InlineData("0x123", false)] // too short
    [InlineData("742d35Cc6634C0532925a3b844Bc9e7595f2bD28", false)] // no 0x prefix
    [InlineData("0xGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGG", false)] // invalid hex
    [InlineData("0x742d35Cc6634C0532925a3b844Bc9e7595f2bD28FF", false)] // too long
    public void IsValidWalletAddress_ShouldValidateCorrectly(string? address, bool expected)
    {
        _service.IsValidWalletAddress(address!).Should().Be(expected);
    }

    [Fact]
    public async Task GenerateChallengeAsync_WithValidAddress_ShouldReturnChallenge()
    {
        var address = "0x742d35Cc6634C0532925a3b844Bc9e7595f2bD28";
        var challenge = await _service.GenerateChallengeAsync(address);

        challenge.Should().NotBeNull();
        challenge.WalletAddress.Should().Be(address);
        challenge.Message.Should().Contain("GameGuild");
        challenge.Message.Should().Contain(address);
        challenge.Nonce.Should().NotBeNullOrEmpty();
        challenge.ExpiresAt.Should().BeAfter(challenge.IssuedAt);
    }

    [Fact]
    public async Task GenerateChallengeAsync_WithTenantId_ShouldIncludeTenantId()
    {
        var tenantId = Guid.NewGuid();
        var challenge = await _service.GenerateChallengeAsync("0x742d35Cc6634C0532925a3b844Bc9e7595f2bD28", tenantId);

        challenge.TenantId.Should().Be(tenantId);
    }

    [Fact]
    public async Task GenerateChallengeAsync_WithInvalidAddress_ShouldThrowArgumentException()
    {
        await Assert.ThrowsAsync<ArgumentException>(
            () => _service.GenerateChallengeAsync("invalid-address"));
    }

    [Fact]
    public async Task VerifySignatureAsync_WithInvalidAddress_ShouldReturnFalse()
    {
        var result = await _service.VerifySignatureAsync("invalid-address", "0x" + new string('a', 130), "message");
        result.Should().BeFalse();
    }

    [Fact]
    public async Task VerifySignatureAsync_WithNoChallenge_ShouldReturnFalse()
    {
        var result = await _service.VerifySignatureAsync(
            "0x742d35Cc6634C0532925a3b844Bc9e7595f2bD28",
            "0x" + new string('a', 130),
            "message");
        result.Should().BeFalse();
    }

    [Fact]
    public async Task VerifySignatureAsync_WithMismatchedMessage_ShouldReturnFalse()
    {
        var address = "0x742d35Cc6634C0532925a3b844Bc9e7595f2bD28";
        var challenge = await _service.GenerateChallengeAsync(address);

        var result = await _service.VerifySignatureAsync(
            address,
            "0x" + new string('a', 130),
            "wrong message");
        result.Should().BeFalse();
    }

    [Fact]
    public async Task VerifySignatureAsync_WithSignedChallenge_ShouldReturnTrue()
    {
        var key = EthECKey.GenerateKey();
        var address = key.GetPublicAddress();
        var challenge = await _service.GenerateChallengeAsync(address);
        var signature = new EthereumMessageSigner().EncodeUTF8AndSign(challenge.Message, key);

        var result = await _service.VerifySignatureAsync(
            address,
            signature,
            challenge.Message);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task VerifySignatureAsync_WithDistributedCache_ShouldWorkAcrossServiceInstances()
    {
        var distributedCache = new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions()));
        using var firstMemoryCache = new MemoryCache(new MemoryCacheOptions());
        using var secondMemoryCache = new MemoryCache(new MemoryCacheOptions());
        var firstService = new Web3Service(NullLogger<Web3Service>.Instance, firstMemoryCache, distributedCache);
        var secondService = new Web3Service(NullLogger<Web3Service>.Instance, secondMemoryCache, distributedCache);
        var key = EthECKey.GenerateKey();
        var address = key.GetPublicAddress();

        var challenge = await firstService.GenerateChallengeAsync(address);
        var signature = new EthereumMessageSigner().EncodeUTF8AndSign(challenge.Message, key);

        var result = await secondService.VerifySignatureAsync(address, signature, challenge.Message);

        result.Should().BeTrue();
    }
}

#endregion

#region UserEnumerationProtectionService Tests

/// <summary>
/// Tests for UserEnumerationProtectionService — throttling, error messages, timing protection.
/// </summary>
public class UserEnumerationProtectionServiceTests
{
    private readonly UserEnumerationProtectionService _service;
    private readonly IMemoryCache _memoryCache;

    public UserEnumerationProtectionServiceTests()
    {
        _memoryCache = new MemoryCache(new MemoryCacheOptions());
        _service = new UserEnumerationProtectionService(
            NullLogger<UserEnumerationProtectionService>.Instance,
            _memoryCache);
    }

    [Theory]
    [InlineData("login", "Invalid credentials. Please check your email and password.")]
    [InlineData("password_reset", "If an account exists with that email, a password reset link has been sent.")]
    [InlineData("registration", "Unable to complete registration. Please try again.")]
    [InlineData("mfa", "Invalid authentication code. Please try again.")]
    [InlineData("unknown", "Authentication failed. Please try again.")]
    public void GetGenericErrorMessage_ShouldReturnCorrectMessage(string context, string expected)
    {
        _service.GetGenericErrorMessage(context).Should().Be(expected);
    }

    [Fact]
    public void GetConsistentErrorMessage_ShouldReturnConsistentString()
    {
        var message = _service.GetConsistentErrorMessage();
        message.Should().Be("Invalid credentials. Please check your email and password.");
    }

    [Fact]
    public void GetBaseProcessingTime_ShouldReturn400ms()
    {
        var time = _service.GetBaseProcessingTime();
        time.Should().Be(TimeSpan.FromMilliseconds(400));
    }

    [Fact]
    public async Task ShouldThrottleAsync_FirstAttempt_ShouldNotThrottle()
    {
        var result = await _service.ShouldThrottleAsync("test-user");
        result.ShouldThrottle.Should().BeFalse();
        result.DelayMs.Should().Be(0);
    }

    [Fact]
    public async Task RecordEnumerationAttemptAsync_ShouldIncrementCount()
    {
        var identifier = "test-user-" + Guid.NewGuid();

        await _service.RecordEnumerationAttemptAsync(identifier, "login");

        var result = await _service.ShouldThrottleAsync(identifier);
        result.AttemptCount.Should().BeGreaterOrEqualTo(1);
    }

    [Fact]
    public async Task ShouldThrottleAsync_AfterManyAttempts_ShouldThrottle()
    {
        var identifier = "brute-force-" + Guid.NewGuid();

        // Record 11 attempts (threshold is 10)
        for (int i = 0; i < 11; i++)
        {
            await _service.RecordEnumerationAttemptAsync(identifier, "login");
        }

        var result = await _service.ShouldThrottleAsync(identifier);
        result.ShouldThrottle.Should().BeTrue();
        result.DelayMs.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task ShouldThrottleAsync_WithDistributedCache_CountsAttemptsAcrossServiceInstances()
    {
        var distributedCache = new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions()));
        using var firstMemoryCache = new MemoryCache(new MemoryCacheOptions());
        using var secondMemoryCache = new MemoryCache(new MemoryCacheOptions());
        var firstService = new UserEnumerationProtectionService(
            NullLogger<UserEnumerationProtectionService>.Instance,
            firstMemoryCache,
            distributedCache);
        var secondService = new UserEnumerationProtectionService(
            NullLogger<UserEnumerationProtectionService>.Instance,
            secondMemoryCache,
            distributedCache);
        var identifier = "distributed-enum-" + Guid.NewGuid();

        for (var i = 0; i < 11; i++)
        {
            await firstService.RecordEnumerationAttemptAsync(identifier, "login");
        }

        var result = await secondService.ShouldThrottleAsync(identifier);

        result.ShouldThrottle.Should().BeTrue();
        result.AttemptCount.Should().Be(11);
    }
}

#endregion

#region SiemIntegrationService Tests

/// <summary>
/// Tests for SiemIntegrationService — disabled SIEM configuration path.
/// </summary>
public class SiemIntegrationServiceTests
{
    [Fact]
    public void IsEnabled_WhenDisabled_ShouldReturnFalse()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Authentication:Siem:Enabled"] = "false"
            })
            .Build();

        var service = new SiemIntegrationService(config, NullLogger<SiemIntegrationService>.Instance);
        service.IsEnabled().Should().BeFalse();
    }

    [Fact]
    public void IsEnabled_WhenEnabled_ShouldReturnTrue()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Authentication:Siem:Enabled"] = "true",
                ["Authentication:Siem:Endpoint"] = "https://siem.example.com"
            })
            .Build();

        var service = new SiemIntegrationService(config, NullLogger<SiemIntegrationService>.Instance);
        service.IsEnabled().Should().BeTrue();
    }

    [Fact]
    public async Task SendSecurityEventAsync_WhenDisabled_ShouldReturnImmediately()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Authentication:Siem:Enabled"] = "false"
            })
            .Build();

        var service = new SiemIntegrationService(config, NullLogger<SiemIntegrationService>.Instance);

        // Should not throw, just return
        await service.SendSecurityEventAsync(new SiemEvent
        {
            EventType = "Test",
            Severity = SiemSeverity.Low,
            Description = "Test event"
        });
    }

    [Fact]
    public async Task SendSecurityEventAsync_WhenEnabledButNoEndpoint_ShouldLogOnly()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Authentication:Siem:Enabled"] = "true"
            })
            .Build();

        var service = new SiemIntegrationService(config, NullLogger<SiemIntegrationService>.Instance);

        // Should not throw — logs the event but doesn't send to endpoint
        await service.SendSecurityEventAsync(new SiemEvent
        {
            EventType = "TestEvent",
            Severity = SiemSeverity.High,
            Description = "Test event description",
            UserId = Guid.NewGuid(),
            IpAddress = "127.0.0.1"
        });
    }

    [Fact]
    public async Task SendAnomalyEventAsync_WhenDisabled_ShouldNotThrow()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Authentication:Siem:Enabled"] = "false"
            })
            .Build();

        var service = new SiemIntegrationService(config, NullLogger<SiemIntegrationService>.Instance);

        var attempt = new AuthenticationAttempt
        {
            Email = "test@test.com",
            IpAddress = "1.2.3.4",
            UserAgent = "Test",
            IsSuccessful = false
        };

        var analysis = new AuthenticationAttemptAnalysis { RiskScore = 80 };

        await service.SendAnomalyEventAsync(attempt, analysis);
    }
}

#endregion

#region JwtTokenService Additional Tests

/// <summary>
/// Additional JwtTokenService tests covering untested methods.
/// </summary>
public class JwtTokenServiceAdditionalTests
{
    private readonly JwtTokenService _service;
    private readonly Mock<IRefreshTokenRepository> _refreshTokenRepositoryMock;
    private readonly Mock<IRefreshTokenHasher> _refreshTokenHasherMock;
    private readonly JwtOptions _jwtOptions;

    public JwtTokenServiceAdditionalTests()
    {
        _jwtOptions = new JwtOptions
        {
            SecretKey = "ThisIsAVerySecureSecretKeyForTestingPurposesOnly12345",
            Issuer = "TestIssuer",
            Audience = "TestAudience",
            AccessTokenExpirationMinutes = 15,
            RefreshTokenExpirationDays = 7,
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ClockSkewSeconds = 30
        };

        _refreshTokenRepositoryMock = new Mock<IRefreshTokenRepository>();
        _refreshTokenHasherMock = new Mock<IRefreshTokenHasher>();

        var optionsMock = new Mock<IOptions<JwtOptions>>();
        optionsMock.Setup(x => x.Value).Returns(_jwtOptions);

        _service = new JwtTokenService(
            NullLogger<JwtTokenService>.Instance,
            _refreshTokenRepositoryMock.Object,
            _refreshTokenHasherMock.Object,
            new Mock<IHttpContextAccessor>().Object,
            optionsMock.Object);
    }

    // Sync wrapper tests

    [Fact]
    public void GenerateAccessToken_Sync_ShouldReturnToken()
    {
        var token = _service.GenerateAccessToken(Guid.NewGuid(), "test@test.com", new[] { "User" });
        token.Should().NotBeNullOrEmpty();
        token.Split('.').Should().HaveCount(3);
    }

    [Fact]
    public void GenerateAccessToken_SyncWithAdditionalClaims_ShouldReturnToken()
    {
        var token = _service.GenerateAccessToken(Guid.NewGuid(), "test@test.com",
            new[] { "User" }, Enumerable.Empty<System.Security.Claims.Claim>());
        token.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void GenerateRefreshToken_Sync_ShouldThrowNotSupportedException()
    {
        Assert.Throws<NotSupportedException>(() => _service.GenerateRefreshToken());
    }

    [Fact]
    public void GetPrincipalFromExpiredToken_ShouldReturnPrincipal()
    {
        var userId = Guid.NewGuid();
        var token = _service.GenerateAccessToken(userId, "test@test.com", new[] { "User" });

        var principal = _service.GetPrincipalFromExpiredToken(token);

        principal.Identity?.IsAuthenticated.Should().BeTrue();
        principal.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value.Should().Be(userId.ToString());
        principal.IsInRole("User").Should().BeTrue();
    }

    [Fact]
    public void ValidateToken_Sync_ShouldReturnPrincipal()
    {
        var userId = Guid.NewGuid();
        var token = _service.GenerateAccessToken(userId, "test@test.com", new[] { "User" });

        var principal = _service.ValidateToken(token);

        principal.Identity?.IsAuthenticated.Should().BeTrue();
        principal.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value.Should().Be(userId.ToString());
        principal.IsInRole("User").Should().BeTrue();
    }

    // ValidateTokenAsync tests

    [Fact]
    public async Task ValidateTokenAsync_WithValidToken_ShouldReturnTrue()
    {
        var token = await _service.GenerateAccessTokenAsync(
            Guid.NewGuid(), "test@test.com", new[] { "User" }, null, 1, CancellationToken.None);

        var result = await _service.ValidateTokenAsync(token);
        result.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateTokenAsync_WithInvalidToken_ShouldReturnFalse()
    {
        var result = await _service.ValidateTokenAsync("invalid.token.string");
        result.Should().BeFalse();
    }

    [Fact]
    public async Task ValidateTokenAsync_WithTamperedToken_ShouldReturnFalse()
    {
        var token = await _service.GenerateAccessTokenAsync(
            Guid.NewGuid(), "test@test.com", new[] { "User" }, null, 1, CancellationToken.None);

        var parts = token.Split('.');
        parts[2] = "tampered_signature"; // Tamper with signature
        var tamperedToken = string.Join('.', parts);

        var result = await _service.ValidateTokenAsync(tamperedToken);
        result.Should().BeFalse();
    }

    // GetTokenPayloadAsync tests

    [Fact]
    public async Task GetTokenPayloadAsync_WithValidToken_ShouldReturnPayload()
    {
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var token = await _service.GenerateAccessTokenAsync(
            userId, "test@test.com", new[] { "Admin" }, tenantId, 1, CancellationToken.None);

        var payload = await _service.GetTokenPayloadAsync(token);

        payload.Should().NotBeNull();
        payload!.UserId.Should().Be(userId);
        payload.Email.Should().Be("test@test.com");
        payload.Roles.Should().Contain("Admin");
        payload.TenantId.Should().Be(tenantId);
    }

    [Fact]
    public async Task GetTokenPayloadAsync_WithInvalidFormat_ShouldReturnNull()
    {
        var result = await _service.GetTokenPayloadAsync("not-a-jwt");
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetTokenPayloadAsync_WithoutTenantId_ShouldReturnNullTenant()
    {
        var token = await _service.GenerateAccessTokenAsync(
            Guid.NewGuid(), "test@test.com", new[] { "User" }, null, 1, CancellationToken.None);

        var payload = await _service.GetTokenPayloadAsync(token);

        payload.Should().NotBeNull();
        payload!.TenantId.Should().BeNull();
    }

    // RevokeRefreshTokenAsync tests

    [Fact]
    public async Task RevokeRefreshTokenAsync_WhenTokenNotFound_ShouldReturnFalse()
    {
        _refreshTokenHasherMock.Setup(x => x.HashToken(It.IsAny<string>())).Returns("hashed");
        _refreshTokenRepositoryMock.Setup(x => x.GetByTokenAsync("hashed", It.IsAny<CancellationToken>()))
            .ReturnsAsync((RefreshToken?)null);

        var result = await _service.RevokeRefreshTokenAsync("some-token");
        result.Should().BeFalse();
    }

    [Fact]
    public async Task RevokeRefreshTokenAsync_WhenAlreadyRevoked_ShouldReturnTrue()
    {
        var existingToken = new RefreshToken { Id = Guid.NewGuid(), IsRevoked = true };
        _refreshTokenHasherMock.Setup(x => x.HashToken(It.IsAny<string>())).Returns("hashed");
        _refreshTokenRepositoryMock.Setup(x => x.GetByTokenAsync("hashed", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingToken);

        var result = await _service.RevokeRefreshTokenAsync("some-token");
        result.Should().BeTrue();
    }

    [Fact]
    public async Task RevokeRefreshTokenAsync_WhenActive_ShouldRevokeAndReturnTrue()
    {
        var existingToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            IsRevoked = false,
            UserId = Guid.NewGuid(),
            Token = "hashed"
        };
        _refreshTokenHasherMock.Setup(x => x.HashToken(It.IsAny<string>())).Returns("hashed");
        _refreshTokenRepositoryMock.Setup(x => x.GetByTokenAsync("hashed", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingToken);
        _refreshTokenRepositoryMock.Setup(x => x.UpdateAsync(It.IsAny<RefreshToken>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingToken);

        var result = await _service.RevokeRefreshTokenAsync("some-token");

        result.Should().BeTrue();
        existingToken.IsRevoked.Should().BeTrue();
        existingToken.RevokedAt.Should().NotBeNull();
    }

    // GenerateServiceAccountTokenAsync tests

    [Fact]
    public async Task GenerateServiceAccountTokenAsync_ShouldReturnTokenAndExpiry()
    {
        var scopes = new HashSet<string> { "read", "write" };

        var (token, expiresAt) = await _service.GenerateServiceAccountTokenAsync(
            "sa-123", "client-1", "GameService", scopes, null);

        token.Should().NotBeNullOrEmpty();
        token.Split('.').Should().HaveCount(3);
        expiresAt.Should().BeAfter(DateTime.UtcNow);
    }

    [Fact]
    public async Task GenerateServiceAccountTokenAsync_WithTenant_ShouldIncludeTenantClaim()
    {
        var tenantId = Guid.NewGuid();
        var scopes = new HashSet<string> { "admin" };

        var (token, _) = await _service.GenerateServiceAccountTokenAsync(
            "sa-456", "client-2", "AdminService", scopes, tenantId);

        var payload = await _service.GetTokenPayloadAsync(token);
        payload.Should().NotBeNull();
        payload!.TenantId.Should().Be(tenantId);
    }
}

#endregion
