using FluentAssertions;
using GameGuild.Resources.Contents;
using Xunit;

namespace GameGuild.Resources.Contents.UnitTests;

#region ContentVersion Entity Tests

public class ContentVersionTests
{
    [Fact]
    public void Create_ShouldSetAllProperties()
    {
        var entityId = Guid.NewGuid();
        var createdBy = Guid.NewGuid();

        var version = ContentVersion.Create(
            entityId, "Course", 1,
            "  Introduction to C#  ", createdBy,
            summary: "  A beginner course  ",
            body: "<p>Content</p>",
            metadata: "{\"key\":\"value\"}",
            changeNotes: "  Initial version  ");

        version.Id.Should().NotBeEmpty();
        version.EntityId.Should().Be(entityId);
        version.EntityType.Should().Be("Course"); // trimmed
        version.VersionNumber.Should().Be(1);
        version.Title.Should().Be("Introduction to C#"); // trimmed
        version.Summary.Should().Be("A beginner course"); // trimmed
        version.Body.Should().Be("<p>Content</p>");
        version.Metadata.Should().Be("{\"key\":\"value\"}");
        version.CreatedBy.Should().Be(createdBy);
        version.ChangeNotes.Should().Be("Initial version"); // trimmed
        version.Status.Should().Be(ContentVersionStatus.Draft);
        version.IsCurrentVersion.Should().BeFalse();
    }

    [Fact]
    public void Create_WithMinimalParams_ShouldSetDefaults()
    {
        var version = ContentVersion.Create(
            Guid.NewGuid(), "Page", 1,
            "Title", Guid.NewGuid());

        version.Summary.Should().BeNull();
        version.Body.Should().BeNull();
        version.Metadata.Should().BeNull();
        version.ChangeNotes.Should().BeNull();
        version.SubmittedForReviewAt.Should().BeNull();
        version.ReviewedAt.Should().BeNull();
        version.PublishedAt.Should().BeNull();
    }

    [Fact]
    public void UpdateDraft_ShouldUpdateFields()
    {
        var version = ContentVersion.Create(
            Guid.NewGuid(), "Course", 1, "Title", Guid.NewGuid());

        version.UpdateDraft(
            title: "  Updated Title  ",
            summary: "  New summary  ",
            body: "<p>Updated</p>",
            changeNotes: "  Updated content  ");

        version.Title.Should().Be("Updated Title");
        version.Summary.Should().Be("New summary");
        version.Body.Should().Be("<p>Updated</p>");
        version.ChangeNotes.Should().Be("Updated content");
    }

    [Fact]
    public void UpdateDraft_WhenNotDraft_ShouldThrow()
    {
        var version = ContentVersion.Create(
            Guid.NewGuid(), "Course", 1, "Title", Guid.NewGuid());
        version.SubmitForReview(Guid.NewGuid());

        var act = () => version.UpdateDraft(title: "New Title");

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*draft*");
    }

    [Fact]
    public void SubmitForReview_FromDraft_ShouldTransition()
    {
        var version = ContentVersion.Create(
            Guid.NewGuid(), "Course", 1, "Title", Guid.NewGuid());
        var submitter = Guid.NewGuid();

        version.SubmitForReview(submitter);

        version.Status.Should().Be(ContentVersionStatus.PendingReview);
        version.SubmittedBy.Should().Be(submitter);
        version.SubmittedForReviewAt.Should().NotBeNull();
    }

    [Fact]
    public void SubmitForReview_WhenNotDraft_ShouldThrow()
    {
        var version = ContentVersion.Create(
            Guid.NewGuid(), "Course", 1, "Title", Guid.NewGuid());
        version.SubmitForReview(Guid.NewGuid());

        var act = () => version.SubmitForReview(Guid.NewGuid());

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*drafts*");
    }

    [Fact]
    public void Approve_FromPendingReview_ShouldTransition()
    {
        var version = CreatePendingReviewVersion();
        var reviewer = Guid.NewGuid();

        version.Approve(reviewer, "Looks good!");

        version.Status.Should().Be(ContentVersionStatus.Approved);
        version.ReviewedBy.Should().Be(reviewer);
        version.ReviewedAt.Should().NotBeNull();
        version.ReviewNotes.Should().Be("Looks good!");
    }

