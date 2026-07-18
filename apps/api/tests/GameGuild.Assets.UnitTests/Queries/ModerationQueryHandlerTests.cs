using GameGuild.Assets.Queries;

namespace GameGuild.Assets.UnitTests.Queries;

public class GetAssetReportsQueryHandlerTests
{
    private readonly Mock<IAssetReportRepository> _reportRepositoryMock;
    private readonly GetAssetReportsHandler _handler;

    public GetAssetReportsQueryHandlerTests()
    {
        _reportRepositoryMock = new Mock<IAssetReportRepository>();
        _handler = new GetAssetReportsHandler(_reportRepositoryMock.Object);
    }

    [Fact]
    public async Task Handle_NoReports_ReturnsEmptyList()
    {
        // Arrange
        var assetReferenceId = Guid.NewGuid();
        var query = new GetAssetReportsQuery(assetReferenceId);

        _reportRepositoryMock
            .Setup(x => x.GetByAssetReferenceAsync(assetReferenceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AssetReport>());

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_MultipleReports_ReturnsAllMapped()
    {
        // Arrange
        var assetReferenceId = Guid.NewGuid();
        var query = new GetAssetReportsQuery(assetReferenceId);

        var reports = new List<AssetReport>
        {
            CreateReport(Guid.NewGuid(), assetReferenceId, ReportReason.Inappropriate),
            CreateReport(Guid.NewGuid(), assetReferenceId, ReportReason.Copyright),
            CreateReport(Guid.NewGuid(), assetReferenceId, ReportReason.Spam)
        };

        _reportRepositoryMock
            .Setup(x => x.GetByAssetReferenceAsync(assetReferenceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(reports);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(3);
    }

    [Fact]
    public async Task Handle_MapsReportProperties()
    {
        // Arrange
        var assetReferenceId = Guid.NewGuid();
        var reportId = Guid.NewGuid();
        var reportedByUserId = Guid.NewGuid();
        var reviewerId = Guid.NewGuid();
        var query = new GetAssetReportsQuery(assetReferenceId);

        var report = CreateReport(reportId, assetReferenceId, ReportReason.Violence);
        typeof(AssetReport).GetProperty("ReportedByUserId")?.SetValue(report, reportedByUserId);
        typeof(AssetReport).GetProperty("Details")?.SetValue(report, "Contains violence");
        typeof(AssetReport).GetProperty("Status")?.SetValue(report, ReportStatus.Resolved);
        typeof(AssetReport).GetProperty("Decision")?.SetValue(report, ReviewDecision.ContentRemoved);
        typeof(AssetReport).GetProperty("ReviewedByUserId")?.SetValue(report, reviewerId);
        typeof(AssetReport).GetProperty("ReviewNotes")?.SetValue(report, "Reviewed and removed");

        _reportRepositoryMock
            .Setup(x => x.GetByAssetReferenceAsync(assetReferenceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AssetReport> { report });

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().HaveCount(1);
        var dto = result[0];
        dto.Id.Should().Be(reportId);
        dto.AssetReferenceId.Should().Be(assetReferenceId);
        dto.Reason.Should().Be(ReportReason.Violence);
        dto.Description.Should().Be("Contains violence");
        dto.Status.Should().Be(ReportStatus.Resolved);
        dto.Decision.Should().Be(ReviewDecision.ContentRemoved);
        dto.ReviewedByUserId.Should().Be(reviewerId);
        dto.ReviewNotes.Should().Be("Reviewed and removed");
    }

    [Theory]
    [InlineData(ReportReason.Inappropriate)]
    [InlineData(ReportReason.Copyright)]
    [InlineData(ReportReason.Spam)]
    [InlineData(ReportReason.Violence)]
    [InlineData(ReportReason.Harassment)]
    public async Task Handle_DifferentReasons_MapsCorrectly(ReportReason reason)
    {
        // Arrange
        var assetReferenceId = Guid.NewGuid();
        var query = new GetAssetReportsQuery(assetReferenceId);

        var report = CreateReport(Guid.NewGuid(), assetReferenceId, reason);

        _reportRepositoryMock
            .Setup(x => x.GetByAssetReferenceAsync(assetReferenceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AssetReport> { report });

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().HaveCount(1);
        result[0].Reason.Should().Be(reason);
    }

    [Fact]
    public async Task Handle_AssetDtoIsNull()
    {
        // Arrange
        var assetReferenceId = Guid.NewGuid();
        var query = new GetAssetReportsQuery(assetReferenceId);

        var report = CreateReport(Guid.NewGuid(), assetReferenceId, ReportReason.Spam);

        _reportRepositoryMock
            .Setup(x => x.GetByAssetReferenceAsync(assetReferenceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AssetReport> { report });

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().HaveCount(1);
        result[0].Asset.Should().BeNull();
    }

    private static AssetReport CreateReport(Guid id, Guid assetReferenceId, ReportReason reason)
    {
        var report = new AssetReport(
            assetReferenceId,
            Guid.NewGuid(),
            reason,
            null);
        
        typeof(AssetReport).GetProperty("Id")?.SetValue(report, id);
        
        return report;
    }
}

public class GetModerationQueueQueryHandlerTests
{
    private readonly Mock<IAssetModerationService> _moderationServiceMock;
    private readonly GetModerationQueueHandler _handler;

    public GetModerationQueueQueryHandlerTests()
    {
        _moderationServiceMock = new Mock<IAssetModerationService>();
        _handler = new GetModerationQueueHandler(_moderationServiceMock.Object);
    }

    [Fact]
    public async Task Handle_NoReports_ReturnsEmptyList()
    {
        // Arrange
        var query = new GetModerationQueueQuery(100);

        _moderationServiceMock
            .Setup(x => x.GetPendingReportsAsync(100, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AssetReport>());

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_PassesLimitToService()
    {
        // Arrange
        var query = new GetModerationQueueQuery(50);

        _moderationServiceMock
            .Setup(x => x.GetPendingReportsAsync(50, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AssetReport>());

        // Act
        await _handler.Handle(query, CancellationToken.None);

        // Assert
        _moderationServiceMock.Verify(
            x => x.GetPendingReportsAsync(50, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ReportWithoutReference_AssetDtoIsNull()
    {
        // Arrange
        var query = new GetModerationQueueQuery(100);

        var report = CreateReport(Guid.NewGuid(), Guid.NewGuid());
        // Reference is null by default

        _moderationServiceMock
            .Setup(x => x.GetPendingReportsAsync(100, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AssetReport> { report });

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().HaveCount(1);
        result[0].Asset.Should().BeNull();
    }

    [Fact]
    public async Task Handle_ReportWithReference_MapsAssetDto()
    {
        // Arrange
        var query = new GetModerationQueueQuery(100);
        var assetReferenceId = Guid.NewGuid();
        var contentId = Guid.NewGuid();

        var report = CreateReportWithReference(Guid.NewGuid(), assetReferenceId, contentId);

        _moderationServiceMock
            .Setup(x => x.GetPendingReportsAsync(100, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AssetReport> { report });

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().HaveCount(1);
        result[0].Asset.Should().NotBeNull();
        result[0].Asset!.Id.Should().Be(assetReferenceId);
    }

    [Fact]
    public async Task Handle_ReportWithReferenceAndContent_MapsContentDto()
    {
        // Arrange
        var query = new GetModerationQueueQuery(100);
        var assetReferenceId = Guid.NewGuid();
        var contentId = Guid.NewGuid();

        var report = CreateReportWithReferenceAndContent(Guid.NewGuid(), assetReferenceId, contentId);

        _moderationServiceMock
            .Setup(x => x.GetPendingReportsAsync(100, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AssetReport> { report });

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().HaveCount(1);
        result[0].Asset.Should().NotBeNull();
        var asset = result[0].Asset!;
        asset.Content.Should().NotBeNull();
        var content = asset.Content!;
        content.Id.Should().Be(contentId);
        content.MimeType.Should().Be("image/png");
    }

    [Fact]
    public async Task Handle_MultipleReports_ReturnsAllMapped()
    {
        // Arrange
        var query = new GetModerationQueueQuery(100);

        var reports = new List<AssetReport>
        {
            CreateReport(Guid.NewGuid(), Guid.NewGuid()),
            CreateReport(Guid.NewGuid(), Guid.NewGuid()),
            CreateReport(Guid.NewGuid(), Guid.NewGuid())
        };

        _moderationServiceMock
            .Setup(x => x.GetPendingReportsAsync(100, It.IsAny<CancellationToken>()))
            .ReturnsAsync(reports);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().HaveCount(3);
    }

    [Fact]
    public async Task Handle_MapsAllReportProperties()
    {
        // Arrange
        var query = new GetModerationQueueQuery(100);
        var reportId = Guid.NewGuid();
        var assetReferenceId = Guid.NewGuid();
        var reportedByUserId = Guid.NewGuid();

        var report = new AssetReport(
            assetReferenceId,
            reportedByUserId,
            ReportReason.Copyright,
            "Copyright violation details");
        
        typeof(AssetReport).GetProperty("Id")?.SetValue(report, reportId);

        _moderationServiceMock
            .Setup(x => x.GetPendingReportsAsync(100, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AssetReport> { report });

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().HaveCount(1);
        var dto = result[0];
        dto.Id.Should().Be(reportId);
        dto.AssetReferenceId.Should().Be(assetReferenceId);
        dto.ReportedByUserId.Should().Be(reportedByUserId);
        dto.Reason.Should().Be(ReportReason.Copyright);
        dto.Description.Should().Be("Copyright violation details");
    }

    private static AssetReport CreateReport(Guid id, Guid assetReferenceId)
    {
        var report = new AssetReport(
            assetReferenceId,
            Guid.NewGuid(),
            ReportReason.Spam,
            null);
        
        typeof(AssetReport).GetProperty("Id")?.SetValue(report, id);
        
        return report;
    }

    private static AssetReport CreateReportWithReference(Guid reportId, Guid assetReferenceId, Guid contentId)
    {
        var reference = new AssetReference(
            contentId,
            Guid.NewGuid(),
            "Test Asset",
            AssetAccessPolicy.Private,
            null,
            null);
        typeof(AssetReference).GetProperty("Id")?.SetValue(reference, assetReferenceId);

        var report = new AssetReport(
            assetReferenceId,
            Guid.NewGuid(),
            ReportReason.Spam,
            null);
        typeof(AssetReport).GetProperty("Id")?.SetValue(report, reportId);
        typeof(AssetReport).GetProperty("Reference")?.SetValue(report, reference);
        
        return report;
    }

    private static AssetReport CreateReportWithReferenceAndContent(Guid reportId, Guid assetReferenceId, Guid contentId)
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
        typeof(AssetReference).GetProperty("Id")?.SetValue(reference, assetReferenceId);
        typeof(AssetReference).GetProperty("Content")?.SetValue(reference, content);

        var report = new AssetReport(
            assetReferenceId,
            Guid.NewGuid(),
            ReportReason.Spam,
            null);
        typeof(AssetReport).GetProperty("Id")?.SetValue(report, reportId);
        typeof(AssetReport).GetProperty("Reference")?.SetValue(report, reference);
        
        return report;
    }
}
