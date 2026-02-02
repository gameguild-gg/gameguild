using FluentAssertions;
using GameGuild.Resources.Contents;
using Xunit;

namespace GameGuild.Contents.UnitTests.Entities;

/// <summary>
/// Unit tests for ContentVersion entity
/// </summary>
public class ContentVersionEntityTests
{
    [Fact]
    public void Create_WithValidParameters_ShouldCreateContentVersion()
    {
        // Arrange
        var entityId = Guid.NewGuid();
        var entityType = "Course";
        var versionNumber = 1;
        var title = "My Course Title";
        var createdBy = Guid.NewGuid();

        // Act
        var contentVersion = ContentVersion.Create(entityId, entityType, versionNumber, title, createdBy);

        // Assert
        contentVersion.EntityId.Should().Be(entityId);
        contentVersion.EntityType.Should().Be(entityType);
        contentVersion.VersionNumber.Should().Be(versionNumber);
        contentVersion.Title.Should().Be(title);
        contentVersion.CreatedBy.Should().Be(createdBy);
        contentVersion.Status.Should().Be(ContentVersionStatus.Draft);
        contentVersion.IsCurrentVersion.Should().BeFalse();
    }

    [Fact]
    public void Create_WithOptionalParameters_ShouldSetAllFields()
    {
        // Arrange
        var entityId = Guid.NewGuid();
        var entityType = "Project";
        var versionNumber = 2;
        var title = "Project Title";
        var createdBy = Guid.NewGuid();
        var summary = "This is a summary";
        var body = "Full content body";
        var metadata = "{\"key\":\"value\"}";
        var changeNotes = "Initial version";

        // Act
        var contentVersion = ContentVersion.Create(
            entityId, entityType, versionNumber, title, createdBy,
            summary, body, metadata, changeNotes);

        // Assert
        contentVersion.Summary.Should().Be(summary);
        contentVersion.Body.Should().Be(body);
        contentVersion.Metadata.Should().Be(metadata);
        contentVersion.ChangeNotes.Should().Be(changeNotes);
    }

    [Fact]
    public void Create_ShouldTrimStrings()
    {
        // Arrange
        var entityId = Guid.NewGuid();
        var entityType = "  Course  ";
        var title = "  Title with spaces  ";
        var createdBy = Guid.NewGuid();

        // Act
        var contentVersion = ContentVersion.Create(entityId, entityType, 1, title, createdBy);

        // Assert
        contentVersion.EntityType.Should().Be("Course");
        contentVersion.Title.Should().Be("Title with spaces");
    }

    [Fact]
    public void UpdateDraft_WhenStatusIsDraft_ShouldUpdateFields()
    {
        // Arrange
        var contentVersion = ContentVersion.Create(Guid.NewGuid(), "Course", 1, "Original Title", Guid.NewGuid());

        // Act
        contentVersion.UpdateDraft(title: "Updated Title", summary: "New Summary");

        // Assert
        contentVersion.Title.Should().Be("Updated Title");
        contentVersion.Summary.Should().Be("New Summary");
    }

    [Fact]
    public void UpdateDraft_WhenStatusIsNotDraft_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var contentVersion = ContentVersion.Create(Guid.NewGuid(), "Course", 1, "Title", Guid.NewGuid());
        contentVersion.SubmitForReview(Guid.NewGuid());