    [Fact]
    public void Approve_WhenNotPendingReview_ShouldThrow()
    {
        var version = ContentVersion.Create(
            Guid.NewGuid(), "Course", 1, "Title", Guid.NewGuid());

        var act = () => version.Approve(Guid.NewGuid());

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*pending review*");
    }

    [Fact]
    public void Reject_FromPendingReview_ShouldTransition()
    {
        var version = CreatePendingReviewVersion();
        var reviewer = Guid.NewGuid();

        version.Reject(reviewer, "  Needs improvements  ");

        version.Status.Should().Be(ContentVersionStatus.Rejected);
        version.ReviewedBy.Should().Be(reviewer);
        version.ReviewNotes.Should().Be("Needs improvements"); // trimmed
    }

    [Fact]
    public void Reject_WhenNotPendingReview_ShouldThrow()
    {
        var version = ContentVersion.Create(
            Guid.NewGuid(), "Course", 1, "Title", Guid.NewGuid());

        var act = () => version.Reject(Guid.NewGuid());

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Publish_FromApproved_ShouldTransition()
    {
        var version = CreateApprovedVersion();
        var publisher = Guid.NewGuid();

        version.Publish(publisher);

        version.Status.Should().Be(ContentVersionStatus.Published);
        version.PublishedBy.Should().Be(publisher);
        version.PublishedAt.Should().NotBeNull();
        version.IsCurrentVersion.Should().BeTrue();
    }

    [Fact]
    public void Publish_WhenDraft_ShouldThrow()
    {
        var version = ContentVersion.Create(
            Guid.NewGuid(), "Course", 1, "Title", Guid.NewGuid());

        var act = () => version.Publish(Guid.NewGuid());

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*approved or scheduled*");
    }

    [Fact]
    public void SchedulePublish_FromApproved_ShouldTransition()
    {
        var version = CreateApprovedVersion();
        var scheduledAt = DateTime.UtcNow.AddDays(7);
        var scheduledBy = Guid.NewGuid();

        version.SchedulePublish(scheduledAt, scheduledBy);

        version.Status.Should().Be(ContentVersionStatus.Scheduled);
        version.ScheduledPublishAt.Should().Be(scheduledAt);
        version.PublishedBy.Should().Be(scheduledBy);
    }

    [Fact]
    public void SchedulePublish_WhenNotApproved_ShouldThrow()
    {
        var version = ContentVersion.Create(
            Guid.NewGuid(), "Course", 1, "Title", Guid.NewGuid());

        var act = () => version.SchedulePublish(DateTime.UtcNow.AddDays(1), Guid.NewGuid());

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*approved*");
    }

    [Fact]
    public void Publish_FromScheduled_ShouldWork()
    {
        var version = CreateApprovedVersion();
        version.SchedulePublish(DateTime.UtcNow.AddDays(1), Guid.NewGuid());

        version.Publish(Guid.NewGuid());

        version.Status.Should().Be(ContentVersionStatus.Published);
        version.IsCurrentVersion.Should().BeTrue();
    }

    [Fact]
    public void Archive_ShouldSetStatusAndClearCurrent()
    {
        var version = CreateApprovedVersion();
        version.Publish(Guid.NewGuid());
        version.IsCurrentVersion.Should().BeTrue();

        version.Archive();

        version.Status.Should().Be(ContentVersionStatus.Archived);
        version.IsCurrentVersion.Should().BeFalse();
    }

    [Fact]
    public void SetAsCurrent_ShouldToggle()
    {
        var version = ContentVersion.Create(
            Guid.NewGuid(), "Course", 1, "Title", Guid.NewGuid());

        version.SetAsCurrent(true);
        version.IsCurrentVersion.Should().BeTrue();

        version.SetAsCurrent(false);
        version.IsCurrentVersion.Should().BeFalse();
    }

    // --- Helpers ---

    private static ContentVersion CreatePendingReviewVersion()
    {
        var version = ContentVersion.Create(
            Guid.NewGuid(), "Course", 1, "Title", Guid.NewGuid());
        version.SubmitForReview(Guid.NewGuid());
        return version;
    }

    private static ContentVersion CreateApprovedVersion()
    {
        var version = CreatePendingReviewVersion();
        version.Approve(Guid.NewGuid());
        return version;
    }
}

#endregion

#region ContentVersionReview Tests

public class ContentVersionReviewTests
{
    [Fact]
    public void Create_ShouldSetAllProperties()
    {
        var versionId = Guid.NewGuid();
        var reviewerId = Guid.NewGuid();

        var review = ContentVersionReview.Create(
            versionId, reviewerId,
            ContentReviewDecision.Approve,
            feedback: "  Well written  ",
            suggestions: "{\"items\":[]}");

        review.Id.Should().NotBeEmpty();
        review.ContentVersionId.Should().Be(versionId);
        review.ReviewerId.Should().Be(reviewerId);
        review.Decision.Should().Be(ContentReviewDecision.Approve);
        review.Feedback.Should().Be("Well written"); // trimmed
        review.Suggestions.Should().Be("{\"items\":[]}");
    }

