using GameGuild.Assets.Commands;

namespace GameGuild.Assets.UnitTests.Commands;

public class DeleteAssetCommandHandlerTests
{
    private readonly Mock<IAssetReferenceRepository> _referenceRepositoryMock;
    private readonly Mock<IAssetContentRepository> _contentRepositoryMock;
    private readonly DeleteAssetHandler _handler;

    public DeleteAssetCommandHandlerTests()
    {
        _referenceRepositoryMock = new Mock<IAssetReferenceRepository>();
        _contentRepositoryMock = new Mock<IAssetContentRepository>();
        _handler = new DeleteAssetHandler(
            _referenceRepositoryMock.Object,
            _contentRepositoryMock.Object);
    }

    [Fact]
    public async Task Handle_ReferenceNotFound_ReturnsFailure()
    {
        // Arrange
        var command = new DeleteAssetCommand(Guid.NewGuid(), Guid.NewGuid());
        _referenceRepositoryMock
            .Setup(x => x.GetByIdAsync(command.AssetReferenceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((AssetReference?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeFalse();
        result.ContentMarkedForDeletion.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_NotOwnerWithoutForceDelete_ReturnsFailure()
    {
        // Arrange
        var assetReferenceId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var contentId = Guid.NewGuid();
        var command = new DeleteAssetCommand(assetReferenceId, userId, ForceDelete: false);
        
        var reference = CreateAssetReference(assetReferenceId, contentId, Guid.NewGuid());
        
        _referenceRepositoryMock
            .Setup(x => x.GetByIdAsync(assetReferenceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(reference);
        _referenceRepositoryMock
            .Setup(x => x.IsOwnedByUserAsync(assetReferenceId, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeFalse();
        result.ContentMarkedForDeletion.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_OwnerDeletesAsset_SuccessAndMarksContentForDeletion()
    {
        // Arrange
        var assetReferenceId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var contentId = Guid.NewGuid();
        var command = new DeleteAssetCommand(assetReferenceId, userId, ForceDelete: false);
        
        var reference = CreateAssetReference(assetReferenceId, contentId, userId);
        var content = CreateAssetContent(contentId, markedForDeletion: true);
        
        _referenceRepositoryMock
            .Setup(x => x.GetByIdAsync(assetReferenceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(reference);
        _referenceRepositoryMock
            .Setup(x => x.IsOwnedByUserAsync(assetReferenceId, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _contentRepositoryMock
            .Setup(x => x.GetByIdAsync(contentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(content);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        result.ContentMarkedForDeletion.Should().BeTrue();
        _referenceRepositoryMock.Verify(x => x.DeleteAsync(assetReferenceId, It.IsAny<CancellationToken>()), Times.Once);
        _contentRepositoryMock.Verify(x => x.DecrementReferenceCountAsync(contentId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_OwnerDeletesAsset_ContentNotMarkedForDeletion()
    {
        // Arrange
        var assetReferenceId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var contentId = Guid.NewGuid();
        var command = new DeleteAssetCommand(assetReferenceId, userId, ForceDelete: false);
        
        var reference = CreateAssetReference(assetReferenceId, contentId, userId);
        var content = CreateAssetContent(contentId, markedForDeletion: false);
        
        _referenceRepositoryMock
            .Setup(x => x.GetByIdAsync(assetReferenceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(reference);
        _referenceRepositoryMock
            .Setup(x => x.IsOwnedByUserAsync(assetReferenceId, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _contentRepositoryMock
            .Setup(x => x.GetByIdAsync(contentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(content);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        result.ContentMarkedForDeletion.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_ForceDeleteByAdmin_SuccessEvenIfNotOwner()
    {
        // Arrange
        var assetReferenceId = Guid.NewGuid();
        var adminUserId = Guid.NewGuid();
        var contentId = Guid.NewGuid();
        var ownerUserId = Guid.NewGuid();
        var command = new DeleteAssetCommand(assetReferenceId, adminUserId, ForceDelete: true);
        
        var reference = CreateAssetReference(assetReferenceId, contentId, ownerUserId);
        var content = CreateAssetContent(contentId, markedForDeletion: false);
        
        _referenceRepositoryMock
            .Setup(x => x.GetByIdAsync(assetReferenceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(reference);
        _contentRepositoryMock
            .Setup(x => x.GetByIdAsync(contentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(content);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        _referenceRepositoryMock.Verify(x => x.IsOwnedByUserAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ContentNullAfterDecrement_ContentNotMarkedForDeletion()
    {
        // Arrange
        var assetReferenceId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var contentId = Guid.NewGuid();
        var command = new DeleteAssetCommand(assetReferenceId, userId, ForceDelete: false);
        
        var reference = CreateAssetReference(assetReferenceId, contentId, userId);
        
        _referenceRepositoryMock
            .Setup(x => x.GetByIdAsync(assetReferenceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(reference);
        _referenceRepositoryMock
            .Setup(x => x.IsOwnedByUserAsync(assetReferenceId, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _contentRepositoryMock
            .Setup(x => x.GetByIdAsync(contentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((AssetContent?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        result.ContentMarkedForDeletion.Should().BeFalse();
    }

    private static AssetReference CreateAssetReference(Guid id, Guid contentId, Guid createdByUserId)
    {
        var reference = new AssetReference(
            contentId,
            createdByUserId,
            "Test Asset",
            AssetAccessPolicy.Private,
            null,
            null);
        
        // Set Id using reflection since it's likely from EntityBase
        typeof(AssetReference).GetProperty("Id")?.SetValue(reference, id);
        
        return reference;
    }

    private static AssetContent CreateAssetContent(Guid id, bool markedForDeletion)
    {
        var content = new AssetContent(
            "test-bucket",
            "test/object.png",
            "abc123hash",
            "image/png",
            1024,
            100,
            100);
        
        typeof(AssetContent).GetProperty("Id")?.SetValue(content, id);
        
        if (markedForDeletion)
        {
            typeof(AssetContent).GetProperty("MarkedForDeletionAt")?.SetValue(content, DateTime.UtcNow);
        }
        
        return content;
    }
}
