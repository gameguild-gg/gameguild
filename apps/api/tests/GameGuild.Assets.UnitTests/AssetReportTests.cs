namespace GameGuild.Assets.UnitTests;

public class AssetReportTests
{
    [Fact]
    public void Constructor_WithValidParameters_ShouldCreateInstance()
    {
        // Arrange
        var assetReferenceId = Guid.NewGuid();
        var reportedByUserId = Guid.NewGuid();

        // Act
        var report = new AssetReport(
            assetReferenceId,
            reportedByUserId,
            ReportReason.InappropriateContent,
            "This content is offensive");

        // Assert
        report.AssetReferenceId.Should().Be(assetReferenceId);
        report.ReportedByUserId.Should().Be(reportedByUserId);
        report.Reason.Should().Be(ReportReason.InappropriateContent);
        report.Description.Should().Be("This content is offensive");
        report.Status.Should().Be(ReportStatus.Pending);
        report.Decision.Should().BeNull();
        report.ReviewedByUserId.Should().BeNull();
    }

    [Fact]
    public void Constructor_WithoutDescription_ShouldHaveNullDescription()
    {
        // Arrange
        var assetReferenceId = Guid.NewGuid();
        var reportedByUserId = Guid.NewGuid();

        // Act
        var report = new AssetReport(
            assetReferenceId,
            reportedByUserId,
            ReportReason.Copyright);

        // Assert
        report.Description.Should().BeNull();
    }

    [Theory]
    [InlineData(ReportReason.InappropriateContent)]
    [InlineData(ReportReason.Copyright)]
    [InlineData(ReportReason.Spam)]
    [InlineData(ReportReason.Harassment)]
    [InlineData(ReportReason.Malware)]
    [InlineData(ReportReason.Other)]
    public void Constructor_AllReasons_ShouldWork(ReportReason reason)
    {
        // Arrange
        var assetReferenceId = Guid.NewGuid();
        var reportedByUserId = Guid.NewGuid();

        // Act
        var report = new AssetReport(assetReferenceId, reportedByUserId, reason);

        // Assert
        report.Reason.Should().Be(reason);
    }

    [Fact]
    public void SubmitReview_WithDismiss_ShouldResolveReport()
    {
        // Arrange
        var report = CreateTestReport();
        var reviewerId = Guid.NewGuid();

        // Act
        report.SubmitReview(reviewerId, ReviewDecision.Dismiss, "Content is acceptable");

        // Assert
        report.Status.Should().Be(ReportStatus.Resolved);
        report.Decision.Should().Be(ReviewDecision.Dismiss);
        report.ReviewedByUserId.Should().Be(reviewerId);
        report.ReviewNotes.Should().Be("Content is acceptable");
        report.ReviewedAt.Should().NotBeNull();
    }

    [Fact]
    public void SubmitReview_WithWarnUser_ShouldResolveReport()
    {
        // Arrange
        var report = CreateTestReport();
        var reviewerId = Guid.NewGuid();

        // Act
        report.SubmitReview(reviewerId, ReviewDecision.WarnUser, "User warned about content policy");

        // Assert
        report.Status.Should().Be(ReportStatus.Resolved);
        report.Decision.Should().Be(ReviewDecision.WarnUser);
    }

    [Fact]
    public void SubmitReview_WithBlockContent_ShouldResolveReport()
    {
        // Arrange
        var report = CreateTestReport();
        var reviewerId = Guid.NewGuid();

        // Act
        report.SubmitReview(reviewerId, ReviewDecision.BlockContent, "Content violates ToS");

        // Assert
        report.Status.Should().Be(ReportStatus.Resolved);
        report.Decision.Should().Be(ReviewDecision.BlockContent);
    }

    [Fact]
    public void SubmitReview_WithDeleteAsset_ShouldResolveReport()
    {
        // Arrange
        var report = CreateTestReport();
        var reviewerId = Guid.NewGuid();

        // Act
        report.SubmitReview(reviewerId, ReviewDecision.DeleteAsset, "Asset removed");

        // Assert
        report.Status.Should().Be(ReportStatus.Resolved);
        report.Decision.Should().Be(ReviewDecision.DeleteAsset);
    }

    [Fact]
    public void SubmitReview_WithSuspendUser_ShouldResolveReport()
    {
        // Arrange
        var report = CreateTestReport();
        var reviewerId = Guid.NewGuid();

        // Act
        report.SubmitReview(reviewerId, ReviewDecision.SuspendUser, "Repeated violations");

        // Assert
        report.Status.Should().Be(ReportStatus.Resolved);
        report.Decision.Should().Be(ReviewDecision.SuspendUser);
    }

    [Fact]
    public void SubmitReview_WithoutNotes_ShouldWork()
    {
        // Arrange
        var report = CreateTestReport();
        var reviewerId = Guid.NewGuid();

        // Act
        report.SubmitReview(reviewerId, ReviewDecision.Dismiss);

        // Assert
        report.ReviewNotes.Should().BeNull();
    }

    private static AssetReport CreateTestReport()
    {
        return new AssetReport(
            Guid.NewGuid(),
            Guid.NewGuid(),
            ReportReason.InappropriateContent,
            "Test report");
    }
}