    [Fact]
    public void Create_WithMinimalParams_ShouldHaveNulls()
    {
        var review = ContentVersionReview.Create(
            Guid.NewGuid(), Guid.NewGuid(),
            ContentReviewDecision.Pending);

        review.Feedback.Should().BeNull();
        review.Suggestions.Should().BeNull();
    }
}

#endregion

#region Enum Tests

public class ContentVersionStatusEnumTests
{
    [Theory]
    [InlineData(ContentVersionStatus.Draft, 0)]
    [InlineData(ContentVersionStatus.PendingReview, 1)]
    [InlineData(ContentVersionStatus.Approved, 2)]
    [InlineData(ContentVersionStatus.Rejected, 3)]
    [InlineData(ContentVersionStatus.Scheduled, 4)]
    [InlineData(ContentVersionStatus.Published, 5)]
    [InlineData(ContentVersionStatus.Archived, 6)]
    public void ContentVersionStatus_Values(ContentVersionStatus status, int expected)
    {
        ((int)status).Should().Be(expected);
    }

    [Fact]
    public void ContentVersionStatus_ShouldHave7Values()
    {
        Enum.GetValues<ContentVersionStatus>().Should().HaveCount(7);
    }
}

public class ContentReviewDecisionEnumTests
{
    [Theory]
    [InlineData(ContentReviewDecision.Pending, 0)]
    [InlineData(ContentReviewDecision.Approve, 1)]
    [InlineData(ContentReviewDecision.RequestChanges, 2)]
    [InlineData(ContentReviewDecision.Reject, 3)]
    public void ContentReviewDecision_Values(ContentReviewDecision decision, int expected)
    {
        ((int)decision).Should().Be(expected);
    }

    [Fact]
    public void ContentReviewDecision_ShouldHave4Values()
    {
        Enum.GetValues<ContentReviewDecision>().Should().HaveCount(4);
    }
}

#endregion

#region ContentVersionDiff Record Tests

public class ContentVersionDiffTests
{
    [Fact]
    public void ContentVersionDiff_ShouldSetAllProperties()
    {
        var v1Id = Guid.NewGuid();
        var v2Id = Guid.NewGuid();

        var diff = new ContentVersionDiff(
            v1Id, v2Id,
            Version1Number: 1, Version2Number: 2,
            TitleChanged: true, SummaryChanged: false,
            BodyChanged: true, MetadataChanged: false,
            TitleDiff: "old -> new", SummaryDiff: null, BodyDiff: "changes here");

        diff.Version1Id.Should().Be(v1Id);
        diff.Version2Id.Should().Be(v2Id);
        diff.Version1Number.Should().Be(1);
        diff.Version2Number.Should().Be(2);
        diff.TitleChanged.Should().BeTrue();
        diff.SummaryChanged.Should().BeFalse();
        diff.BodyChanged.Should().BeTrue();
        diff.TitleDiff.Should().Be("old -> new");
        diff.SummaryDiff.Should().BeNull();
        diff.BodyDiff.Should().Be("changes here");
    }

    [Fact]
    public void ContentVersionDiff_Equality_ShouldWorkByValue()
    {
        var v1 = Guid.NewGuid();
        var v2 = Guid.NewGuid();

        var diff1 = new ContentVersionDiff(v1, v2, 1, 2, true, false, true, false, null, null, null);
        var diff2 = new ContentVersionDiff(v1, v2, 1, 2, true, false, true, false, null, null, null);

        diff1.Should().Be(diff2);
    }
}

#endregion
