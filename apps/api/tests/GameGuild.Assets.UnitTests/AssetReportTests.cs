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
            ReportReason.Inappropriate,
            "This content is offensive");

        // Assert
        report.AssetReferenceId.Should().Be(assetReferenceId);
        report.ReportedByUserId.Should().Be(reportedByUserId);
        report.Reason.Should().Be(ReportReason.Inappropriate);
        report.Details.Should().Be("This content is offensive");
        report.Status.Should().Be(ReportStatus.Pending);
        report.Decision.Should().BeNull();
        report.ReviewedByUserId.Should().BeNull();
    }

    [Fact]
    public void Constructor_WithoutDetails_ShouldHaveNullDetails()
    {
        // Arrange
        var assetReferenceId = Guid.NewGuid();
        var reportedByUserId = Guid.NewGuid();

        // Act
        var report = new AssetReport(
            assetReferenceId,
            reportedByUserId,
            ReportReason.Copyright,
            null);

        // Assert
        report.Details.Should().BeNull();
    }

    [Theory]
    [InlineData(ReportReason.Inappropriate)]
    [InlineData(ReportReason.Copyright)]
    [InlineData(ReportReason.Spam)]
    [InlineData(ReportReason.Harassment)]
    [InlineData(ReportReason.Violence)]
    [InlineData(ReportReason.Other)]
    public void Constructor_AllReasons_ShouldWork(ReportReason reason)
    {
        // Arrange
        var assetReferenceId = Guid.NewGuid();
        var reportedByUserId = Guid.NewGuid();

        // Act
        var report = new AssetReport(assetReferenceId, reportedByUserId, reason, null);

        // Assert
        report.Reason.Should().Be(reason);
    }

    [Fact]
    public void SubmitReview_WithNoAction_ShouldResolveReport()
    {
        // Arrange
        var report = CreateTestReport();
        var reviewerId = Guid.NewGuid();

        // Act
        report.SubmitReview(reviewerId, ReviewDecision.NoAction, "Content is acceptable");

        // Assert
        report.Status.Should().Be(ReportStatus.Resolved);
        report.Decision.Should().Be(ReviewDecision.NoAction);
        report.ReviewedByUserId.Should().Be(reviewerId);
        report.ReviewNotes.Should().Be("Content is acceptable");
        report.ReviewedAt.Should().NotBeNull();
    }

    [Fact]
    public void SubmitReview_WithUserWarned_ShouldResolveReport()
    {
        // Arrange
        var report = CreateTestReport();
        var reviewerId = Guid.NewGuid();

        // Act
        report.SubmitReview(reviewerId, ReviewDecision.UserWarned, "User warned about content policy");

        // Assert
        report.Status.Should().Be(ReportStatus.Resolved);
        report.Decision.Should().Be(ReviewDecision.UserWarned);
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
    public void SubmitReview_WithContentRemoved_ShouldResolveReport()
    {
        // Arrange
        var report = CreateTestReport();
        var reviewerId = Guid.NewGuid();

        // Act
        report.SubmitReview(reviewerId, ReviewDecision.ContentRemoved, "Asset removed");

        // Assert
        report.Status.Should().Be(ReportStatus.Resolved);
        report.Decision.Should().Be(ReviewDecision.ContentRemoved);
    }

    [Fact]
    public void SubmitReview_WithUserSuspended_ShouldResolveReport()
    {
        // Arrange
        var report = CreateTestReport();
        var reviewerId = Guid.NewGuid();

        // Act
        report.SubmitReview(reviewerId, ReviewDecision.UserSuspended, "Repeated violations");

        // Assert
        report.Status.Should().Be(ReportStatus.Resolved);
        report.Decision.Should().Be(ReviewDecision.UserSuspended);
    }

    [Fact]
    public void SubmitReview_WithoutNotes_ShouldWork()
    {
        // Arrange
        var report = CreateTestReport();
        var reviewerId = Guid.NewGuid();

        // Act
        report.SubmitReview(reviewerId, ReviewDecision.NoAction);

        // Assert
        report.ReviewNotes.Should().BeNull();
    }

    private static AssetReport CreateTestReport()
    {
        return new AssetReport(
            Guid.NewGuid(),
            Guid.NewGuid(),
            ReportReason.Inappropriate,
            "Test report");
    }
}
