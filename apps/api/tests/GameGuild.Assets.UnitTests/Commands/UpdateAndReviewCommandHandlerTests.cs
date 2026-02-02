using GameGuild.Assets.Commands;

namespace GameGuild.Assets.UnitTests.Commands;

public class UpdateAssetCommandHandlerTests
{
    private readonly Mock<IAssetReferenceRepository> _referenceRepositoryMock;
    private readonly UpdateAssetHandler _handler;

    public UpdateAssetCommandHandlerTests()
    {
        _referenceRepositoryMock = new Mock<IAssetReferenceRepository>();
        _handler = new UpdateAssetHandler(_referenceRepositoryMock.Object);
    }

    [Fact]
    public async Task Handle_AssetNotFound_ReturnsNull()
    {
        // Arrange
        var command = new UpdateAssetCommand(
            Guid.NewGuid(),
            Guid.NewGuid(),
            DisplayName: "New Name");

        _referenceRepositoryMock
            .Setup(x => x.GetByIdAsync(command.AssetReferenceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((AssetReference?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task Handle_NotOwner_ReturnsNull()
    {
        // Arrange
        var assetReferenceId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var ownerUserId = Guid.NewGuid();
        var command = new UpdateAssetCommand(assetReferenceId, userId, DisplayName: "New Name");

        var reference = CreateAssetReference(assetReferenceId, ownerUserId);

        _referenceRepositoryMock
            .Setup(x => x.GetByIdAsync(assetReferenceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(reference);

        _referenceRepositoryMock
            .Setup(x => x.IsOwnedByUserAsync(assetReferenceId, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().BeNull();
        _referenceRepositoryMock.Verify(x => x.UpdateAsync(It.IsAny<AssetReference>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_OwnerUpdatesDisplayName_UpdatesAndReturnsResponse()
    {
        // Arrange
        var assetReferenceId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var command = new UpdateAssetCommand(assetReferenceId, userId, DisplayName: "New Display Name");

        var reference = CreateAssetReference(assetReferenceId, userId);

        _referenceRepositoryMock
            .Setup(x => x.GetByIdAsync(assetReferenceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(reference);

        _referenceRepositoryMock
            .Setup(x => x.IsOwnedByUserAsync(assetReferenceId, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.AssetReferenceId.Should().Be(assetReferenceId);
        _referenceRepositoryMock.Verify(x => x.UpdateAsync(reference, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_UpdateAccessPolicy_UpdatesAndReturnsResponse()
    {
        // Arrange
        var assetReferenceId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var command = new UpdateAssetCommand(assetReferenceId, userId, AccessPolicy: AssetAccessPolicy.Public);

        var reference = CreateAssetReference(assetReferenceId, userId);

        _referenceRepositoryMock
            .Setup(x => x.GetByIdAsync(assetReferenceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(reference);

        _referenceRepositoryMock
            .Setup(x => x.IsOwnedByUserAsync(assetReferenceId, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        _referenceRepositoryMock.Verify(x => x.UpdateAsync(reference, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_UpdateBothDisplayNameAndPolicy_UpdatesBoth()
    {
        // Arrange
        var assetReferenceId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var command = new UpdateAssetCommand(
            assetReferenceId,
            userId,
            DisplayName: "Updated Name",
            AccessPolicy: AssetAccessPolicy.TenantPublic);

        var reference = CreateAssetReference(assetReferenceId, userId);

        _referenceRepositoryMock
            .Setup(x => x.GetByIdAsync(assetReferenceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(reference);

        _referenceRepositoryMock
            .Setup(x => x.IsOwnedByUserAsync(assetReferenceId, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        _referenceRepositoryMock.Verify(x => x.UpdateAsync(reference, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_NoUpdates_StillCallsUpdate()
    {
        // Arrange
        var assetReferenceId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var command = new UpdateAssetCommand(assetReferenceId, userId);

        var reference = CreateAssetReference(assetReferenceId, userId);

        _referenceRepositoryMock
            .Setup(x => x.GetByIdAsync(assetReferenceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(reference);

        _referenceRepositoryMock
            .Setup(x => x.IsOwnedByUserAsync(assetReferenceId, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        _referenceRepositoryMock.Verify(x => x.UpdateAsync(reference, It.IsAny<CancellationToken>()), Times.Once);
    }

    private static AssetReference CreateAssetReference(Guid id, Guid createdByUserId)
    {
        var reference = new AssetReference(
            Guid.NewGuid(),
            createdByUserId,
            "Original Name",
            AssetAccessPolicy.Private,
            null,
            null);
        
        typeof(AssetReference).GetProperty("Id")?.SetValue(reference, id);
        
        return reference;
    }
}

public class ReviewReportCommandHandlerTests
{
    private readonly Mock<IAssetModerationService> _moderationServiceMock;
    private readonly ReviewReportHandler _handler;

    public ReviewReportCommandHandlerTests()
    {
        _moderationServiceMock = new Mock<IAssetModerationService>();
        _handler = new ReviewReportHandler(_moderationServiceMock.Object);
    }

    [Fact]
    public async Task Handle_ReviewSucceeds_ReturnsResponse()
    {
        // Arrange
        var reportId = Guid.NewGuid();
        var reviewerId = Guid.NewGuid();
        var command = new ReviewReportCommand(
            reportId,
            reviewerId,
            ReviewDecision.NoAction,
            "Looks good");

        _moderationServiceMock
            .Setup(x => x.SubmitReviewAsync(
                reportId,
                reviewerId,
                ReviewDecision.NoAction,
                "Looks good",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.ReportId.Should().Be(reportId);
        result.Status.Should().Be(ReportStatus.Resolved);
        result.Decision.Should().Be(ReviewDecision.NoAction);
    }

    [Fact]
    public async Task Handle_ReviewFails_ReturnsNull()
    {
        // Arrange
        var command = new ReviewReportCommand(
            Guid.NewGuid(),
            Guid.NewGuid(),
            ReviewDecision.ContentRemoved);

        _moderationServiceMock
            .Setup(x => x.SubmitReviewAsync(
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<ReviewDecision>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    [Theory]
    [InlineData(ReviewDecision.NoAction)]
    [InlineData(ReviewDecision.ContentRemoved)]
    [InlineData(ReviewDecision.UserWarned)]
    public async Task Handle_DifferentDecisions_PassesCorrectDecision(ReviewDecision decision)
    {
        // Arrange
        var reportId = Guid.NewGuid();
        var reviewerId = Guid.NewGuid();
        var command = new ReviewReportCommand(reportId, reviewerId, decision);

        _moderationServiceMock
            .Setup(x => x.SubmitReviewAsync(
                reportId,
                reviewerId,
                decision,
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Decision.Should().Be(decision);
        _moderationServiceMock.Verify(
            x => x.SubmitReviewAsync(reportId, reviewerId, decision, null, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_NullNotes_PassesNullToService()
    {
        // Arrange
        var reportId = Guid.NewGuid();
        var reviewerId = Guid.NewGuid();
        var command = new ReviewReportCommand(reportId, reviewerId, ReviewDecision.NoAction, Notes: null);

        _moderationServiceMock
            .Setup(x => x.SubmitReviewAsync(
                reportId,
                reviewerId,
                ReviewDecision.NoAction,
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _moderationServiceMock.Verify(
            x => x.SubmitReviewAsync(reportId, reviewerId, ReviewDecision.NoAction, null, It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
