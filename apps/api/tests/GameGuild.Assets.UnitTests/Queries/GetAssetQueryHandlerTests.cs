using GameGuild.Assets.Queries;

namespace GameGuild.Assets.UnitTests.Queries;

public class GetAssetQueryHandlerTests
{
    private readonly Mock<IAssetReferenceRepository> _referenceRepositoryMock;
    private readonly Mock<IAssetAccessService> _accessServiceMock;
    private readonly GetAssetHandler _handler;

    public GetAssetQueryHandlerTests()
    {
        _referenceRepositoryMock = new Mock<IAssetReferenceRepository>();
        _accessServiceMock = new Mock<IAssetAccessService>();
        _handler = new GetAssetHandler(
            _referenceRepositoryMock.Object,
            _accessServiceMock.Object);
    }

    [Fact]
    public async Task Handle_AssetNotFound_ReturnsNull()
    {
        // Arrange
        var query = new GetAssetQuery(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            IncludeContentDetails: false);

        _referenceRepositoryMock
            .Setup(x => x.GetByIdAsync(query.AssetReferenceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((AssetReference?)null);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task Handle_AccessDenied_ReturnsNull()
    {
        // Arrange
        var assetReferenceId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var query = new GetAssetQuery(assetReferenceId, userId, tenantId);

        var reference = CreateAssetReference(assetReferenceId);

        _referenceRepositoryMock
            .Setup(x => x.GetByIdAsync(assetReferenceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(reference);

        _accessServiceMock
            .Setup(x => x.ValidateAccessAsync(assetReferenceId, userId, tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AssetAccessValidation(false, AssetAccessDeniedReason.OwnershipRequired));

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task Handle_AccessGranted_ReturnsAssetDto()
    {
        // Arrange
        var assetReferenceId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var query = new GetAssetQuery(assetReferenceId, userId, tenantId);

        var reference = CreateAssetReference(assetReferenceId);

        _referenceRepositoryMock
            .Setup(x => x.GetByIdAsync(assetReferenceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(reference);

        _accessServiceMock
            .Setup(x => x.ValidateAccessAsync(assetReferenceId, userId, tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AssetAccessValidation(true, null));

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(assetReferenceId);
        result.Content.Should().BeNull();
    }

    [Fact]
    public async Task Handle_IncludeContentDetails_UsesGetByIdWithContent()
    {
        // Arrange
        var assetReferenceId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var query = new GetAssetQuery(assetReferenceId, userId, tenantId, IncludeContentDetails: true);

        var contentId = Guid.NewGuid();
        var reference = CreateAssetReferenceWithContent(assetReferenceId, contentId);

        _referenceRepositoryMock
            .Setup(x => x.GetByIdWithContentAsync(assetReferenceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(reference);

        _accessServiceMock
            .Setup(x => x.ValidateAccessAsync(assetReferenceId, userId, tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AssetAccessValidation(true, null));

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Content.Should().NotBeNull();
        result.Content!.Id.Should().Be(contentId);
        _referenceRepositoryMock.Verify(x => x.GetByIdWithContentAsync(assetReferenceId, It.IsAny<CancellationToken>()), Times.Once);
        _referenceRepositoryMock.Verify(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithContentDetails_ReturnsContentDto()
    {
        // Arrange
        var assetReferenceId = Guid.NewGuid();
        var contentId = Guid.NewGuid();
        var query = new GetAssetQuery(assetReferenceId, Guid.NewGuid(), Guid.NewGuid(), IncludeContentDetails: true);

        var reference = CreateAssetReferenceWithContent(assetReferenceId, contentId);

        _referenceRepositoryMock
            .Setup(x => x.GetByIdWithContentAsync(assetReferenceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(reference);

        _accessServiceMock
            .Setup(x => x.ValidateAccessAsync(It.IsAny<Guid>(), It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AssetAccessValidation(true, null));

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Content.Should().NotBeNull();
        result.Content!.MimeType.Should().Be("image/png");
        result.Content.SizeBytes.Should().Be(1024);
        result.Content.Width.Should().Be(100);
        result.Content.Height.Should().Be(100);
    }

    [Fact]
    public async Task Handle_ReturnsCorrectAssetProperties()
    {
        // Arrange
        var assetReferenceId = Guid.NewGuid();
        var contentId = Guid.NewGuid();
        var createdByUserId = Guid.NewGuid();
        var parentResourceId = Guid.NewGuid();
        var query = new GetAssetQuery(assetReferenceId, Guid.NewGuid(), Guid.NewGuid());

        var reference = new AssetReference(
            contentId,
            createdByUserId,
            "Test Asset Name",
            AssetAccessPolicy.Public,
            "Course",
            parentResourceId);
        
        typeof(AssetReference).GetProperty("Id")?.SetValue(reference, assetReferenceId);
        typeof(AssetReference).GetProperty("AccessCount")?.SetValue(reference, 42L);
        typeof(AssetReference).GetProperty("LastAccessedAt")?.SetValue(reference, DateTime.UtcNow.AddDays(-1));

        _referenceRepositoryMock
            .Setup(x => x.GetByIdAsync(assetReferenceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(reference);

        _accessServiceMock
            .Setup(x => x.ValidateAccessAsync(It.IsAny<Guid>(), It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AssetAccessValidation(true, null));

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(assetReferenceId);
        result.AssetContentId.Should().Be(contentId);
        result.CreatedByUserId.Should().Be(createdByUserId);
        result.DisplayName.Should().Be("Test Asset Name");
        result.AccessPolicy.Should().Be(AssetAccessPolicy.Public);
        result.ParentResourceType.Should().Be("Course");
        result.ParentResourceId.Should().Be(parentResourceId);
        result.AccessCount.Should().Be(42);
    }

    [Fact]
    public async Task Handle_NullUserId_ValidatesWithNullUser()
    {
        // Arrange
        var assetReferenceId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var query = new GetAssetQuery(assetReferenceId, UserId: null, TenantId: tenantId);

        var reference = CreateAssetReference(assetReferenceId);

        _referenceRepositoryMock
            .Setup(x => x.GetByIdAsync(assetReferenceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(reference);

        _accessServiceMock
            .Setup(x => x.ValidateAccessAsync(assetReferenceId, null, tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AssetAccessValidation(true, null));

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        _accessServiceMock.Verify(
            x => x.ValidateAccessAsync(assetReferenceId, null, tenantId, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    private static AssetReference CreateAssetReference(Guid id)
    {
        var reference = new AssetReference(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Test Asset",
            AssetAccessPolicy.Private,
            null,
            null);
        
        typeof(AssetReference).GetProperty("Id")?.SetValue(reference, id);
        
        return reference;
    }

    private static AssetReference CreateAssetReferenceWithContent(Guid referenceId, Guid contentId)
    {
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
            Guid.NewGuid(),
            "Test Asset",
            AssetAccessPolicy.Private,
            null,
            null);
        
        typeof(AssetReference).GetProperty("Id")?.SetValue(reference, referenceId);
        typeof(AssetReference).GetProperty("Content")?.SetValue(reference, content);
        
        return reference;
    }
}
