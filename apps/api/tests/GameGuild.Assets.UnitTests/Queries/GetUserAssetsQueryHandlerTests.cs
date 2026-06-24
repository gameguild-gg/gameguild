using GameGuild.Assets.Queries;

namespace GameGuild.Assets.UnitTests.Queries;

public class GetUserAssetsQueryHandlerTests
{
    private readonly Mock<IAssetReferenceRepository> _referenceRepositoryMock;
    private readonly GetUserAssetsHandler _handler;

    public GetUserAssetsQueryHandlerTests()
    {
        _referenceRepositoryMock = new Mock<IAssetReferenceRepository>();
        _handler = new GetUserAssetsHandler(_referenceRepositoryMock.Object);
    }

    [Fact]
    public async Task Handle_NoAssets_ReturnsEmptyList()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var query = new GetUserAssetsQuery(userId, Guid.NewGuid());

        _referenceRepositoryMock
            .Setup(x => x.GetByUserAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AssetReference>());

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_MultipleAssets_ReturnsAllAssets()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var query = new GetUserAssetsQuery(userId, Guid.NewGuid());

        var references = new List<AssetReference>
        {
            CreateAssetReference(Guid.NewGuid(), userId, "Asset 1"),
            CreateAssetReference(Guid.NewGuid(), userId, "Asset 2"),
            CreateAssetReference(Guid.NewGuid(), userId, "Asset 3")
        };

        _referenceRepositoryMock
            .Setup(x => x.GetByUserAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(references);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(3);
    }

    [Fact]
    public async Task Handle_WithSkip_SkipsSpecifiedNumber()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var query = new GetUserAssetsQuery(userId, Guid.NewGuid(), Skip: 2);

        var references = new List<AssetReference>
        {
            CreateAssetReference(Guid.NewGuid(), userId, "Asset 1"),
            CreateAssetReference(Guid.NewGuid(), userId, "Asset 2"),
            CreateAssetReference(Guid.NewGuid(), userId, "Asset 3"),
            CreateAssetReference(Guid.NewGuid(), userId, "Asset 4")
        };

        _referenceRepositoryMock
            .Setup(x => x.GetByUserAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(references);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result[0].DisplayName.Should().Be("Asset 3");
        result[1].DisplayName.Should().Be("Asset 4");
    }

    [Fact]
    public async Task Handle_WithTake_TakesSpecifiedNumber()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var query = new GetUserAssetsQuery(userId, Guid.NewGuid(), Take: 2);

        var references = new List<AssetReference>
        {
            CreateAssetReference(Guid.NewGuid(), userId, "Asset 1"),
            CreateAssetReference(Guid.NewGuid(), userId, "Asset 2"),
            CreateAssetReference(Guid.NewGuid(), userId, "Asset 3"),
            CreateAssetReference(Guid.NewGuid(), userId, "Asset 4")
        };

        _referenceRepositoryMock
            .Setup(x => x.GetByUserAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(references);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result[0].DisplayName.Should().Be("Asset 1");
        result[1].DisplayName.Should().Be("Asset 2");
    }

    [Fact]
    public async Task Handle_WithSkipAndTake_AppliesBothPagination()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var query = new GetUserAssetsQuery(userId, Guid.NewGuid(), Skip: 1, Take: 2);

        var references = new List<AssetReference>
        {
            CreateAssetReference(Guid.NewGuid(), userId, "Asset 1"),
            CreateAssetReference(Guid.NewGuid(), userId, "Asset 2"),
            CreateAssetReference(Guid.NewGuid(), userId, "Asset 3"),
            CreateAssetReference(Guid.NewGuid(), userId, "Asset 4")
        };

        _referenceRepositoryMock
            .Setup(x => x.GetByUserAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(references);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result[0].DisplayName.Should().Be("Asset 2");
        result[1].DisplayName.Should().Be("Asset 3");
    }

