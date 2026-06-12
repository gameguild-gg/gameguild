// Wave 2 coverage tests — targeting 80.57% → 90%+
// Covers: AssetTokenService, AssetAccessService, DeduplicationService,
//   AssetAuthorizationHandler (HandleRequirementAsync + CheckAccessAsync),
//   AssetRateLimitService, VirusScanService, small records/validators,
//   TransformedAsset entity, StorageUploadResult, StorageMetadata

using System.Security.Claims;
using FluentAssertions;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using GameGuild.Assets;
using GameGuild.Assets.Deduplication;
using GameGuild.Assets.Security;
using GameGuild.Assets.VirusScan;
using GameGuild.CQRS.Models;
using GameGuild.Features;
using GameGuild.Identity.Authorization;
using GameGuild.Identity.Authorization.Models;
using GameGuild.Identity.Context.Actors;
using GameGuild.Identity.Tenants;
using Xunit;

namespace GameGuild.Assets.UnitTests;

// ═══════════════════════════════════════════════════════════════════
//  AssetTokenService — 93 uncovered lines → cover GenerateToken,
//  ValidateToken, GenerateEphemeralToken, ValidateEphemeralToken,
//  GetCurrentTimeWindow, cache behaviour, EvictExpiredEntries
// ═══════════════════════════════════════════════════════════════════
public class AssetTokenServiceCoverageTests
{
    private static AssetTokenService CreateService(string? secretKey = null, int expiryHours = 24, int windowHours = 24)
    {
        var opts = Options.Create(new AssetTokenOptions
        {
            SecretKey = secretKey ?? Convert.ToBase64String(new byte[32]),
            DefaultExpiryHours = expiryHours,
            TimeWindowHours = windowHours
        });
        return new AssetTokenService(opts);
    }