        // Act
        var act = () => contentVersion.UpdateDraft(title: "New Title");

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*only update draft*");
    }

    [Fact]
    public void SubmitForReview_WhenStatusIsDraft_ShouldTransitionToPendingReview()
    {
        // Arrange
        var contentVersion = ContentVersion.Create(Guid.NewGuid(), "Course", 1, "Title", Guid.NewGuid());
        var submittedBy = Guid.NewGuid();

        // Act
        contentVersion.SubmitForReview(submittedBy);

        // Assert
        contentVersion.Status.Should().Be(ContentVersionStatus.PendingReview);
        contentVersion.SubmittedBy.Should().Be(submittedBy);
        contentVersion.SubmittedForReviewAt.Should().NotBeNull();
    }

    [Fact]
    public void SubmitForReview_WhenStatusIsNotDraft_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var contentVersion = ContentVersion.Create(Guid.NewGuid(), "Course", 1, "Title", Guid.NewGuid());
        contentVersion.SubmitForReview(Guid.NewGuid());

        // Act
        var act = () => contentVersion.SubmitForReview(Guid.NewGuid());

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*only submit drafts*");
    }

    [Fact]
    public void Approve_WhenStatusIsPendingReview_ShouldTransitionToApproved()
    {
        // Arrange
        var contentVersion = ContentVersion.Create(Guid.NewGuid(), "Course", 1, "Title", Guid.NewGuid());
        contentVersion.SubmitForReview(Guid.NewGuid());
        var reviewedBy = Guid.NewGuid();
        var reviewNotes = "Looks good!";

        // Act
        contentVersion.Approve(reviewedBy, reviewNotes);

        // Assert
        contentVersion.Status.Should().Be(ContentVersionStatus.Approved);
        contentVersion.ReviewedBy.Should().Be(reviewedBy);
        contentVersion.ReviewedAt.Should().NotBeNull();
        contentVersion.ReviewNotes.Should().Be(reviewNotes);
    }

    [Fact]
    public void Approve_WhenStatusIsNotPendingReview_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var contentVersion = ContentVersion.Create(Guid.NewGuid(), "Course", 1, "Title", Guid.NewGuid());

        // Act
        var act = () => contentVersion.Approve(Guid.NewGuid());

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*only approve versions pending review*");
    }

    [Fact]
    public void Reject_WhenStatusIsPendingReview_ShouldTransitionToRejected()
    {
        // Arrange
        var contentVersion = ContentVersion.Create(Guid.NewGuid(), "Course", 1, "Title", Guid.NewGuid());
        contentVersion.SubmitForReview(Guid.NewGuid());
        var reviewedBy = Guid.NewGuid();
        var reviewNotes = "Needs more work";

        // Act
        contentVersion.Reject(reviewedBy, reviewNotes);

        // Assert
        contentVersion.Status.Should().Be(ContentVersionStatus.Rejected);
        contentVersion.ReviewedBy.Should().Be(reviewedBy);
        contentVersion.ReviewNotes.Should().Be(reviewNotes);
    }

    [Fact]
    public void Publish_WhenStatusIsApproved_ShouldTransitionToPublished()
    {
        // Arrange
        var contentVersion = ContentVersion.Create(Guid.NewGuid(), "Course", 1, "Title", Guid.NewGuid());
        contentVersion.SubmitForReview(Guid.NewGuid());
        contentVersion.Approve(Guid.NewGuid());
        var publishedBy = Guid.NewGuid();

        // Act
        contentVersion.Publish(publishedBy);

        // Assert
        contentVersion.Status.Should().Be(ContentVersionStatus.Published);
        contentVersion.PublishedBy.Should().Be(publishedBy);
        contentVersion.PublishedAt.Should().NotBeNull();
        contentVersion.IsCurrentVersion.Should().BeTrue();
    }

    [Fact]
    public void Publish_WhenStatusIsNotApprovedOrScheduled_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var contentVersion = ContentVersion.Create(Guid.NewGuid(), "Course", 1, "Title", Guid.NewGuid());

        // Act
        var act = () => contentVersion.Publish(Guid.NewGuid());

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*only publish approved or scheduled*");
    }

    [Fact]
    public void SchedulePublish_WhenStatusIsApproved_ShouldTransitionToScheduled()
    {
        // Arrange
        var contentVersion = ContentVersion.Create(Guid.NewGuid(), "Course", 1, "Title", Guid.NewGuid());
        contentVersion.SubmitForReview(Guid.NewGuid());
        contentVersion.Approve(Guid.NewGuid());
        var scheduledAt = DateTime.UtcNow.AddDays(7);
        var scheduledBy = Guid.NewGuid();

        // Act
        contentVersion.SchedulePublish(scheduledAt, scheduledBy);

        // Assert
        contentVersion.Status.Should().Be(ContentVersionStatus.Scheduled);
        contentVersion.ScheduledPublishAt.Should().Be(scheduledAt);
        contentVersion.PublishedBy.Should().Be(scheduledBy);
    }

    [Fact]
    public void Archive_ShouldTransitionToArchivedAndClearCurrent()
    {
        // Arrange
        var contentVersion = ContentVersion.Create(Guid.NewGuid(), "Course", 1, "Title", Guid.NewGuid());

        // Act
        contentVersion.Archive();

        // Assert
        contentVersion.Status.Should().Be(ContentVersionStatus.Archived);
        contentVersion.IsCurrentVersion.Should().BeFalse();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void SetAsCurrent_ShouldUpdateIsCurrentVersion(bool isCurrent)
    {
        // Arrange
        var contentVersion = ContentVersion.Create(Guid.NewGuid(), "Course", 1, "Title", Guid.NewGuid());

        // Act
        contentVersion.SetAsCurrent(isCurrent);

        // Assert
        contentVersion.IsCurrentVersion.Should().Be(isCurrent);
    }
}

/// <summary>
/// Unit tests for ContentVersionStatus enum
/// </summary>
public class ContentVersionStatusEnumTests
{
    [Theory]
    [InlineData(ContentVersionStatus.Draft)]
    [InlineData(ContentVersionStatus.PendingReview)]
    [InlineData(ContentVersionStatus.Approved)]
    [InlineData(ContentVersionStatus.Rejected)]
    [InlineData(ContentVersionStatus.Published)]
    [InlineData(ContentVersionStatus.Archived)]
    [InlineData(ContentVersionStatus.Scheduled)]
    public void ContentVersionStatus_AllValues_ShouldBeDefined(ContentVersionStatus status)
    {
        // Assert
        Enum.IsDefined(typeof(ContentVersionStatus), status).Should().BeTrue();
    }

    [Fact]
    public void ContentVersionStatus_ShouldHaveExpectedCount()
    {
        // Assert
        Enum.GetValues<ContentVersionStatus>().Should().HaveCountGreaterOrEqualTo(6);
    }
}