    [Fact]
    public async Task Handle_WithContent_IncludesContentDto()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var query = new GetUserAssetsQuery(userId, Guid.NewGuid());

        var contentId = Guid.NewGuid();
        var reference = CreateAssetReferenceWithContent(Guid.NewGuid(), userId, contentId);

        _referenceRepositoryMock
            .Setup(x => x.GetByUserAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AssetReference> { reference });

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        result[0].Content.Should().NotBeNull();
        result[0].Content!.Id.Should().Be(contentId);
        result[0].Content.MimeType.Should().Be("image/png");
    }

    [Fact]
    public async Task Handle_WithoutContent_ContentIsNull()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var query = new GetUserAssetsQuery(userId, Guid.NewGuid());

        var reference = CreateAssetReference(Guid.NewGuid(), userId, "Asset 1");

        _referenceRepositoryMock
            .Setup(x => x.GetByUserAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AssetReference> { reference });

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        result[0].Content.Should().BeNull();
    }

    [Fact]
    public async Task Handle_MapsAllAssetProperties()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var assetId = Guid.NewGuid();
        var contentId = Guid.NewGuid();
        var parentResourceId = Guid.NewGuid();
        var query = new GetUserAssetsQuery(userId, Guid.NewGuid());

        var reference = new AssetReference(
            contentId,
            userId,
            "Test Asset Name",
            AssetAccessPolicy.Public,
            "Project",
            parentResourceId);
        
        typeof(AssetReference).GetProperty("Id")?.SetValue(reference, assetId);
        typeof(AssetReference).GetProperty("AccessCount")?.SetValue(reference, 100L);

        _referenceRepositoryMock
            .Setup(x => x.GetByUserAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AssetReference> { reference });

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        var asset = result[0];
        asset.Id.Should().Be(assetId);
        asset.AssetContentId.Should().Be(contentId);
        asset.CreatedByUserId.Should().Be(userId);
        asset.DisplayName.Should().Be("Test Asset Name");
        asset.AccessPolicy.Should().Be(AssetAccessPolicy.Public);
        asset.ParentResourceType.Should().Be("Project");
        asset.ParentResourceId.Should().Be(parentResourceId);
        asset.AccessCount.Should().Be(100);
    }

    [Fact]
    public async Task Handle_SkipBeyondCount_ReturnsEmptyList()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var query = new GetUserAssetsQuery(userId, Guid.NewGuid(), Skip: 100);

        var references = new List<AssetReference>
        {
            CreateAssetReference(Guid.NewGuid(), userId, "Asset 1"),
            CreateAssetReference(Guid.NewGuid(), userId, "Asset 2")
        };

        _referenceRepositoryMock
            .Setup(x => x.GetByUserAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(references);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_NullTenantId_StillReturnAssets()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var query = new GetUserAssetsQuery(userId, TenantId: null);

        var references = new List<AssetReference>
        {
            CreateAssetReference(Guid.NewGuid(), userId, "Asset 1")
        };

        _referenceRepositoryMock
            .Setup(x => x.GetByUserAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(references);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(1);
    }

    private static AssetReference CreateAssetReference(Guid id, Guid createdByUserId, string displayName)
    {
        var reference = new AssetReference(
            Guid.NewGuid(),
            createdByUserId,
            displayName,
            AssetAccessPolicy.Private,
            null,
            null);
        
        typeof(AssetReference).GetProperty("Id")?.SetValue(reference, id);
        
        return reference;
    }

    private static AssetReference CreateAssetReferenceWithContent(Guid referenceId, Guid createdByUserId, Guid contentId)
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
            createdByUserId,
            "Test Asset",
            AssetAccessPolicy.Private,
            null,
            null);
        
        typeof(AssetReference).GetProperty("Id")?.SetValue(reference, referenceId);
        typeof(AssetReference).GetProperty("Content")?.SetValue(reference, content);
        
        return reference;
    }
}
