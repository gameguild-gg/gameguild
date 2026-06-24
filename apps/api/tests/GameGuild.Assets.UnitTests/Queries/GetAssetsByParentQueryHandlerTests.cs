using GameGuild.Assets.Queries;

namespace GameGuild.Assets.UnitTests.Queries;

public class GetAssetsByParentQueryHandlerTests
{
    private readonly Mock<IAssetReferenceRepository> _referenceRepositoryMock;
    private readonly Mock<IAssetAccessService> _accessServiceMock;
    private readonly GetAssetsByParentHandler _handler;

    public GetAssetsByParentQueryHandlerTests()
    {
        _referenceRepositoryMock = new Mock<IAssetReferenceRepository>();
        _accessServiceMock = new Mock<IAssetAccessService>();
        _handler = new GetAssetsByParentHandler(
            _referenceRepositoryMock.Object,
            _accessServiceMock.Object);
    }

    [Fact]
    public async Task Handle_NoAssetsForParent_ReturnsEmptyList()
    {
        // Arrange
        var parentResourceId = Guid.NewGuid();
        var query = new GetAssetsByParentQuery(
            "Course",
            parentResourceId,
            Guid.NewGuid(),
            Guid.NewGuid());

        _referenceRepositoryMock
            .Setup(x => x.GetByParentAsync("Course", parentResourceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AssetReference>());

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_AllAssetsAccessible_ReturnsAll()
    {
        // Arrange
        var parentResourceId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var query = new GetAssetsByParentQuery("Project", parentResourceId, userId, tenantId);

        var references = new List<AssetReference>
        {
            CreateAssetReference(Guid.NewGuid(), "Asset 1"),
            CreateAssetReference(Guid.NewGuid(), "Asset 2"),
            CreateAssetReference(Guid.NewGuid(), "Asset 3")
        };

        _referenceRepositoryMock
            .Setup(x => x.GetByParentAsync("Project", parentResourceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(references);

        _accessServiceMock
            .Setup(x => x.ValidateAccessAsync(It.IsAny<Guid>(), userId, tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AssetAccessValidation(true, null));

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(3);
    }

    [Fact]
    public async Task Handle_SomeAssetsNotAccessible_ReturnsOnlyAccessible()
    {
        // Arrange
        var parentResourceId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var query = new GetAssetsByParentQuery("Course", parentResourceId, userId, tenantId);

        var accessibleAssetId = Guid.NewGuid();
        var inaccessibleAssetId = Guid.NewGuid();

        var references = new List<AssetReference>
        {
            CreateAssetReference(accessibleAssetId, "Accessible Asset"),
            CreateAssetReference(inaccessibleAssetId, "Private Asset")
        };

        _referenceRepositoryMock
            .Setup(x => x.GetByParentAsync("Course", parentResourceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(references);

        _accessServiceMock
            .Setup(x => x.ValidateAccessAsync(accessibleAssetId, userId, tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AssetAccessValidation(true, null));

        _accessServiceMock
            .Setup(x => x.ValidateAccessAsync(inaccessibleAssetId, userId, tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AssetAccessValidation(false, AssetAccessDeniedReason.OwnershipRequired));

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        result[0].Id.Should().Be(accessibleAssetId);
        result[0].DisplayName.Should().Be("Accessible Asset");
    }

    [Fact]
    public async Task Handle_NoneAccessible_ReturnsEmptyList()
    {
        // Arrange
        var parentResourceId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var query = new GetAssetsByParentQuery("Course", parentResourceId, userId, tenantId);

        var references = new List<AssetReference>
        {
            CreateAssetReference(Guid.NewGuid(), "Private Asset 1"),
            CreateAssetReference(Guid.NewGuid(), "Private Asset 2")
        };

        _referenceRepositoryMock
            .Setup(x => x.GetByParentAsync("Course", parentResourceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(references);

        _accessServiceMock
            .Setup(x => x.ValidateAccessAsync(It.IsAny<Guid>(), userId, tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AssetAccessValidation(false, AssetAccessDeniedReason.OwnershipRequired));

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_WithContent_IncludesContentDto()
    {
        // Arrange
        var parentResourceId = Guid.NewGuid();
        var contentId = Guid.NewGuid();
        var query = new GetAssetsByParentQuery("Project", parentResourceId, Guid.NewGuid(), Guid.NewGuid());

        var reference = CreateAssetReferenceWithContent(Guid.NewGuid(), contentId);

        _referenceRepositoryMock
            .Setup(x => x.GetByParentAsync("Project", parentResourceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AssetReference> { reference });

        _accessServiceMock
            .Setup(x => x.ValidateAccessAsync(It.IsAny<Guid>(), It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AssetAccessValidation(true, null));

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        result[0].Content.Should().NotBeNull();
        result[0].Content!.Id.Should().Be(contentId);
        result[0].Content.MimeType.Should().Be("image/png");
        result[0].Content.SizeBytes.Should().Be(1024);
    }

    [Fact]
    public async Task Handle_WithoutContent_ContentIsNull()
    {
        // Arrange
        var parentResourceId = Guid.NewGuid();
        var query = new GetAssetsByParentQuery("Project", parentResourceId, Guid.NewGuid(), Guid.NewGuid());

        var reference = CreateAssetReference(Guid.NewGuid(), "Asset without content");

        _referenceRepositoryMock
            .Setup(x => x.GetByParentAsync("Project", parentResourceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AssetReference> { reference });

        _accessServiceMock
            .Setup(x => x.ValidateAccessAsync(It.IsAny<Guid>(), It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AssetAccessValidation(true, null));

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        result[0].Content.Should().BeNull();
    }

    [Fact]
    public async Task Handle_ValidatesAccessForEachAsset()
    {
        // Arrange
        var parentResourceId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var query = new GetAssetsByParentQuery("Course", parentResourceId, userId, tenantId);

        var assetId1 = Guid.NewGuid();
        var assetId2 = Guid.NewGuid();

        var references = new List<AssetReference>
        {
            CreateAssetReference(assetId1, "Asset 1"),
            CreateAssetReference(assetId2, "Asset 2")
        };

        _referenceRepositoryMock
            .Setup(x => x.GetByParentAsync("Course", parentResourceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(references);

        _accessServiceMock
            .Setup(x => x.ValidateAccessAsync(It.IsAny<Guid>(), userId, tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AssetAccessValidation(true, null));

        // Act
        await _handler.Handle(query, CancellationToken.None);

        // Assert
        _accessServiceMock.Verify(
            x => x.ValidateAccessAsync(assetId1, userId, tenantId, It.IsAny<CancellationToken>()),
            Times.Once);
        _accessServiceMock.Verify(
            x => x.ValidateAccessAsync(assetId2, userId, tenantId, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_NullUserId_ValidatesWithNullUser()
    {
        // Arrange
        var parentResourceId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var query = new GetAssetsByParentQuery("Course", parentResourceId, UserId: null, TenantId: tenantId);

        var assetId = Guid.NewGuid();
        var references = new List<AssetReference>
        {
            CreateAssetReference(assetId, "Public Asset")
        };

        _referenceRepositoryMock
            .Setup(x => x.GetByParentAsync("Course", parentResourceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(references);

        _accessServiceMock
            .Setup(x => x.ValidateAccessAsync(assetId, null, tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AssetAccessValidation(true, null));

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        _accessServiceMock.Verify(
            x => x.ValidateAccessAsync(assetId, null, tenantId, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Theory]
    [InlineData("Course")]
    [InlineData("Project")]
    [InlineData("Program")]
    [InlineData("Assignment")]
    public async Task Handle_DifferentParentResourceTypes_QueriesCorrectly(string parentResourceType)
    {
        // Arrange
        var parentResourceId = Guid.NewGuid();
        var query = new GetAssetsByParentQuery(parentResourceType, parentResourceId, Guid.NewGuid(), Guid.NewGuid());

        _referenceRepositoryMock
            .Setup(x => x.GetByParentAsync(parentResourceType, parentResourceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AssetReference>());

        // Act
        await _handler.Handle(query, CancellationToken.None);

        // Assert
        _referenceRepositoryMock.Verify(
            x => x.GetByParentAsync(parentResourceType, parentResourceId, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    private static AssetReference CreateAssetReference(Guid id, string displayName)
    {
        var reference = new AssetReference(
            Guid.NewGuid(),
            Guid.NewGuid(),
            displayName,
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
