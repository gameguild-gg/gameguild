using FluentAssertions;
using Xunit;

namespace GameGuild.Learning.Courses.UnitTests.Entities;

public sealed class ContentReportCoverageTests
{
    [Fact]
    public void AssignToModerator_MovesPendingReportIntoReview()
    {
        var moderatorId = Guid.NewGuid();
        var report = new ContentReport();

        report.AssignToModerator(moderatorId);

        report.ModeratorId.Should().Be(moderatorId);
        report.AssignedAt.Should().NotBeNull();
        report.Status.Should().Be(ReportStatus.InReview);
        report.UpdatedAt.Should().NotBe(default);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("Content removed")]
    public void Resolve_RecordsDecisionAndOptionalAction(string? actionTaken)
    {
        var report = new ContentReport();

        report.Resolve("Verified by moderation", actionTaken);

        report.Status.Should().Be(ReportStatus.Resolved);
        report.ResolvedAt.Should().NotBeNull();
        report.ResolutionNotes.Should().Be("Verified by moderation");
        report.ActionTaken.Should().Be(actionTaken);
        report.UpdatedAt.Should().NotBe(default);
    }

    [Fact]
    public void Dismiss_RecordsReasonAndCompletionTime()
    {
        var report = new ContentReport();

        report.Dismiss("No policy violation");

        report.Status.Should().Be(ReportStatus.Dismissed);
        report.ResolvedAt.Should().NotBeNull();
        report.ResolutionNotes.Should().Be("No policy violation");
        report.UpdatedAt.Should().NotBe(default);
    }
}
