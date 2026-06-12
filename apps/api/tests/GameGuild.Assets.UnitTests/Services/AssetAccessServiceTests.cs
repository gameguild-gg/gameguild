using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using GameGuild.CQRS.Models;
using GameGuild.Features;
using GameGuild.Identity.Authorization;
using GameGuild.Identity.Tenants;

namespace GameGuild.Assets.UnitTests.Services;

public class AssetAccessServiceTests
{
    private readonly Mock<IAssetReferenceRepository> _referenceRepositoryMock;
    private readonly Mock<ITransformedAssetRepository> _transformedAssetRepositoryMock;
    private readonly Mock<IAssetStorageService> _storageServiceMock;
    private readonly Mock<IAssetTokenService> _tokenServiceMock;
    private readonly Mock<ITenantMemberRepository> _tenantMemberRepositoryMock;
    private readonly Mock<IFeatureFlagEvaluationService> _featureServiceMock;
    private readonly Mock<IResourcePermissionService> _resourcePermissionServiceMock;
    private readonly Mock<ILogger<AssetAccessService>> _loggerMock;
    private readonly AssetAccessOptions _options;
    private readonly AssetAccessService _service;

    public AssetAccessServiceTests()
    {
        _referenceRepositoryMock = new Mock<IAssetReferenceRepository>();
        _transformedAssetRepositoryMock = new Mock<ITransformedAssetRepository>();
        _storageServiceMock = new Mock<IAssetStorageService>();
        _tokenServiceMock = new Mock<IAssetTokenService>();
        _tenantMemberRepositoryMock = new Mock<ITenantMemberRepository>();
        _featureServiceMock = new Mock<IFeatureFlagEvaluationService>();
        _resourcePermissionServiceMock = new Mock<IResourcePermissionService>();
        _loggerMock = new Mock<ILogger<AssetAccessService>>();
        _options = new AssetAccessOptions
        {
            BaseUrl = "https://cdn.example.com",
            DefaultExpiryMinutes = 60,
            UsePresignedUrls = true
        };

        _service = new AssetAccessService(
            _referenceRepositoryMock.Object,
            _transformedAssetRepositoryMock.Object,
            _storageServiceMock.Object,
            _tokenServiceMock.Object,
            _tenantMemberRepositoryMock.Object,
            _featureServiceMock.Object,
            Options.Create(_options),
            _loggerMock.Object,
            _resourcePermissionServiceMock.Object);
    }

    #region ValidateAccessAsync Tests

