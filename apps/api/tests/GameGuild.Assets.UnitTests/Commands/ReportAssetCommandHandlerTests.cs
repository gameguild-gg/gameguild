using GameGuild.Assets.Commands;

namespace GameGuild.Assets.UnitTests.Commands;

public class ReportAssetCommandHandlerTests
{
    private readonly Mock<IAssetModerationService> _moderationServiceMock;
    private readonly Mock<IAssetReferenceRepository> _referenceRepositoryMock;
    private readonly ReportAssetHandler _handler;

    public ReportAssetCommandHandlerTests()
    {
        _moderationServiceMock = new Mock<IAssetModerationService>();
        _referenceRepositoryMock = new Mock<IAssetReferenceRepository>();
        _handler = new ReportAssetHandler(
            _moderationServiceMock.Object,
            _referenceRepositoryMock.Object);
    }

    [Fact]
    public async Task Handle_AssetNotFound_ReturnsNull()
    {
        // Arrange
        var command = new ReportAssetCommand(
            Guid.NewGuid(),
            Guid.NewGuid(),
            ReportReason.Inappropriate,
            "Test description");

        _referenceRepositoryMock
            .Setup(x => x.GetByIdAsync(command.AssetReferenceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((AssetReference?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().BeNull();
        _moderationServiceMock.Verify(
            x => x.CreateReportAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<ReportReason>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_UserReportsOwnAsset_ReturnsNull()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var assetReferenceId = Guid.NewGuid();
        var command = new ReportAssetCommand(
            assetReferenceId,
            userId,
            ReportReason.Inappropriate);

        var reference = CreateAssetReference(assetReferenceId, userId); // Same user as creator

        _referenceRepositoryMock
            .Setup(x => x.GetByIdAsync(assetReferenceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(reference);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().BeNull();
        _moderationServiceMock.Verify(
            x => x.CreateReportAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<ReportReason>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_ValidReport_CreatesReportAndReturnsResponse()
    {
        // Arrange
        var reporterUserId = Guid.NewGuid();
        var assetOwnerUserId = Guid.NewGuid();
        var assetReferenceId = Guid.NewGuid();
        var reportId = Guid.NewGuid();
        var command = new ReportAssetCommand(
            assetReferenceId,
            reporterUserId,
            ReportReason.Inappropriate,
            "Contains inappropriate material");

        var reference = CreateAssetReference(assetReferenceId, assetOwnerUserId);
        var report = CreateAssetReport(reportId, assetReferenceId, reporterUserId, ReportStatus.Pending);

        _referenceRepositoryMock
            .Setup(x => x.GetByIdAsync(assetReferenceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(reference);

        _moderationServiceMock
            .Setup(x => x.CreateReportAsync(
                assetReferenceId,
                reporterUserId,
                ReportReason.Inappropriate,
                "Contains inappropriate material",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(report);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.ReportId.Should().Be(reportId);
        result.Status.Should().Be(ReportStatus.Pending);
    }

    [Fact]
    public async Task Handle_ModerationServiceReturnsNull_ReturnsNull()
    {
        // Arrange
        var reporterUserId = Guid.NewGuid();
        var assetOwnerUserId = Guid.NewGuid();
        var assetReferenceId = Guid.NewGuid();
        var command = new ReportAssetCommand(
            assetReferenceId,
            reporterUserId,
            ReportReason.Copyright);

        var reference = CreateAssetReference(assetReferenceId, assetOwnerUserId);

        _referenceRepositoryMock
            .Setup(x => x.GetByIdAsync(assetReferenceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(reference);

        _moderationServiceMock
            .Setup(x => x.CreateReportAsync(
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<ReportReason>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((AssetReport?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task Handle_NullDescription_PassesNullToService()
    {
        // Arrange
        var reporterUserId = Guid.NewGuid();
        var assetOwnerUserId = Guid.NewGuid();
        var assetReferenceId = Guid.NewGuid();
        var reportId = Guid.NewGuid();
        var command = new ReportAssetCommand(
            assetReferenceId,
            reporterUserId,
            ReportReason.Spam,
            Description: null);

        var reference = CreateAssetReference(assetReferenceId, assetOwnerUserId);
        var report = CreateAssetReport(reportId, assetReferenceId, reporterUserId, ReportStatus.Pending);

        _referenceRepositoryMock
            .Setup(x => x.GetByIdAsync(assetReferenceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(reference);

        _moderationServiceMock
            .Setup(x => x.CreateReportAsync(
                assetReferenceId,
                reporterUserId,
                ReportReason.Spam,
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(report);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        _moderationServiceMock.Verify(
            x => x.CreateReportAsync(assetReferenceId, reporterUserId, ReportReason.Spam, null, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Theory]
    [InlineData(ReportReason.Inappropriate)]
    [InlineData(ReportReason.Copyright)]
    [InlineData(ReportReason.Spam)]
    [InlineData(ReportReason.Violence)]
    [InlineData(ReportReason.Other)]
    public async Task Handle_DifferentReportReasons_PassesCorrectReason(ReportReason reason)
    {
        // Arrange
        var reporterUserId = Guid.NewGuid();
        var assetOwnerUserId = Guid.NewGuid();
        var assetReferenceId = Guid.NewGuid();
        var reportId = Guid.NewGuid();
        var command = new ReportAssetCommand(
            assetReferenceId,
            reporterUserId,
            reason);

        var reference = CreateAssetReference(assetReferenceId, assetOwnerUserId);
        var report = CreateAssetReport(reportId, assetReferenceId, reporterUserId, ReportStatus.Pending);

        _referenceRepositoryMock
            .Setup(x => x.GetByIdAsync(assetReferenceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(reference);

        _moderationServiceMock
            .Setup(x => x.CreateReportAsync(
                assetReferenceId,
                reporterUserId,
                reason,
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(report);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        _moderationServiceMock.Verify(
            x => x.CreateReportAsync(assetReferenceId, reporterUserId, reason, null, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    private static AssetReference CreateAssetReference(Guid id, Guid createdByUserId)
    {
        var reference = new AssetReference(
            Guid.NewGuid(),
            createdByUserId,
            "Test Asset",
            AssetAccessPolicy.Private,
            null,
            null);
        
        typeof(AssetReference).GetProperty("Id")?.SetValue(reference, id);
        
        return reference;
    }

    private static AssetReport CreateAssetReport(Guid id, Guid assetReferenceId, Guid reportedByUserId, ReportStatus status)
    {
        var report = new AssetReport(
            assetReferenceId,
            reportedByUserId,
            ReportReason.Inappropriate,
            null);
        
        typeof(AssetReport).GetProperty("Id")?.SetValue(report, id);
        typeof(AssetReport).GetProperty("Status")?.SetValue(report, status);
        
        return report;
    }
}