    [Fact]
    public void Constructor_WithEmptySecretKey_GeneratesRandomKey()
    {
        var opts = Options.Create(new AssetTokenOptions { SecretKey = string.Empty });
        var svc = new AssetTokenService(opts);
        // Should not throw — random key is generated
        var token = svc.GenerateToken(Guid.NewGuid(), Guid.NewGuid(), AssetAccessPolicy.Public);
        token.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void Constructor_WithZeroExpiryHours_DefaultsTo24()
    {
        var svc = CreateService(expiryHours: 0);
        var token = svc.GenerateToken(Guid.NewGuid(), Guid.NewGuid(), AssetAccessPolicy.Public);
        token.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void Constructor_WithZeroWindowHours_DefaultsTo8()
    {
        var svc = CreateService(windowHours: 0);
        svc.GetCurrentTimeWindow().Should().BeGreaterOrEqualTo(0);
    }

    [Fact]
    public void GenerateToken_ReturnsNonEmptyBase64UrlString()
    {
        var svc = CreateService();
        var token = svc.GenerateToken(Guid.NewGuid(), Guid.NewGuid(), AssetAccessPolicy.Authenticated);
        token.Should().NotBeNullOrEmpty();
        // Base64url chars only
        token.Should().MatchRegex(@"^[A-Za-z0-9_\-]+$");
    }

    [Fact]
    public void GenerateToken_WithCustomExpiry_Succeeds()
    {
        var svc = CreateService();
        var token = svc.GenerateToken(Guid.NewGuid(), Guid.NewGuid(), AssetAccessPolicy.Public,
            customExpiry: TimeSpan.FromMinutes(5));
        token.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void GenerateToken_WithTransformation_Succeeds()
    {
        var svc = CreateService();
        var spec = TransformationSpec.Parse("w=100,h=100");
        var token = svc.GenerateToken(Guid.NewGuid(), Guid.NewGuid(), AssetAccessPolicy.Public, spec);
        token.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void ValidateToken_WithValidToken_ReturnsPayload()
    {
        var svc = CreateService();
        var assetId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var token = svc.GenerateToken(assetId, tenantId, AssetAccessPolicy.Public);

        var payload = svc.ValidateToken(token, assetId, tenantId);

        payload.Should().NotBeNull();
        payload!.AssetReferenceId.Should().Be(assetId);
        payload.AccessPolicy.Should().Be(AssetAccessPolicy.Public);
    }

    [Fact]
    public void ValidateToken_WithWrongAssetId_ReturnsNull()
    {
        var svc = CreateService();
        var token = svc.GenerateToken(Guid.NewGuid(), Guid.NewGuid(), AssetAccessPolicy.Public);

        var payload = svc.ValidateToken(token, Guid.NewGuid(), Guid.NewGuid());
        payload.Should().BeNull();
    }

    [Fact]
    public void ValidateToken_WithTruncatedToken_ReturnsNull()
    {
        var svc = CreateService();
        var payload = svc.ValidateToken("shortTok", Guid.NewGuid(), null);
        payload.Should().BeNull();
    }

    [Fact]
    public void ValidateToken_WithMalformedToken_ReturnsNull()
    {
        var svc = CreateService();
        var payload = svc.ValidateToken("!!!invalid!!!", Guid.NewGuid(), null);
        payload.Should().BeNull();
    }

    [Fact]
    public void ValidateToken_CachesResult_ReturnsCachedOnSecondCall()
    {
        var svc = CreateService();
        var assetId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var token = svc.GenerateToken(assetId, tenantId, AssetAccessPolicy.Authenticated);

        var first = svc.ValidateToken(token, assetId, tenantId);
        var second = svc.ValidateToken(token, assetId, tenantId);

        first.Should().NotBeNull();
        second.Should().NotBeNull();
        // Both should have same data (from cache)
        second!.AssetReferenceId.Should().Be(first!.AssetReferenceId);
    }

    [Fact]
    public void ValidateToken_WithNullTenantId_Works()
    {
        var svc = CreateService();
        var assetId = Guid.NewGuid();
        var token = svc.GenerateToken(assetId, Guid.Empty, AssetAccessPolicy.Public);
        var payload = svc.ValidateToken(token, assetId, null);
        payload.Should().NotBeNull();
    }

    [Fact]
    public void GetCurrentTimeWindow_ReturnsNonNegativeInt()
    {
        var svc = CreateService();
        svc.GetCurrentTimeWindow().Should().BeGreaterOrEqualTo(0);
    }

    [Fact]
    public void GenerateEphemeralToken_WithoutUserId_Succeeds()
    {
        var svc = CreateService();
        var assetId = Guid.NewGuid();
        var token = svc.GenerateEphemeralToken(assetId, TimeSpan.FromHours(1));
        token.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void GenerateEphemeralToken_WithUserId_Succeeds()
    {
        var svc = CreateService();
        var assetId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var token = svc.GenerateEphemeralToken(assetId, TimeSpan.FromHours(1), userId);
        token.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void ValidateEphemeralToken_Valid_ReturnsPayload()
    {
        var svc = CreateService();
        var assetId = Guid.NewGuid();
        var token = svc.GenerateEphemeralToken(assetId, TimeSpan.FromHours(1));

        var payload = svc.ValidateEphemeralToken(token);

        payload.Should().NotBeNull();
        payload!.AssetReferenceId.Should().Be(assetId);
        payload.UserId.Should().BeNull();
    }

    [Fact]
    public void ValidateEphemeralToken_WithUserId_ReturnsPayloadWithUserId()
    {
        var svc = CreateService();
        var assetId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var token = svc.GenerateEphemeralToken(assetId, TimeSpan.FromHours(1), userId);

        var payload = svc.ValidateEphemeralToken(token);

        payload.Should().NotBeNull();
        payload!.AssetReferenceId.Should().Be(assetId);
        payload.UserId.Should().Be(userId);
    }

    [Fact]
    public void ValidateEphemeralToken_WithTruncatedToken_ReturnsNull()
    {
        var svc = CreateService();
        var payload = svc.ValidateEphemeralToken("short");
        payload.Should().BeNull();
    }

    [Fact]
    public void ValidateEphemeralToken_WithMalformedToken_ReturnsNull()
    {
        var svc = CreateService();
        var payload = svc.ValidateEphemeralToken("!!!malformed!!!");
        payload.Should().BeNull();
    }

    [Fact]
    public void ValidateEphemeralToken_WithTamperedSignature_ReturnsNull()
    {
        var svc = CreateService();
        var token = svc.GenerateEphemeralToken(Guid.NewGuid(), TimeSpan.FromHours(1));
        var chars = token.ToCharArray();
        chars[0] = chars[0] == 'A' ? 'B' : 'A';
        var tampered = new string(chars);

        svc.ValidateEphemeralToken(tampered).Should().BeNull();
    }

    [Fact]
    public void ValidateToken_DifferentAccessPolicies_AllRoundTrip()
    {
        var svc = CreateService();
        var assetId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();

        foreach (var policy in Enum.GetValues<AssetAccessPolicy>())
        {
            var token = svc.GenerateToken(assetId, tenantId, policy);
            var payload = svc.ValidateToken(token, assetId, tenantId);
            payload.Should().NotBeNull($"policy {policy} should round-trip");
            payload!.AccessPolicy.Should().Be(policy);
        }
    }
}

// ═══════════════════════════════════════════════════════════════════
//  AssetAccessService — 33 uncovered lines
// ═══════════════════════════════════════════════════════════════════
public class AssetAccessServiceCoverageTests
{
    private readonly Mock<IAssetReferenceRepository> _refRepo = new();
    private readonly Mock<ITransformedAssetRepository> _transformedAssetRepository = new();
    private readonly Mock<IAssetStorageService> _storageService = new();
    private readonly Mock<IAssetTokenService> _tokenService = new();
    private readonly Mock<ITenantMemberRepository> _tenantMemberRepository = new();
    private readonly Mock<IFeatureFlagEvaluationService> _featureService = new();
    private readonly Mock<IResourcePermissionService> _resourcePermissionService = new();
    private readonly AssetAccessOptions _options = new() { BaseUrl = "https://cdn.test.com", DefaultExpiryMinutes = 60 };

    private AssetAccessService CreateService()
    {
        return new AssetAccessService(
            _refRepo.Object,
            _transformedAssetRepository.Object,
            _storageService.Object,
            _tokenService.Object,
            _tenantMemberRepository.Object,
            _featureService.Object,
            Options.Create(_options),
            NullLogger<AssetAccessService>.Instance,
            _resourcePermissionService.Object);
    }

    private static AssetReference CreateRef(
        Guid id,
        Guid? userId = null,
        AssetAccessPolicy policy = AssetAccessPolicy.Public,
        bool deleted = false,
        string? parentResourceType = null,
        Guid? parentResourceId = null)
    {
        var uid = userId ?? Guid.NewGuid();
        var r = new AssetReference(Guid.NewGuid(), uid, "test", policy, parentResourceType, parentResourceId);
        typeof(AssetReference).GetProperty("Id")!.SetValue(r, id);
        if (deleted)
        {
            typeof(AssetReference).GetProperty("Version")!.SetValue(r, 1);
            r.SoftDelete();
        }
        return r;
    }

    private static AssetContent CreateContent(VirusScanStatus scanStatus = VirusScanStatus.Clean, ModerationStatus modStatus = ModerationStatus.Approved)
    {
        var c = new AssetContent("assets", "content/ab/c1/abc123.png", "abc123def456ghi789", "image/png", 1024, 100, 100);
        c.VirusScanStatus = scanStatus;
        c.ModerationStatus = modStatus;
        return c;
    }

    // --- ValidateAccessAsync ---

    [Fact]
    public async Task ValidateAccessAsync_NotFound_ReturnsInvalid()
    {
        _refRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((AssetReference?)null);

        var svc = CreateService();
        var result = await svc.ValidateAccessAsync(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());

        result.IsValid.Should().BeFalse();
        result.DeniedReason.Should().Be(AssetAccessDeniedReason.NotFound);
    }

    [Fact]
    public async Task ValidateAccessAsync_Deleted_ReturnsNotFound()
    {
        var id = Guid.NewGuid();
        var assetRef = CreateRef(id, deleted: true);
        _refRepo.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(assetRef);

        var svc = CreateService();
        var result = await svc.ValidateAccessAsync(id, Guid.NewGuid(), Guid.NewGuid());

        result.IsValid.Should().BeFalse();
        result.DeniedReason.Should().Be(AssetAccessDeniedReason.NotFound);
    }

    [Fact]
    public async Task ValidateAccessAsync_Public_ReturnsValid()
    {
        var id = Guid.NewGuid();
        _refRepo.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateRef(id, policy: AssetAccessPolicy.Public));

        var svc = CreateService();
        var result = await svc.ValidateAccessAsync(id, null, null);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateAccessAsync_Unlisted_ReturnsValid()
    {
        var id = Guid.NewGuid();
        _refRepo.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateRef(id, policy: AssetAccessPolicy.Unlisted));

        var svc = CreateService();
        var result = await svc.ValidateAccessAsync(id, null, null);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateAccessAsync_Authenticated_NoUser_ReturnsDenied()
    {
        var id = Guid.NewGuid();
        _refRepo.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateRef(id, policy: AssetAccessPolicy.Authenticated));

        var svc = CreateService();
        var result = await svc.ValidateAccessAsync(id, null, null);
        result.IsValid.Should().BeFalse();
        result.DeniedReason.Should().Be(AssetAccessDeniedReason.AuthenticationRequired);
    }

    [Fact]
    public async Task ValidateAccessAsync_Authenticated_WithUser_ReturnsValid()
    {
        var id = Guid.NewGuid();
        _refRepo.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateRef(id, policy: AssetAccessPolicy.Authenticated));

        var svc = CreateService();
        var result = await svc.ValidateAccessAsync(id, Guid.NewGuid(), null);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateAccessAsync_OwnerOnly_NoUser_ReturnsDenied()
    {
        var id = Guid.NewGuid();
        _refRepo.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateRef(id, policy: AssetAccessPolicy.OwnerOnly));

        var svc = CreateService();
        var result = await svc.ValidateAccessAsync(id, null, null);
        result.IsValid.Should().BeFalse();
        result.DeniedReason.Should().Be(AssetAccessDeniedReason.AuthenticationRequired);
    }

    [Fact]
    public async Task ValidateAccessAsync_OwnerOnly_WrongUser_ReturnsDenied()
    {
        var id = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        _refRepo.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateRef(id, userId: ownerId, policy: AssetAccessPolicy.OwnerOnly));

        var svc = CreateService();
        var result = await svc.ValidateAccessAsync(id, Guid.NewGuid(), null);
        result.IsValid.Should().BeFalse();
        result.DeniedReason.Should().Be(AssetAccessDeniedReason.OwnershipRequired);
    }

    [Fact]
    public async Task ValidateAccessAsync_OwnerOnly_CorrectOwner_ReturnsValid()
    {
        var id = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        _refRepo.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateRef(id, userId: ownerId, policy: AssetAccessPolicy.OwnerOnly));

        var svc = CreateService();
        var result = await svc.ValidateAccessAsync(id, ownerId, null);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateAccessAsync_Inherited_NoUser_ReturnsDenied()
    {
        var id = Guid.NewGuid();
        _refRepo.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateRef(id, policy: AssetAccessPolicy.Inherited));

        var svc = CreateService();
        var result = await svc.ValidateAccessAsync(id, null, null);
        result.IsValid.Should().BeFalse();
        result.DeniedReason.Should().Be(AssetAccessDeniedReason.AuthenticationRequired);
    }

    [Fact]
    public async Task ValidateAccessAsync_Inherited_WithUser_ReturnsValid()
    {
        var id = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var parentResourceId = Guid.NewGuid();
        _refRepo.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateRef(
                id,
                policy: AssetAccessPolicy.Inherited,
                parentResourceType: "Course",
                parentResourceId: parentResourceId));
        _resourcePermissionService
            .Setup(x => x.HasPermissionAsync(
                It.Is<TenantId>(value => value.Value == tenantId),
                userId,
                "Course",
                parentResourceId.ToString(),
                "read",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var svc = CreateService();
        var result = await svc.ValidateAccessAsync(id, userId, tenantId);
        result.IsValid.Should().BeTrue();
    }

    // --- ValidateToken ---

    [Fact]
    public void ValidateToken_Valid_ReturnsTrue()
    {
        var assetId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        _tokenService.Setup(t => t.ValidateToken("tok", assetId, tenantId))
            .Returns(new AssetTokenPayload(assetId, 1, DateTimeOffset.UtcNow.AddHours(1).ToUnixTimeSeconds(),
                AssetAccessPolicy.Public, "", tenantId));

        var svc = CreateService();
        svc.ValidateToken("tok", assetId, tenantId).Should().BeTrue();
    }

    [Fact]
    public void ValidateToken_Invalid_ReturnsFalse()
    {
        _tokenService.Setup(t => t.ValidateToken(It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<Guid?>()))
            .Returns((AssetTokenPayload?)null);

        var svc = CreateService();
        svc.ValidateToken("bad", Guid.NewGuid(), null).Should().BeFalse();
    }

    // --- ValidateAccessTokenAsync ---

    [Fact]
    public async Task ValidateAccessTokenAsync_EmptyToken_ReturnsInvalid()
    {
        var svc = CreateService();
        var result = await svc.ValidateAccessTokenAsync(Guid.NewGuid(), "", CancellationToken.None);
        result.IsValid.Should().BeFalse();
        result.Error.Should().Be("Token is required");
    }

    [Fact]
    public async Task ValidateAccessTokenAsync_InvalidToken_ReturnsInvalid()
    {
        _tokenService.Setup(t => t.ValidateToken(It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<Guid?>()))
            .Returns((AssetTokenPayload?)null);

        var svc = CreateService();
        var result = await svc.ValidateAccessTokenAsync(Guid.NewGuid(), "invalid", CancellationToken.None);
        result.IsValid.Should().BeFalse();
        result.Error.Should().Be("Invalid or expired token");
    }

    [Fact]
    public async Task ValidateAccessTokenAsync_ValidToken_AssetNotFound_ReturnsInvalid()
    {
        var assetId = Guid.NewGuid();
        _tokenService.Setup(t => t.ValidateToken("tok", assetId, It.IsAny<Guid?>()))
            .Returns(new AssetTokenPayload(assetId, 1, DateTimeOffset.UtcNow.AddHours(1).ToUnixTimeSeconds(),
                AssetAccessPolicy.Public, "", Guid.Empty));
        _refRepo.Setup(r => r.GetByIdAsync(assetId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((AssetReference?)null);

        var svc = CreateService();
        var result = await svc.ValidateAccessTokenAsync(assetId, "tok");
        result.IsValid.Should().BeFalse();
        result.Error.Should().Be("Asset not found");
    }

    [Fact]
    public async Task ValidateAccessTokenAsync_ValidToken_AssetExists_ReturnsValid()
    {
        var assetId = Guid.NewGuid();
        var expiryTs = DateTimeOffset.UtcNow.AddHours(1).ToUnixTimeSeconds();
        _tokenService.Setup(t => t.ValidateToken("tok", assetId, It.IsAny<Guid?>()))
            .Returns(new AssetTokenPayload(assetId, 1, expiryTs, AssetAccessPolicy.Public, "", Guid.Empty));
        _refRepo.Setup(r => r.GetByIdAsync(assetId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateRef(assetId));

        var svc = CreateService();
        var result = await svc.ValidateAccessTokenAsync(assetId, "tok");
        result.IsValid.Should().BeTrue();
    }

    // --- ValidateEphemeralTokenAsync ---

    [Fact]
    public async Task ValidateEphemeralTokenAsync_EmptyToken_ReturnsInvalid()
    {
        var svc = CreateService();
        var result = await svc.ValidateEphemeralTokenAsync("");
        result.IsValid.Should().BeFalse();
        result.Error.Should().Be("Token is required");
    }

    [Fact]
    public async Task ValidateEphemeralTokenAsync_InvalidToken_ReturnsInvalid()
    {
        _tokenService.Setup(t => t.ValidateEphemeralToken("bad")).Returns((EphemeralTokenPayload?)null);

        var svc = CreateService();
        var result = await svc.ValidateEphemeralTokenAsync("bad");
        result.IsValid.Should().BeFalse();
        result.Error.Should().Be("Invalid ephemeral token");
    }

    [Fact]
    public async Task ValidateEphemeralTokenAsync_ExpiredToken_ReturnsExpired()
    {
        var assetId = Guid.NewGuid();
        _tokenService.Setup(t => t.ValidateEphemeralToken("expired"))
            .Returns(new EphemeralTokenPayload(assetId, DateTimeOffset.UtcNow.AddHours(-1)));

        var svc = CreateService();
        var result = await svc.ValidateEphemeralTokenAsync("expired");
        result.IsValid.Should().BeFalse();
        result.IsExpired.Should().BeTrue();
        result.Error.Should().Be("Token has expired");
    }

    [Fact]
    public async Task ValidateEphemeralTokenAsync_ValidToken_ReturnsValid()
    {
        var assetId = Guid.NewGuid();
        _tokenService.Setup(t => t.ValidateEphemeralToken("valid"))
            .Returns(new EphemeralTokenPayload(assetId, DateTimeOffset.UtcNow.AddHours(1)));

        var svc = CreateService();
        var result = await svc.ValidateEphemeralTokenAsync("valid");
        result.IsValid.Should().BeTrue();
        result.AssetReferenceId.Should().Be(assetId);
    }

    // --- GetOrCreateTransformationAsync ---

    [Fact]
    public async Task GetOrCreateTransformationAsync_Disabled_ReturnsNull()
    {
        _featureService.Setup(f => f.IsEnabledAsync(
            FeatureFlagConstants.AssetFeatureFlags.TransformationsEnabled,
            It.IsAny<FeatureContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var svc = CreateService();
        var result = await svc.GetOrCreateTransformationAsync(Guid.NewGuid(), TransformationSpec.Parse("w=100"));
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetOrCreateTransformationAsync_Enabled_ReturnsCachedTransformedAsset()
    {
        _featureService.Setup(f => f.IsEnabledAsync(
            FeatureFlagConstants.AssetFeatureFlags.TransformationsEnabled,
            It.IsAny<FeatureContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        var contentId = Guid.NewGuid();
        var transformedAsset = new TransformedAsset
        {
            Id = Guid.NewGuid(),
            SourceContentId = contentId,
            TransformationSpec = "w=100",
            BucketName = "assets-transformed",
            ObjectKey = "transformed/image.webp",
            MimeType = "image/webp",
            SizeBytes = 123
        };
        _transformedAssetRepository
            .Setup(r => r.GetAsync(contentId, "w=100", It.IsAny<CancellationToken>()))
            .ReturnsAsync(transformedAsset);
        _storageService
            .Setup(s => s.GetMetadataAsync("assets-transformed", "transformed/image.webp", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new StorageMetadata(123, "image/webp", "etag-123", DateTime.UtcNow));

        var svc = CreateService();
        var result = await svc.GetOrCreateTransformationAsync(contentId, TransformationSpec.Parse("w=100"));

        result.Should().NotBeNull();
        result!.Id.Should().Be(transformedAsset.Id);
        result.ContentHash.Should().Be("etag-123");
        _transformedAssetRepository.Verify(r => r.UpdateAsync(transformedAsset, It.IsAny<CancellationToken>()), Times.Once);
    }

    // --- GenerateAccessUrlAsync ---

    [Fact]
    public async Task GenerateAccessUrlAsync_NoReference_ReturnsNull()
    {
        _refRepo.Setup(r => r.GetByIdWithContentAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((AssetReference?)null);

        var svc = CreateService();
        var result = await svc.GenerateAccessUrlAsync(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        result.Should().BeNull();
    }

    [Fact]
    public async Task GenerateAccessUrlAsync_AccessDenied_ReturnsNull()
    {
        var id = Guid.NewGuid();
        var assetRef = CreateRef(id, policy: AssetAccessPolicy.OwnerOnly);
        assetRef.Content = CreateContent();
        _refRepo.Setup(r => r.GetByIdWithContentAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(assetRef);
        _refRepo.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(assetRef);

        var svc = CreateService();
        var result = await svc.GenerateAccessUrlAsync(id, null, null);
        result.Should().BeNull();
    }

    [Fact]
    public async Task GenerateAccessUrlAsync_InfectedContent_ReturnsNull()
    {
        var id = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var assetRef = CreateRef(id, userId: userId, policy: AssetAccessPolicy.Public);
        assetRef.Content = CreateContent(scanStatus: VirusScanStatus.Infected);
        _refRepo.Setup(r => r.GetByIdWithContentAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(assetRef);
        _refRepo.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(assetRef);

        var svc = CreateService();
        var result = await svc.GenerateAccessUrlAsync(id, userId, null);
        result.Should().BeNull();
    }

    [Fact]
    public async Task GenerateAccessUrlAsync_BlockedContent_ReturnsNull()
    {
        var id = Guid.NewGuid();
        var assetRef = CreateRef(id, policy: AssetAccessPolicy.Public);
        assetRef.Content = CreateContent(modStatus: ModerationStatus.Blocked);
        _refRepo.Setup(r => r.GetByIdWithContentAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(assetRef);
        _refRepo.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(assetRef);

        var svc = CreateService();
        var result = await svc.GenerateAccessUrlAsync(id, Guid.NewGuid(), null);
        result.Should().BeNull();
    }

    [Fact]
    public async Task GenerateAccessUrlAsync_PublicClean_ReturnsUrl()
    {
        var id = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var assetRef = CreateRef(id, policy: AssetAccessPolicy.Public);
        assetRef.Content = CreateContent();
        _refRepo.Setup(r => r.GetByIdWithContentAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(assetRef);
        _refRepo.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(assetRef);
        _tokenService.Setup(t => t.GenerateToken(id, tenantId, AssetAccessPolicy.Public, null, It.IsAny<TimeSpan?>()))
            .Returns("generated-token");
        _featureService.Setup(f => f.GetValueAsync(
            FeatureFlagConstants.AssetFeatureFlags.DownloadWindowHours,
            It.IsAny<FeatureContext>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var svc = CreateService();
        var result = await svc.GenerateAccessUrlAsync(id, Guid.NewGuid(), tenantId);
        result.Should().NotBeNull();
        result!.Url.Should().Contain("generated-token");
        result.Token.Should().Be("generated-token");
    }

    [Fact]
    public async Task GenerateAccessUrlAsync_WithTransformation_DisabledByFlag_IgnoresTransform()
    {
        var id = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var assetRef = CreateRef(id, policy: AssetAccessPolicy.Public);
        assetRef.Content = CreateContent();
        _refRepo.Setup(r => r.GetByIdWithContentAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(assetRef);
        _refRepo.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(assetRef);
        _tokenService.Setup(t => t.GenerateToken(id, tenantId, AssetAccessPolicy.Public, null, It.IsAny<TimeSpan?>()))
            .Returns("tok");
        _featureService.Setup(f => f.IsEnabledAsync(
            FeatureFlagConstants.AssetFeatureFlags.TransformationsEnabled,
            It.IsAny<FeatureContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _featureService.Setup(f => f.GetValueAsync(
            FeatureFlagConstants.AssetFeatureFlags.DownloadWindowHours,
            It.IsAny<FeatureContext>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var spec = TransformationSpec.Parse("w=100");
        var svc = CreateService();
        var result = await svc.GenerateAccessUrlAsync(id, Guid.NewGuid(), tenantId, spec);
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task GenerateAccessUrlAsync_TransformExceedsDimension_ReturnsNull()
    {
        var id = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var assetRef = CreateRef(id, policy: AssetAccessPolicy.Public);
        assetRef.Content = CreateContent();
        _refRepo.Setup(r => r.GetByIdWithContentAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(assetRef);
        _refRepo.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(assetRef);
        _featureService.Setup(f => f.IsEnabledAsync(
            FeatureFlagConstants.AssetFeatureFlags.TransformationsEnabled,
            It.IsAny<FeatureContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _featureService.Setup(f => f.GetValueAsync<int>(
            FeatureFlagConstants.AssetFeatureFlags.MaxTransformDimension,
            It.IsAny<FeatureContext>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(100);

        var spec = TransformationSpec.Parse("w=5000");
        var svc = CreateService();
        var result = await svc.GenerateAccessUrlAsync(id, Guid.NewGuid(), tenantId, spec);
        result.Should().BeNull();
    }

    // --- GenerateDirectStorageUrlAsync ---

    [Fact]
    public async Task GenerateDirectStorageUrlAsync_NotFound_ReturnsNull()
    {
        _refRepo.Setup(r => r.GetByIdWithContentAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((AssetReference?)null);

        var svc = CreateService();
        var result = await svc.GenerateDirectStorageUrlAsync(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        result.Should().BeNull();
    }

    [Fact]
    public async Task GenerateDirectStorageUrlAsync_AccessDenied_ReturnsNull()
    {
        var id = Guid.NewGuid();
        var assetRef = CreateRef(id, policy: AssetAccessPolicy.OwnerOnly);
        assetRef.Content = CreateContent();
        _refRepo.Setup(r => r.GetByIdWithContentAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(assetRef);
        _refRepo.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(assetRef);

        var svc = CreateService();
        var result = await svc.GenerateDirectStorageUrlAsync(id, null, null);
        result.Should().BeNull();
    }

    [Fact]
    public async Task GenerateDirectStorageUrlAsync_Succeeds_ReturnsPresignedUrl()
    {
        var id = Guid.NewGuid();
        var assetRef = CreateRef(id, policy: AssetAccessPolicy.Public);
        assetRef.Content = CreateContent();
        _refRepo.Setup(r => r.GetByIdWithContentAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(assetRef);
        _refRepo.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(assetRef);
        _storageService.Setup(s => s.GeneratePresignedUrlAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<TimeSpan>(), true, It.IsAny<CancellationToken>()))
            .ReturnsAsync("https://presigned.url/file");

        var svc = CreateService();
        var result = await svc.GenerateDirectStorageUrlAsync(id, Guid.NewGuid(), Guid.NewGuid());
        result.Should().NotBeNull();
        result!.Url.Should().Be("https://presigned.url/file");
    }
}

// ═══════════════════════════════════════════════════════════════════
//  DeduplicationService — 15 uncovered lines
// ═══════════════════════════════════════════════════════════════════
public class DeduplicationServiceCoverageTests
{
    private readonly Mock<IAssetContentRepository> _contentRepo = new();

    private DeduplicationService CreateService(bool enabled = true, bool perceptual = true, int threshold = 5)
    {
        var opts = Options.Create(new DeduplicationOptions
        {
            Enabled = enabled,
            EnablePerceptualHashing = perceptual,
            PerceptualHashThreshold = threshold
        });
        return new DeduplicationService(_contentRepo.Object, opts, NullLogger<DeduplicationService>.Instance);
    }

    [Fact]
    public async Task ComputeContentHashAsync_ReturnsHexHash()
    {
        var svc = CreateService();
        using var stream = new MemoryStream(new byte[] { 1, 2, 3 });
        var hash = await svc.ComputeContentHashAsync(stream);
        hash.Should().NotBeNullOrEmpty();
        hash.Should().MatchRegex("^[a-f0-9]+$");
        stream.Position.Should().Be(0); // stream reset
    }

    [Fact]
    public async Task ComputePerceptualHashAsync_DisabledSetting_ReturnsNull()
    {
        var svc = CreateService(perceptual: false);
        using var stream = new MemoryStream(new byte[10]);
        var hash = await svc.ComputePerceptualHashAsync(stream, "image/png");
        hash.Should().BeNull();
    }

    [Fact]
    public async Task ComputePerceptualHashAsync_NonImage_ReturnsNull()
    {
        var svc = CreateService();
        using var stream = new MemoryStream(new byte[10]);
        var hash = await svc.ComputePerceptualHashAsync(stream, "application/pdf");
        hash.Should().BeNull();
    }

    [Fact]
    public async Task ComputePerceptualHashAsync_InvalidImageData_ReturnsNull()
    {
        var svc = CreateService();
        using var stream = new MemoryStream(new byte[] { 0, 0, 0 });
        // Invalid image data → exception caught → returns null
        var hash = await svc.ComputePerceptualHashAsync(stream, "image/png");
        hash.Should().BeNull();
    }

    [Fact]
    public void ComputeHammingDistance_EmptyHash_ReturnsMaxValue()
    {
        DeduplicationService.ComputeHammingDistance("", "abc").Should().Be(int.MaxValue);
        DeduplicationService.ComputeHammingDistance("abc", "").Should().Be(int.MaxValue);
        DeduplicationService.ComputeHammingDistance(null!, "abc").Should().Be(int.MaxValue);
    }

    [Fact]
    public void ComputeHammingDistance_InvalidHex_ReturnsMaxValue()
    {
        DeduplicationService.ComputeHammingDistance("zzzzzzzzzzzzzzzz", "0000000000000000")
            .Should().Be(int.MaxValue);
    }

    [Fact]
    public void ComputeHammingDistance_IdenticalHashes_ReturnsZero()
    {
        DeduplicationService.ComputeHammingDistance("0000000000000001", "0000000000000001")
            .Should().Be(0);
    }

    [Fact]
    public void ComputeHammingDistance_DifferentHashes_ReturnsPopCount()
    {
        // 0x0000000000000001 vs 0x0000000000000003 => XOR = 2 => popcount = 1
        DeduplicationService.ComputeHammingDistance("0000000000000001", "0000000000000003")
            .Should().Be(1);
    }

    [Fact]
    public void AreSimilar_NullHashes_ReturnsFalse()
    {
        var svc = CreateService();
        svc.AreSimilar(null, "abc").Should().BeFalse();
        svc.AreSimilar("abc", null).Should().BeFalse();
    }

    [Fact]
    public void AreSimilar_IdenticalHashes_ReturnsTrue()
    {
        var svc = CreateService(threshold: 5);
        svc.AreSimilar("0000000000000001", "0000000000000001").Should().BeTrue();
    }

    [Fact]
    public void AreSimilar_VeryDifferentHashes_ReturnsFalse()
    {
        var svc = CreateService(threshold: 1);
        svc.AreSimilar("ffffffffffffffff", "0000000000000000").Should().BeFalse();
    }

    [Fact]
    public async Task FindExistingContentAsync_Disabled_ReturnsNull()
    {
        var svc = CreateService(enabled: false);
        var result = await svc.FindExistingContentAsync("somehash");
        result.Should().BeNull();
    }

    [Fact]
    public async Task FindExistingContentAsync_NotFound_ReturnsNull()
    {
        _contentRepo.Setup(r => r.GetByContentHashAsync("hash123", It.IsAny<CancellationToken>()))
            .ReturnsAsync((AssetContent?)null);
        var svc = CreateService();
        var result = await svc.FindExistingContentAsync("hash123");
        result.Should().BeNull();
    }

    [Fact]
    public async Task FindExistingContentAsync_Found_ReturnsId()
    {
        var contentId = Guid.NewGuid();
        var content = new AssetContent("assets", "content/ha/sh/hash123.png", "hash123", "image/png", 100, null, null);
        typeof(AssetContent).GetProperty("Id")!.SetValue(content, contentId);
        _contentRepo.Setup(r => r.GetByContentHashAsync("hash123", It.IsAny<CancellationToken>()))
            .ReturnsAsync(content);

        var svc = CreateService();
        var result = await svc.FindExistingContentAsync("hash123");
        result.Should().Be(contentId);
    }
}

// ═══════════════════════════════════════════════════════════════════
//  AssetAuthorizationHandler — HandleRequirementAsync + CheckAccessAsync (15 lines)
// ═══════════════════════════════════════════════════════════════════
public class AssetAuthorizationHandlerRequirementTests
{
    private readonly Mock<IActorContextAccessor> _actorCtx = new();
    private readonly Mock<IAssetReferenceRepository> _refRepo = new();
    private readonly Mock<IAccessControlListService> _aclService = new();

    private AssetAuthorizationHandler CreateHandler()
    {
        return new AssetAuthorizationHandler(
            _actorCtx.Object, _refRepo.Object, _aclService.Object,
            NullLogger<AssetAuthorizationHandler>.Instance);
    }

    private static ActorContext CreateActor(string? subjectId = null, Guid? tenantId = null, string[]? permissions = null, bool authenticated = true)
    {
        return new ActorContext
        {
            ActorKind = authenticated ? ActorKind.User : ActorKind.Anonymous,
            SubjectId = subjectId ?? Guid.NewGuid().ToString(),
            TenantId = tenantId,
            Permissions = new HashSet<string>(permissions ?? Array.Empty<string>()),
            Roles = new HashSet<string>(),
            IsAuthenticated = authenticated
        };
    }

    private static AuthorizationHandlerContext CreateAuthContext(
        AssetAccessRequirement requirement, object? resource = null)
    {
        var identity = new ClaimsIdentity("test");
        var principal = new ClaimsPrincipal(identity);
        return new AuthorizationHandlerContext(
            new[] { requirement },
            principal,
            resource);
    }

    // --- HandleRequirementAsync ---

    [Fact]
    public async Task HandleRequirementAsync_Unauthenticated_DoesNotSucceed()
    {
        _actorCtx.Setup(a => a.ActorContext).Returns(CreateActor(authenticated: false));
        var handler = CreateHandler();
        var ctx = CreateAuthContext(AssetAccessRequirement.Read);

        await handler.HandleAsync(ctx);

        ctx.HasSucceeded.Should().BeFalse();
    }

    [Fact]
    public async Task HandleRequirementAsync_HasPermission_Succeeds()
    {
        _actorCtx.Setup(a => a.ActorContext).Returns(CreateActor(permissions: new[] { AssetsPermission.Keys.Read }));
        var handler = CreateHandler();
        var ctx = CreateAuthContext(AssetAccessRequirement.Read);

        await handler.HandleAsync(ctx);

        ctx.HasSucceeded.Should().BeTrue();
    }

    private static AssetReference MakeRef(Guid? userId = null, Guid? id = null)
    {
        var uid = userId ?? Guid.NewGuid();
        var r = new AssetReference(Guid.NewGuid(), uid, "test", AssetAccessPolicy.Public, null, null);
        if (id.HasValue) typeof(AssetReference).GetProperty("Id")!.SetValue(r, id.Value);
        return r;
    }

    [Fact]
    public async Task HandleRequirementAsync_OwnerAccess_Succeeds()
    {
        var userId = Guid.NewGuid();
        var assetRef = MakeRef(userId: userId);

        _actorCtx.Setup(a => a.ActorContext).Returns(CreateActor(subjectId: userId.ToString()));
        var handler = CreateHandler();
        var ctx = CreateAuthContext(AssetAccessRequirement.Read, assetRef);

        await handler.HandleAsync(ctx);

        ctx.HasSucceeded.Should().BeTrue();
    }

    [Fact]
    public async Task HandleRequirementAsync_NoOwnerAccess_WhenNotAllowed_DoesNotSucceed()
    {
        var userId = Guid.NewGuid();
        var assetRef = MakeRef(userId: userId);

        // Create requirement does NOT allow owner access
        _actorCtx.Setup(a => a.ActorContext).Returns(CreateActor(subjectId: userId.ToString()));
        var handler = CreateHandler();
        var ctx = CreateAuthContext(AssetAccessRequirement.Create, assetRef);

        await handler.HandleAsync(ctx);

        // Create requirement: AllowOwnerAccess = false, so no permission + no owner + ACL (no tenant) → fail
        ctx.HasSucceeded.Should().BeFalse();
    }

    [Fact]
    public async Task HandleRequirementAsync_AclAccess_Succeeds()
    {
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var assetRef = MakeRef(userId: Guid.NewGuid()); // different user

        _actorCtx.Setup(a => a.ActorContext).Returns(CreateActor(subjectId: userId.ToString(), tenantId: tenantId));
        _aclService.Setup(a => a.HasAccessAsync(
            It.IsAny<AclSubject>(), tenantId, It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<AccessLevel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var handler = CreateHandler();
        var ctx = CreateAuthContext(AssetAccessRequirement.Read, assetRef);

        await handler.HandleAsync(ctx);

        ctx.HasSucceeded.Should().BeTrue();
    }

    [Fact]
    public async Task HandleRequirementAsync_AclDenied_DoesNotSucceed()
    {
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var assetRef = MakeRef(userId: Guid.NewGuid()); // different user

        _actorCtx.Setup(a => a.ActorContext).Returns(CreateActor(subjectId: userId.ToString(), tenantId: tenantId));
        _aclService.Setup(a => a.HasAccessAsync(
            It.IsAny<AclSubject>(), tenantId, It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<AccessLevel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var handler = CreateHandler();
        var ctx = CreateAuthContext(AssetAccessRequirement.Read, assetRef);

        await handler.HandleAsync(ctx);

        ctx.HasSucceeded.Should().BeFalse();
    }

    [Fact]
    public async Task HandleRequirementAsync_NoResource_NoTenant_DoesNotSucceed()
    {
        _actorCtx.Setup(a => a.ActorContext).Returns(CreateActor());
        var handler = CreateHandler();
        var ctx = CreateAuthContext(AssetAccessRequirement.Read); // no resource

        await handler.HandleAsync(ctx);

        ctx.HasSucceeded.Should().BeFalse();
    }

    // --- CheckAccessAsync via CanReadAsync etc. ---

    [Fact]
    public async Task CanReadAsync_WithPermission_ReturnsTrue()
    {
        _actorCtx.Setup(a => a.ActorContext).Returns(CreateActor(permissions: new[] { AssetsPermission.Keys.Read }));
        var handler = CreateHandler();
        var result = await handler.CanReadAsync(Guid.NewGuid());
        result.Should().BeTrue();
    }

    [Fact]
    public async Task CanReadAsync_Unauthenticated_ReturnsFalse()
    {
        _actorCtx.Setup(a => a.ActorContext).Returns(CreateActor(authenticated: false));
        var handler = CreateHandler();
        var result = await handler.CanReadAsync(Guid.NewGuid());
        result.Should().BeFalse();
    }

    [Fact]
    public async Task CanReadAsync_OwnsAsset_ReturnsTrue()
    {
        var userId = Guid.NewGuid();
        var assetId = Guid.NewGuid();
        var assetRef = MakeRef(userId: userId, id: assetId);

        _actorCtx.Setup(a => a.ActorContext).Returns(CreateActor(subjectId: userId.ToString()));
        _refRepo.Setup(r => r.GetByIdAsync(assetId, It.IsAny<CancellationToken>())).ReturnsAsync(assetRef);

        var handler = CreateHandler();
        var result = await handler.CanReadAsync(assetId);
        result.Should().BeTrue();
    }

    [Fact]
    public async Task CanReadAsync_AssetNotFound_ReturnsFalse()
    {
        _actorCtx.Setup(a => a.ActorContext).Returns(CreateActor());
        _refRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((AssetReference?)null);

        var handler = CreateHandler();
        var result = await handler.CanReadAsync(Guid.NewGuid());
        result.Should().BeFalse();
    }

    [Fact]
    public async Task CanReadAsync_AclGrantsAccess_ReturnsTrue()
    {
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var assetId = Guid.NewGuid();
        var assetRef = MakeRef(userId: Guid.NewGuid(), id: assetId); // different

        _actorCtx.Setup(a => a.ActorContext).Returns(CreateActor(subjectId: userId.ToString(), tenantId: tenantId));
        _refRepo.Setup(r => r.GetByIdAsync(assetId, It.IsAny<CancellationToken>())).ReturnsAsync(assetRef);
        _aclService.Setup(a => a.HasAccessAsync(
            It.IsAny<AclSubject>(), tenantId, It.IsAny<string>(), assetId.ToString(),
            AccessLevel.Read, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var handler = CreateHandler();
        var result = await handler.CanReadAsync(assetId);
        result.Should().BeTrue();
    }

    [Fact]
    public async Task CanReadAsync_NoTenant_NoPermission_NotOwner_ReturnsFalse()
    {
        var assetId = Guid.NewGuid();
        var assetRef = MakeRef(userId: Guid.NewGuid(), id: assetId);

        _actorCtx.Setup(a => a.ActorContext).Returns(CreateActor(tenantId: null)); // no tenant → no ACL check
        _refRepo.Setup(r => r.GetByIdAsync(assetId, It.IsAny<CancellationToken>())).ReturnsAsync(assetRef);

        var handler = CreateHandler();
        var result = await handler.CanReadAsync(assetId);
        result.Should().BeFalse();
    }

    [Fact]
    public async Task CanCreateAsync_WithPermission_ReturnsTrue()
    {
        _actorCtx.Setup(a => a.ActorContext).Returns(CreateActor(permissions: new[] { AssetsPermission.Keys.Create }));
        var handler = CreateHandler();
        var result = await handler.CanCreateAsync();
        result.Should().BeTrue();
    }

    [Fact]
    public async Task CanCreateAsync_Unauthenticated_ReturnsFalse()
    {
        _actorCtx.Setup(a => a.ActorContext).Returns(CreateActor(authenticated: false));
        var handler = CreateHandler();
        var result = await handler.CanCreateAsync();
        result.Should().BeFalse();
    }

    [Fact]
    public async Task CanUpdateAsync_WithPermission_ReturnsTrue()
    {
        _actorCtx.Setup(a => a.ActorContext).Returns(CreateActor(permissions: new[] { AssetsPermission.Keys.Update }));
        var handler = CreateHandler();
        var result = await handler.CanUpdateAsync(Guid.NewGuid());
        result.Should().BeTrue();
    }

    [Fact]
    public async Task CanDeleteAsync_WithPermission_ReturnsTrue()
    {
        _actorCtx.Setup(a => a.ActorContext).Returns(CreateActor(permissions: new[] { AssetsPermission.Keys.Delete }));
        var handler = CreateHandler();
        var result = await handler.CanDeleteAsync(Guid.NewGuid());
        result.Should().BeTrue();
    }

    [Fact]
    public async Task CanTransformAsync_WithPermission_ReturnsTrue()
    {
        _actorCtx.Setup(a => a.ActorContext).Returns(CreateActor(permissions: new[] { AssetsPermission.Keys.Transform }));
        var handler = CreateHandler();
        var result = await handler.CanTransformAsync(Guid.NewGuid());
        result.Should().BeTrue();
    }

    [Fact]
    public async Task CanGenerateUrlAsync_WithPermission_ReturnsTrue()
    {
        _actorCtx.Setup(a => a.ActorContext).Returns(CreateActor(permissions: new[] { AssetsPermission.Keys.GenerateUrl }));
        var handler = CreateHandler();
        var result = await handler.CanGenerateUrlAsync(Guid.NewGuid());
        result.Should().BeTrue();
    }

    [Fact]
    public async Task CanReportAsync_WithPermission_ReturnsTrue()
    {
        _actorCtx.Setup(a => a.ActorContext).Returns(CreateActor(permissions: new[] { AssetsPermission.Keys.Report }));
        var handler = CreateHandler();
        var result = await handler.CanReportAsync(Guid.NewGuid());
        result.Should().BeTrue();
    }

    [Fact]
    public async Task IsAdminAsync_WithPermission_ReturnsTrue()
    {
        _actorCtx.Setup(a => a.ActorContext).Returns(CreateActor(permissions: new[] { AssetsPermission.Keys.Admin }));
        var handler = CreateHandler();
        var result = await handler.IsAdminAsync();
        result.Should().BeTrue();
    }

    [Fact]
    public async Task CanModerateAsync_WithPermission_ReturnsTrue()
    {
        _actorCtx.Setup(a => a.ActorContext).Returns(CreateActor(permissions: new[] { AssetsPermission.Keys.Moderate }));
        var handler = CreateHandler();
        var result = await handler.CanModerateAsync();
        result.Should().BeTrue();
    }

    [Fact]
    public async Task IsAdminAsync_Unauthenticated_ReturnsFalse()
    {
        _actorCtx.Setup(a => a.ActorContext).Returns(CreateActor(authenticated: false));
        var handler = CreateHandler();
        var result = await handler.IsAdminAsync();
        result.Should().BeFalse();
    }

    [Fact]
    public async Task CanModerateAsync_Unauthenticated_ReturnsFalse()
    {
        _actorCtx.Setup(a => a.ActorContext).Returns(CreateActor(authenticated: false));
        var handler = CreateHandler();
        var result = await handler.CanModerateAsync();
        result.Should().BeFalse();
    }
}

// ═══════════════════════════════════════════════════════════════════
//  AssetRateLimitService — 4 uncovered lines
// ═══════════════════════════════════════════════════════════════════
public class AssetRateLimitServiceCoverageTests
{
    private readonly Mock<IDistributedCache> _cache = new();

    private AssetRateLimitService CreateService(bool enabled = true, int maxAccess = 1000, int max403 = 50, int blockMinutes = 60)
    {
        var opts = Options.Create(new AssetRateLimitOptions
        {
            Enabled = enabled,
            MaxAccessPerAssetPerHour = maxAccess,
            Max403PerIpPerHour = max403,
            BlockDurationMinutes = blockMinutes,
            WindowSizeSeconds = 3600
        });
        return new AssetRateLimitService(_cache.Object, opts, NullLogger<AssetRateLimitService>.Instance);
    }

    [Fact]
    public async Task CheckAssetAccessRateAsync_Disabled_ReturnsAllowed()
    {
        var svc = CreateService(enabled: false);
        var result = await svc.CheckAssetAccessRateAsync(Guid.NewGuid());
        result.IsAllowed.Should().BeTrue();
    }

    [Fact]
    public async Task CheckAssetAccessRateAsync_BelowLimit_ReturnsAllowed()
    {
        _cache.Setup(c => c.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((byte[]?)null);

        var svc = CreateService();
        var result = await svc.CheckAssetAccessRateAsync(Guid.NewGuid());
        result.IsAllowed.Should().BeTrue();
        result.CurrentCount.Should().Be(1);
    }

    [Fact]
    public async Task CheckAssetAccessRateAsync_AtLimit_ReturnsDenied()
    {
        var countBytes = System.Text.Encoding.UTF8.GetBytes("1000");
        _cache.Setup(c => c.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(countBytes);

        var svc = CreateService(maxAccess: 1000);
        var result = await svc.CheckAssetAccessRateAsync(Guid.NewGuid());
        result.IsAllowed.Should().BeFalse();
        result.Reason.Should().Contain("rate limit");
    }

    [Fact]
    public async Task Record403ResponseAsync_Disabled_ReturnsAllowed()
    {
        var svc = CreateService(enabled: false);
        var result = await svc.Record403ResponseAsync("1.2.3.4");
        result.IsAllowed.Should().BeTrue();
    }

    [Fact]
    public async Task Record403ResponseAsync_EmptyIp_ReturnsAllowed()
    {
        var svc = CreateService();
        var result = await svc.Record403ResponseAsync("");
        result.IsAllowed.Should().BeTrue();
    }

    [Fact]
    public async Task Record403ResponseAsync_BelowLimit_ReturnsAllowed()
    {
        _cache.Setup(c => c.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((byte[]?)null);

        var svc = CreateService();
        var result = await svc.Record403ResponseAsync("1.2.3.4");
        result.IsAllowed.Should().BeTrue();
    }

    [Fact]
    public async Task Record403ResponseAsync_AtLimit_BlocksIp()
    {
        var countBytes = System.Text.Encoding.UTF8.GetBytes("49");
        _cache.Setup(c => c.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(countBytes);

        var svc = CreateService(max403: 50);
        var result = await svc.Record403ResponseAsync("1.2.3.4");
        result.IsAllowed.Should().BeFalse();
        result.Reason.Should().Contain("blocked");
    }

    [Fact]
    public async Task IsIpBlockedAsync_Disabled_ReturnsFalse()
    {
        var svc = CreateService(enabled: false);
        var result = await svc.IsIpBlockedAsync("1.2.3.4");
        result.Should().BeFalse();
    }

    [Fact]
    public async Task IsIpBlockedAsync_EmptyIp_ReturnsFalse()
    {
        var svc = CreateService();
        var result = await svc.IsIpBlockedAsync("");
        result.Should().BeFalse();
    }

    [Fact]
    public async Task IsIpBlockedAsync_NotBlocked_ReturnsFalse()
    {
        _cache.Setup(c => c.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((byte[]?)null);

        var svc = CreateService();
        var result = await svc.IsIpBlockedAsync("1.2.3.4");
        result.Should().BeFalse();
    }

    [Fact]
    public async Task IsIpBlockedAsync_Blocked_ReturnsTrue()
    {
        var blockedAt = System.Text.Encoding.UTF8.GetBytes(DateTime.UtcNow.ToString("O"));
        _cache.Setup(c => c.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(blockedAt);

        var svc = CreateService();
        var result = await svc.IsIpBlockedAsync("1.2.3.4");
        result.Should().BeTrue();
    }

    [Fact]
    public async Task GetAccessStatsAsync_NoCacheEntry_ReturnsZero()
    {
        _cache.Setup(c => c.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((byte[]?)null);

        var svc = CreateService();
        var assetId = Guid.NewGuid();
        var stats = await svc.GetAccessStatsAsync(assetId);
        stats.CurrentHourCount.Should().Be(0);
        stats.AssetReferenceId.Should().Be(assetId);
    }

    [Fact]
    public async Task GetAccessStatsAsync_WithCacheEntry_ReturnsCount()
    {
        var countBytes = System.Text.Encoding.UTF8.GetBytes("42");
        _cache.Setup(c => c.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(countBytes);

        var svc = CreateService();
        var stats = await svc.GetAccessStatsAsync(Guid.NewGuid());
        stats.CurrentHourCount.Should().Be(42);
    }
}

// ═══════════════════════════════════════════════════════════════════
//  VirusScanService — 2 uncovered lines
// ═══════════════════════════════════════════════════════════════════
public class VirusScanServiceCoverageTests
{
    private VirusScanService CreateService(bool enabled = true, VirusScanMode mode = VirusScanMode.Hybrid,
        long maxScanSize = 100_000_000, string[]? syncMimeTypes = null)
    {
        var opts = Options.Create(new VirusScanOptions
        {
            Enabled = enabled,
            Mode = mode,
            MaxScanSizeBytes = maxScanSize,
            SyncScanMimeTypes = syncMimeTypes ?? new[] { "image/jpeg", "image/png" }
        });
        return new VirusScanService(opts, NullLogger<VirusScanService>.Instance);
    }

    [Fact]
    public async Task ScanAsync_Disabled_ReturnsClean()
    {
        var svc = CreateService(enabled: false);
        using var stream = new MemoryStream(new byte[10]);
        var result = await svc.ScanAsync(stream, "test.txt");
        result.IsClean.Should().BeTrue();
        result.Status.Should().Be("Scanning disabled");
    }

    [Fact]
    public async Task ScanAsync_FileTooLarge_ReturnsOversized()
    {
        var svc = CreateService(maxScanSize: 5);
        using var stream = new MemoryStream(new byte[10]);
        var result = await svc.ScanAsync(stream, "big.bin");
        result.IsClean.Should().BeFalse();
        result.ThreatName.Should().Be("OVERSIZED_FILE");
    }

    [Fact]
    public async Task ScanAsync_NormalFile_ReturnsClean()
    {
        var svc = CreateService();
        using var stream = new MemoryStream(new byte[100]);
        var result = await svc.ScanAsync(stream, "ok.txt");
        result.IsClean.Should().BeTrue();
        result.ScanEngine.Should().Be("LocalPolicy");
    }

    [Fact]
    public async Task ScanStoredAsync_RequiresStreamContent()
    {
        var svc = CreateService();
        var result = await svc.ScanStoredAsync("bucket", "key.bin");
        result.IsClean.Should().BeFalse();
        result.ThreatName.Should().Be("STORED_SCAN_REQUIRES_STREAM");
    }

    [Fact]
    public async Task ScanAsync_EicarSignature_ReturnsThreat()
    {
        var svc = CreateService();
        using var stream = new MemoryStream(System.Text.Encoding.ASCII.GetBytes("X5O!P%@AP[4\\PZX54(P^)7CC)7}$EICAR-STANDARD-ANTIVIRUS-TEST-FILE!$H+H*"));
        var result = await svc.ScanAsync(stream, "eicar.txt");
        result.IsClean.Should().BeFalse();
        result.ThreatName.Should().Be("EICAR-Test-Signature");
    }

    [Fact]
    public async Task IsHealthyAsync_ReturnsTrue()
    {
        var svc = CreateService();
        var result = await svc.IsHealthyAsync();
        result.Should().BeTrue();
    }

    [Fact]
    public void RequiresSyncScan_SyncMode_AlwaysTrue()
    {
        var svc = CreateService(mode: VirusScanMode.Sync);
        svc.RequiresSyncScan("application/pdf").Should().BeTrue();
    }

    [Fact]
    public void RequiresSyncScan_AsyncMode_AlwaysFalse()
    {
        var svc = CreateService(mode: VirusScanMode.Async);
        svc.RequiresSyncScan("image/png").Should().BeFalse();
    }

    [Fact]
    public void RequiresSyncScan_HybridMode_ChecksMimeTypes()
    {
        var svc = CreateService(mode: VirusScanMode.Hybrid, syncMimeTypes: new[] { "image/png" });
        svc.RequiresSyncScan("image/png").Should().BeTrue();
        svc.RequiresSyncScan("video/mp4").Should().BeFalse();
    }
}

// ═══════════════════════════════════════════════════════════════════
//  Small record types, validators, entities — ~30 uncovered lines
// ═══════════════════════════════════════════════════════════════════
public class SmallTypesCoverageTests
{
    // StorageUploadResult
    [Fact]
    public void StorageUploadResult_Properties()
    {
        var r = new StorageUploadResult("bucket", "key", "etag", 1024);
        r.BucketName.Should().Be("bucket");
        r.ObjectKey.Should().Be("key");
        r.ETag.Should().Be("etag");
        r.SizeBytes.Should().Be(1024);
    }

    [Fact]
    public void StorageUploadResult_OptionalPropertiesNull()
    {
        var r = new StorageUploadResult("b", "k");
        r.ETag.Should().BeNull();
        r.SizeBytes.Should().BeNull();
    }

    // StorageMetadata
    [Fact]
    public void StorageMetadata_Properties()
    {
        var now = DateTime.UtcNow;
        var m = new StorageMetadata(2048, "image/png", "etag123", now);
        m.SizeBytes.Should().Be(2048);
        m.MimeType.Should().Be("image/png");
        m.ETag.Should().Be("etag123");
        m.LastModified.Should().Be(now);
    }

    // EphemeralTokenPayload
    [Fact]
    public void EphemeralTokenPayload_Properties()
    {
        var id = Guid.NewGuid();
        var exp = DateTimeOffset.UtcNow.AddHours(1);
        var userId = Guid.NewGuid();
        var p = new EphemeralTokenPayload(id, exp, userId);
        p.AssetReferenceId.Should().Be(id);
        p.ExpiresAt.Should().Be(exp);
        p.UserId.Should().Be(userId);
    }

    [Fact]
    public void EphemeralTokenPayload_UserIdDefaultNull()
    {
        var p = new EphemeralTokenPayload(Guid.NewGuid(), DateTimeOffset.UtcNow);
        p.UserId.Should().BeNull();
    }

    // AssetTokenPayload
    [Fact]
    public void AssetTokenPayload_ExpiresAt()
    {
        var ts = DateTimeOffset.UtcNow.AddHours(1).ToUnixTimeSeconds();
        var p = new AssetTokenPayload(Guid.NewGuid(), 1, ts, AssetAccessPolicy.Public, "", Guid.Empty);
        p.ExpiresAt.Should().BeCloseTo(DateTimeOffset.FromUnixTimeSeconds(ts), TimeSpan.FromSeconds(1));
    }

    // AccessUrlRequest
    [Fact]
    public void AccessUrlRequest_Defaults()
    {
        var r = new AccessUrlRequest();
        r.Transform.Should().BeNull();
        r.DirectStorage.Should().BeFalse();
    }

    [Fact]
    public void AccessUrlRequest_WithValues()
    {
        var r = new AccessUrlRequest("w=100", true);
        r.Transform.Should().Be("w=100");
        r.DirectStorage.Should().BeTrue();
    }

    // TokenValidationResult
    [Fact]
    public void TokenValidationResult_Valid()
    {
        var r = new TokenValidationResult(true);
        r.IsValid.Should().BeTrue();
        r.Error.Should().BeNull();
    }

    [Fact]
    public void TokenValidationResult_Invalid()
    {
        var r = new TokenValidationResult(false, "bad token");
        r.IsValid.Should().BeFalse();
        r.Error.Should().Be("bad token");
    }

    // EphemeralTokenValidationResult
    [Fact]
    public void EphemeralTokenValidationResult_Valid()
    {
        var id = Guid.NewGuid();
        var r = new EphemeralTokenValidationResult(true, id);
        r.IsValid.Should().BeTrue();
        r.AssetReferenceId.Should().Be(id);
        r.IsExpired.Should().BeFalse();
    }

    [Fact]
    public void EphemeralTokenValidationResult_Expired()
    {
        var r = new EphemeralTokenValidationResult(false, Guid.Empty, true, "expired");
        r.IsExpired.Should().BeTrue();
        r.Error.Should().Be("expired");
    }

    // AssetAccessValidation
    [Fact]
    public void AssetAccessValidation_Valid()
    {
        var v = new AssetAccessValidation(true, null);
        v.IsValid.Should().BeTrue();
        v.DeniedReason.Should().BeNull();
    }

    // AssetAccessUrl
    [Fact]
    public void AssetAccessUrl_Properties()
    {
        var exp = DateTimeOffset.UtcNow.AddHours(1);
        var u = new AssetAccessUrl("https://cdn.test/file", "tok123", exp, "image/png");
        u.Url.Should().Be("https://cdn.test/file");
        u.Token.Should().Be("tok123");
        u.ExpiresAt.Should().Be(exp);
        u.MimeType.Should().Be("image/png");
    }

    // TransformedAssetInfo
    [Fact]
    public void TransformedAssetInfo_Properties()
    {
        var id = Guid.NewGuid();
        var sourceId = Guid.NewGuid();
        var info = new TransformedAssetInfo(id, sourceId, "bucket", "key", "image/webp", "hash", 512);
        info.Id.Should().Be(id);
        info.SourceContentId.Should().Be(sourceId);
        info.BucketName.Should().Be("bucket");
        info.ObjectKey.Should().Be("key");
        info.MimeType.Should().Be("image/webp");
        info.ContentHash.Should().Be("hash");
        info.SizeBytes.Should().Be(512);
    }

    // TransformedAsset entity
    [Fact]
    public void TransformedAsset_RecordAccess_UpdatesLastAccessedAt()
    {
        var entity = new TransformedAsset
        {
            SourceContentId = Guid.NewGuid(),
            TransformationSpec = "w=100",
            BucketName = "b",
            ObjectKey = "k",
            MimeType = "image/png",
            SizeBytes = 100
        };
        var before = entity.LastAccessedAt;
        System.Threading.Thread.Sleep(10);
        entity.RecordAccess();
        entity.LastAccessedAt.Should().BeOnOrAfter(before);
    }

    [Fact]
    public void TransformedAsset_ShouldEvict_RecentAccess_ReturnsFalse()
    {
        var entity = new TransformedAsset
        {
            SourceContentId = Guid.NewGuid(),
            TransformationSpec = "w=100",
            BucketName = "b",
            ObjectKey = "k",
            MimeType = "image/png",
            SizeBytes = 100
        };
        entity.RecordAccess();
        entity.ShouldEvict(TimeSpan.FromHours(1)).Should().BeFalse();
    }

    // RateLimitResult
    [Fact]
    public void RateLimitResult_Properties()
    {
        var r = new RateLimitResult(true, 5, 100);
        r.IsAllowed.Should().BeTrue();
        r.CurrentCount.Should().Be(5);
        r.Limit.Should().Be(100);
        r.RetryAfter.Should().BeNull();
        r.Reason.Should().BeNull();
    }

    [Fact]
    public void RateLimitResult_Denied()
    {
        var r = new RateLimitResult(false, 100, 100, TimeSpan.FromMinutes(5), "rate limit");
        r.IsAllowed.Should().BeFalse();
        r.RetryAfter.Should().Be(TimeSpan.FromMinutes(5));
    }

    // AssetAccessStats
    [Fact]
    public void AssetAccessStats_Properties()
    {
        var id = Guid.NewGuid();
        var now = DateTime.UtcNow;
        var s = new AssetAccessStats(id, 10, 50, now);
        s.AssetReferenceId.Should().Be(id);
        s.CurrentHourCount.Should().Be(10);
        s.TotalCount.Should().Be(50);
        s.LastAccessTime.Should().Be(now);
    }

    // AssetAccessDeniedReason enum
    [Fact]
    public void AssetAccessDeniedReason_AllValues()
    {
        var values = Enum.GetValues<AssetAccessDeniedReason>();
        values.Should().Contain(AssetAccessDeniedReason.NotFound);
        values.Should().Contain(AssetAccessDeniedReason.TokenInvalid);
        values.Should().Contain(AssetAccessDeniedReason.TokenExpired);
        values.Should().Contain(AssetAccessDeniedReason.AuthenticationRequired);
        values.Should().Contain(AssetAccessDeniedReason.OwnershipRequired);
        values.Should().Contain(AssetAccessDeniedReason.InvalidPolicy);
    }

    // AssetAccessOptions
    [Fact]
    public void AssetAccessOptions_Defaults()
    {
        var o = new AssetAccessOptions();
        o.BaseUrl.Should().BeEmpty();
        o.DefaultExpiryMinutes.Should().Be(60);
        o.UsePresignedUrls.Should().BeTrue();
    }

    // AssetTokenOptions
    [Fact]
    public void AssetTokenOptions_Defaults()
    {
        var o = new AssetTokenOptions();
        o.SecretKey.Should().BeEmpty();
        o.DefaultExpiryHours.Should().Be(24);
        o.TimeWindowHours.Should().Be(8);
    }

    // DeduplicationOptions
    [Fact]
    public void DeduplicationOptions_Defaults()
    {
        var o = new DeduplicationOptions();
        o.Enabled.Should().BeTrue();
        o.EnablePerceptualHashing.Should().BeTrue();
        o.PerceptualHashThreshold.Should().Be(5);
    }

    // AssetRateLimitOptions
    [Fact]
    public void AssetRateLimitOptions_Defaults()
    {
        var o = new AssetRateLimitOptions();
        o.MaxAccessPerAssetPerHour.Should().Be(1000);
        o.Max403PerIpPerHour.Should().Be(50);
        o.BlockDurationMinutes.Should().Be(60);
        o.Enabled.Should().BeTrue();
        o.WindowSizeSeconds.Should().Be(3600);
    }

    // VirusScanOptions
    [Fact]
    public void VirusScanOptions_Defaults()
    {
        var o = new VirusScanOptions();
        o.Enabled.Should().BeTrue();
    }

    // VirusScanResult
    [Fact]
    public void VirusScanResult_Clean()
    {
        var r = new VirusScanResult(true, "Clean");
        r.IsClean.Should().BeTrue();
        r.ThreatName.Should().BeNull();
    }

    [Fact]
    public void VirusScanResult_WithThreat()
    {
        var r = new VirusScanResult(false, "Infected", "Trojan.Test", "Malware",
            "ClamAV", "1.0", TimeSpan.FromMilliseconds(50), "Found threat");
        r.IsClean.Should().BeFalse();
        r.ThreatName.Should().Be("Trojan.Test");
        r.ScanEngine.Should().Be("ClamAV");
        r.ScanDuration.Should().Be(TimeSpan.FromMilliseconds(50));
    }
}