    [Fact]
    public async Task ValidateAccessAsync_ReferenceNotFound_ReturnsNotFound()
    {
        // Arrange
        var assetReferenceId = Guid.NewGuid();
        _referenceRepositoryMock
            .Setup(x => x.GetByIdAsync(assetReferenceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((AssetReference?)null);

        // Act
        var result = await _service.ValidateAccessAsync(assetReferenceId, Guid.NewGuid(), Guid.NewGuid());

        // Assert
        result.IsValid.Should().BeFalse();
        result.DeniedReason.Should().Be(AssetAccessDeniedReason.NotFound);
    }

    [Fact]
    public async Task ValidateAccessAsync_DeletedReference_ReturnsNotFound()
    {
        // Arrange
        var assetReferenceId = Guid.NewGuid();
        var reference = CreateAssetReference(assetReferenceId, AssetAccessPolicy.Public);
        // Set DeletedAt to mark as deleted (IsDeleted is computed from DeletedAt)
        typeof(AssetReference).GetProperty("DeletedAt")?.SetValue(reference, DateTime.UtcNow);

        _referenceRepositoryMock
            .Setup(x => x.GetByIdAsync(assetReferenceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(reference);

        // Act
        var result = await _service.ValidateAccessAsync(assetReferenceId, Guid.NewGuid(), Guid.NewGuid());

        // Assert
        result.IsValid.Should().BeFalse();
        result.DeniedReason.Should().Be(AssetAccessDeniedReason.NotFound);
    }

    [Fact]
    public async Task ValidateAccessAsync_PublicAsset_ReturnsValid()
    {
        // Arrange
        var assetReferenceId = Guid.NewGuid();
        var reference = CreateAssetReference(assetReferenceId, AssetAccessPolicy.Public);

        _referenceRepositoryMock
            .Setup(x => x.GetByIdAsync(assetReferenceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(reference);

        // Act
        var result = await _service.ValidateAccessAsync(assetReferenceId, null, null);

        // Assert
        result.IsValid.Should().BeTrue();
        result.DeniedReason.Should().BeNull();
    }

    [Fact]
    public async Task ValidateAccessAsync_UnlistedAsset_ReturnsValid()
    {
        // Arrange
        var assetReferenceId = Guid.NewGuid();
        var reference = CreateAssetReference(assetReferenceId, AssetAccessPolicy.Unlisted);

        _referenceRepositoryMock
            .Setup(x => x.GetByIdAsync(assetReferenceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(reference);

        // Act
        var result = await _service.ValidateAccessAsync(assetReferenceId, null, null);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateAccessAsync_AuthenticatedAsset_NoUser_ReturnsDenied()
    {
        // Arrange
        var assetReferenceId = Guid.NewGuid();
        var reference = CreateAssetReference(assetReferenceId, AssetAccessPolicy.Authenticated);

        _referenceRepositoryMock
            .Setup(x => x.GetByIdAsync(assetReferenceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(reference);

        // Act
        var result = await _service.ValidateAccessAsync(assetReferenceId, userId: null, Guid.NewGuid());

        // Assert
        result.IsValid.Should().BeFalse();
        result.DeniedReason.Should().Be(AssetAccessDeniedReason.AuthenticationRequired);
    }

    [Fact]
    public async Task ValidateAccessAsync_AuthenticatedAsset_WithUser_ReturnsValid()
    {
        // Arrange
        var assetReferenceId = Guid.NewGuid();
        var reference = CreateAssetReference(assetReferenceId, AssetAccessPolicy.Authenticated);

        _referenceRepositoryMock
            .Setup(x => x.GetByIdAsync(assetReferenceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(reference);

        // Act
        var result = await _service.ValidateAccessAsync(assetReferenceId, Guid.NewGuid(), Guid.NewGuid());

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateAccessAsync_OwnerOnlyAsset_NoUser_ReturnsDenied()
    {
        // Arrange
        var assetReferenceId = Guid.NewGuid();
        var reference = CreateAssetReference(assetReferenceId, AssetAccessPolicy.OwnerOnly);

        _referenceRepositoryMock
            .Setup(x => x.GetByIdAsync(assetReferenceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(reference);

        // Act
        var result = await _service.ValidateAccessAsync(assetReferenceId, userId: null, Guid.NewGuid());

        // Assert
        result.IsValid.Should().BeFalse();
        result.DeniedReason.Should().Be(AssetAccessDeniedReason.AuthenticationRequired);
    }

    [Fact]
    public async Task ValidateAccessAsync_OwnerOnlyAsset_NotOwner_ReturnsDenied()
    {
        // Arrange
        var assetReferenceId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var differentUserId = Guid.NewGuid();
        var reference = CreateAssetReference(assetReferenceId, AssetAccessPolicy.OwnerOnly, ownerId);

        _referenceRepositoryMock
            .Setup(x => x.GetByIdAsync(assetReferenceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(reference);

        // Act
        var result = await _service.ValidateAccessAsync(assetReferenceId, differentUserId, Guid.NewGuid());

        // Assert
        result.IsValid.Should().BeFalse();
        result.DeniedReason.Should().Be(AssetAccessDeniedReason.OwnershipRequired);
    }

    [Fact]
    public async Task ValidateAccessAsync_OwnerOnlyAsset_IsOwner_ReturnsValid()
    {
        // Arrange
        var assetReferenceId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var reference = CreateAssetReference(assetReferenceId, AssetAccessPolicy.OwnerOnly, ownerId);

        _referenceRepositoryMock
            .Setup(x => x.GetByIdAsync(assetReferenceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(reference);

        // Act
        var result = await _service.ValidateAccessAsync(assetReferenceId, ownerId, Guid.NewGuid());

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateAccessAsync_InheritedAsset_NoUser_ReturnsDenied()
    {
        // Arrange
        var assetReferenceId = Guid.NewGuid();
        var reference = CreateAssetReference(assetReferenceId, AssetAccessPolicy.Inherited);

        _referenceRepositoryMock
            .Setup(x => x.GetByIdAsync(assetReferenceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(reference);

        // Act
        var result = await _service.ValidateAccessAsync(assetReferenceId, userId: null, Guid.NewGuid());

        // Assert
        result.IsValid.Should().BeFalse();
        result.DeniedReason.Should().Be(AssetAccessDeniedReason.AuthenticationRequired);
    }

    [Fact]
    public async Task ValidateAccessAsync_InheritedAsset_WithUser_ReturnsValid()
    {
        // Arrange
        var assetReferenceId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var parentResourceId = Guid.NewGuid();
        var reference = CreateAssetReference(
            assetReferenceId,
            AssetAccessPolicy.Inherited,
            parentResourceType: "Course",
            parentResourceId: parentResourceId);

        _referenceRepositoryMock
            .Setup(x => x.GetByIdAsync(assetReferenceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(reference);

        _resourcePermissionServiceMock
            .Setup(x => x.HasPermissionAsync(
                It.Is<TenantId>(id => id.Value == tenantId),
                userId,
                "Course",
                parentResourceId.ToString(),
                "read",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _service.ValidateAccessAsync(assetReferenceId, userId, tenantId);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateAccessAsync_InheritedAsset_WithParentResourceAndNoParentPermission_ReturnsDenied()
    {
        // Arrange
        var assetReferenceId = Guid.NewGuid();
        var reference = CreateAssetReference(
            assetReferenceId,
            AssetAccessPolicy.Inherited,
            parentResourceType: "Course",
            parentResourceId: Guid.NewGuid());

        _referenceRepositoryMock
            .Setup(x => x.GetByIdAsync(assetReferenceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(reference);

        // Act
        var result = await _service.ValidateAccessAsync(assetReferenceId, Guid.NewGuid(), Guid.NewGuid());

        // Assert
        result.IsValid.Should().BeFalse();
        result.DeniedReason.Should().Be(AssetAccessDeniedReason.OwnershipRequired);
        _resourcePermissionServiceMock.Verify(x => x.HasPermissionAsync(
            It.IsAny<TenantId>(),
            It.IsAny<Guid>(),
            "Course",
            reference.ParentResourceId!.Value.ToString(),
            "read",
            It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region ValidateToken Tests

    [Fact]
    public void ValidateToken_ValidToken_ReturnsTrue()
    {
        // Arrange
        var assetReferenceId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var token = "valid-token";

        _tokenServiceMock
            .Setup(x => x.ValidateToken(token, assetReferenceId, tenantId))
            .Returns(new AssetTokenPayload(
                assetReferenceId,
                1,
                DateTimeOffset.UtcNow.AddHours(1).ToUnixTimeSeconds(),
                AssetAccessPolicy.Public,
                string.Empty,
                tenantId));

        // Act
        var result = _service.ValidateToken(token, assetReferenceId, tenantId);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void ValidateToken_InvalidToken_ReturnsFalse()
    {
        // Arrange
        var assetReferenceId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var token = "invalid-token";

        _tokenServiceMock
            .Setup(x => x.ValidateToken(token, assetReferenceId, tenantId))
            .Returns((AssetTokenPayload?)null);

        // Act
        var result = _service.ValidateToken(token, assetReferenceId, tenantId);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void ValidateToken_NullTenantId_UsesEmptyGuid()
    {
        // Arrange
        var assetReferenceId = Guid.NewGuid();
        var token = "token";

        _tokenServiceMock
            .Setup(x => x.ValidateToken(token, assetReferenceId, Guid.Empty))
            .Returns(new AssetTokenPayload(
                assetReferenceId,
                1,
                DateTimeOffset.UtcNow.AddHours(1).ToUnixTimeSeconds(),
                AssetAccessPolicy.Public,
                string.Empty,
                Guid.Empty));

        // Act
        var result = _service.ValidateToken(token, assetReferenceId, tenantId: null);

        // Assert
        result.Should().BeTrue();
        _tokenServiceMock.Verify(x => x.ValidateToken(token, assetReferenceId, Guid.Empty), Times.Once);
    }

    #endregion

    #region GenerateDirectStorageUrlAsync Tests

    [Fact]
    public async Task GenerateDirectStorageUrlAsync_ReferenceNotFound_ReturnsNull()
    {
        // Arrange
        var assetReferenceId = Guid.NewGuid();
        _referenceRepositoryMock
            .Setup(x => x.GetByIdWithContentAsync(assetReferenceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((AssetReference?)null);

        // Act
        var result = await _service.GenerateDirectStorageUrlAsync(assetReferenceId, Guid.NewGuid(), Guid.NewGuid());

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GenerateDirectStorageUrlAsync_ContentNull_ReturnsNull()
    {
        // Arrange
        var assetReferenceId = Guid.NewGuid();
        var reference = CreateAssetReference(assetReferenceId, AssetAccessPolicy.Public);
        // Content is null

        _referenceRepositoryMock
            .Setup(x => x.GetByIdWithContentAsync(assetReferenceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(reference);

        // Act
        var result = await _service.GenerateDirectStorageUrlAsync(assetReferenceId, Guid.NewGuid(), Guid.NewGuid());

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GenerateDirectStorageUrlAsync_AccessDenied_ReturnsNull()
    {
        // Arrange
        var assetReferenceId = Guid.NewGuid();
        var reference = CreateAssetReferenceWithContent(assetReferenceId, AssetAccessPolicy.OwnerOnly, Guid.NewGuid());
        
        _referenceRepositoryMock
            .Setup(x => x.GetByIdWithContentAsync(assetReferenceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(reference);
        
        _referenceRepositoryMock
            .Setup(x => x.GetByIdAsync(assetReferenceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(reference);

        // Act - different user than owner
        var result = await _service.GenerateDirectStorageUrlAsync(assetReferenceId, Guid.NewGuid(), Guid.NewGuid());

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GenerateDirectStorageUrlAsync_Success_ReturnsUrl()
    {
        // Arrange
        var assetReferenceId = Guid.NewGuid();
        var reference = CreateAssetReferenceWithContent(assetReferenceId, AssetAccessPolicy.Public, Guid.NewGuid());
        var presignedUrl = "https://s3.example.com/bucket/key?signature=abc";

        _referenceRepositoryMock
            .Setup(x => x.GetByIdWithContentAsync(assetReferenceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(reference);
        
        _referenceRepositoryMock
            .Setup(x => x.GetByIdAsync(assetReferenceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(reference);

        _storageServiceMock
            .Setup(x => x.GeneratePresignedUrlAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<TimeSpan>(),
                true,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(presignedUrl);

        // Act
        var result = await _service.GenerateDirectStorageUrlAsync(assetReferenceId, Guid.NewGuid(), Guid.NewGuid());

        // Assert
        result.Should().NotBeNull();
        result!.Url.Should().Be(presignedUrl);
        result.Token.Should().BeEmpty();
        result.MimeType.Should().Be("image/png");
    }

    [Fact]
    public async Task GenerateDirectStorageUrlAsync_RecordsAccess()
    {
        // Arrange
        var assetReferenceId = Guid.NewGuid();
        var reference = CreateAssetReferenceWithContent(assetReferenceId, AssetAccessPolicy.Public, Guid.NewGuid());

        _referenceRepositoryMock
            .Setup(x => x.GetByIdWithContentAsync(assetReferenceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(reference);
        
        _referenceRepositoryMock
            .Setup(x => x.GetByIdAsync(assetReferenceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(reference);

        _storageServiceMock
            .Setup(x => x.GeneratePresignedUrlAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<TimeSpan>(),
                true,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync("https://example.com");

        // Act
        await _service.GenerateDirectStorageUrlAsync(assetReferenceId, Guid.NewGuid(), Guid.NewGuid());

        // Assert
        _referenceRepositoryMock.Verify(
            x => x.RecordAccessAsync(assetReferenceId, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    #region Helper Methods

    private static AssetReference CreateAssetReference(
        Guid id,
        AssetAccessPolicy policy,
        Guid? createdByUserId = null,
        string? parentResourceType = null,
        Guid? parentResourceId = null)
    {
        var reference = new AssetReference(
            Guid.NewGuid(),
            createdByUserId ?? Guid.NewGuid(),
            "Test Asset",
            policy,
            parentResourceType,
            parentResourceId);
        
        typeof(AssetReference).GetProperty("Id")?.SetValue(reference, id);
        
        return reference;
    }

    private static AssetReference CreateAssetReferenceWithContent(Guid id, AssetAccessPolicy policy, Guid createdByUserId)
    {
        var contentId = Guid.NewGuid();
        var content = new AssetContent(
            "test-bucket",
            "test/object.png",
            "abc123hash",
            "image/png",
            1024,
            100,
            100);
        typeof(AssetContent).GetProperty("Id")?.SetValue(content, contentId);

        var reference = new AssetReference(
            contentId,
            createdByUserId,
            "Test Asset",
            policy,
            null,
            null);
        typeof(AssetReference).GetProperty("Id")?.SetValue(reference, id);
        typeof(AssetReference).GetProperty("Content")?.SetValue(reference, content);
        
        return reference;
    }

    #endregion
}
