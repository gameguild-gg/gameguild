using FluentAssertions;
using Xunit;

namespace GameGuild.Social.Ratings.Tests;

/// <summary>
/// Tests for rating domain events.
/// </summary>
public class RatingEventsTests
{
    [Fact]
    public void RatingCreatedEvent_ShouldStoreAllProperties()
    {
        var ratingId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var entityId = Guid.NewGuid();

        var evt = new RatingCreatedEvent(ratingId, userId, entityId, "Course", 5, true);

        evt.RatingId.Should().Be(ratingId);
        evt.UserId.Should().Be(userId);
        evt.EntityId.Should().Be(entityId);
        evt.EntityType.Should().Be("Course");
        evt.Value.Should().Be(5);
        evt.HasReview.Should().BeTrue();
    }

    [Fact]
    public void RatingUpdatedEvent_ShouldStoreAllProperties()
    {
        var ratingId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var entityId = Guid.NewGuid();

        var evt = new RatingUpdatedEvent(ratingId, userId, entityId, "Project", 3, 5);

        evt.RatingId.Should().Be(ratingId);
        evt.UserId.Should().Be(userId);
        evt.EntityId.Should().Be(entityId);
        evt.EntityType.Should().Be("Project");
        evt.OldValue.Should().Be(3);
        evt.NewValue.Should().Be(5);
    }

    [Fact]
    public void RatingDeletedEvent_ShouldStoreAllProperties()
    {
        var ratingId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var entityId = Guid.NewGuid();

        var evt = new RatingDeletedEvent(ratingId, userId, entityId, "Post");

        evt.RatingId.Should().Be(ratingId);
        evt.UserId.Should().Be(userId);
        evt.EntityId.Should().Be(entityId);
        evt.EntityType.Should().Be("Post");
    }

    [Fact]
    public void RatingReportedEvent_ShouldStoreAllProperties()
    {
        var ratingId = Guid.NewGuid();
        var reportedBy = Guid.NewGuid();

        var evt = new RatingReportedEvent(ratingId, reportedBy, "Spam", 3);

        evt.RatingId.Should().Be(ratingId);
        evt.ReportedByUserId.Should().Be(reportedBy);
        evt.Reason.Should().Be("Spam");
        evt.TotalReports.Should().Be(3);
    }

    [Fact]
    public void RatingModerationStatusChangedEvent_ShouldStoreAllProperties()
    {
        var ratingId = Guid.NewGuid();
        var evt = new RatingModerationStatusChangedEvent(
            ratingId, RatingModerationStatus.Pending, RatingModerationStatus.Approved);

        evt.RatingId.Should().Be(ratingId);
        evt.OldStatus.Should().Be(RatingModerationStatus.Pending);
        evt.NewStatus.Should().Be(RatingModerationStatus.Approved);
    }

    [Fact]
    public void RatingSummaryRecalculatedEvent_ShouldStoreAllProperties()
    {
        var entityId = Guid.NewGuid();
        var evt = new RatingSummaryRecalculatedEvent(entityId, "Course", 4.5m, 100);

        evt.EntityId.Should().Be(entityId);
        evt.EntityType.Should().Be("Course");
        evt.AverageRating.Should().Be(4.5m);
        evt.TotalRatings.Should().Be(100);
    }

    [Fact]
    public void RatingHelpfulVoteEvent_ShouldStoreAllProperties()
    {
        var ratingId = Guid.NewGuid();
        var voterId = Guid.NewGuid();
        var evt = new RatingHelpfulVoteEvent(ratingId, voterId, true);

        evt.RatingId.Should().Be(ratingId);
        evt.VoterUserId.Should().Be(voterId);
        evt.IsHelpful.Should().BeTrue();
    }

    [Fact]
    public void Events_ShouldSupportRecordEquality()
    {
        var id = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var entityId = Guid.NewGuid();

        var evt1 = new RatingCreatedEvent(id, userId, entityId, "Course", 5, true);
        var evt2 = new RatingCreatedEvent(id, userId, entityId, "Course", 5, true);
        var evt3 = new RatingCreatedEvent(id, userId, entityId, "Course", 4, true);

        evt1.Should().Be(evt2);
        evt1.Should().NotBe(evt3);
    }
}

/// <summary>
/// Tests for RatingModerationStatus enum.
/// </summary>
public class RatingModerationStatusTests
{
    [Theory]
    [InlineData(RatingModerationStatus.Pending, 0)]
    [InlineData(RatingModerationStatus.Approved, 1)]
    [InlineData(RatingModerationStatus.Rejected, 2)]
    [InlineData(RatingModerationStatus.Flagged, 3)]
    public void RatingModerationStatus_ShouldHaveCorrectValues(RatingModerationStatus status, int expected)
    {
        ((int)status).Should().Be(expected);
    }

    [Fact]
    public void RatingModerationStatus_ShouldHave4Values()
    {
        Enum.GetValues<RatingModerationStatus>().Should().HaveCount(4);
    }
}

/// <summary>
/// Tests for RatingSortOrder enum.
/// </summary>
public class RatingSortOrderTests
{
    [Theory]
    [InlineData(RatingSortOrder.MostRecent, 0)]
    [InlineData(RatingSortOrder.Oldest, 1)]
    [InlineData(RatingSortOrder.HighestRating, 2)]
    [InlineData(RatingSortOrder.LowestRating, 3)]
    [InlineData(RatingSortOrder.MostHelpful, 4)]
    public void RatingSortOrder_ShouldHaveCorrectValues(RatingSortOrder order, int expected)
    {
        ((int)order).Should().Be(expected);
    }

    [Fact]
    public void RatingSortOrder_ShouldHave5Values()
    {
        Enum.GetValues<RatingSortOrder>().Should().HaveCount(5);
    }
}

/// <summary>
/// Tests for RatingDto and RatingSummaryDto.
/// </summary>
public class RatingDtoTests
{
    [Fact]
    public void RatingDto_FromEntity_ShouldMapAllProperties()
    {
        var userId = Guid.NewGuid();
        var entityId = Guid.NewGuid();
        var rating = Rating.Create(userId, entityId, "Course", 4, "Great!", "Title");
        rating.MarkAsVerified();
        rating.IncrementHelpful();
        rating.SetModerationStatus(RatingModerationStatus.Flagged);

        var dto = RatingDto.FromEntity(rating);

        dto.Id.Should().Be(rating.Id);
        dto.UserId.Should().Be(userId);
        dto.EntityId.Should().Be(entityId);
        dto.EntityType.Should().Be("Course");
        dto.Value.Should().Be(4);
        dto.ReviewTitle.Should().Be("Title");
        dto.ReviewText.Should().Be("Great!");
        dto.IsVerified.Should().BeTrue();
        dto.HelpfulCount.Should().Be(1);
        dto.ModerationStatus.Should().Be(RatingModerationStatus.Flagged);
        dto.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        dto.EditedAt.Should().BeNull();
    }

    [Fact]
    public void RatingDto_FromEntity_ShouldIncludeEditedAt()
    {
        var rating = Rating.Create(Guid.NewGuid(), Guid.NewGuid(), "Course", 3);
        rating.Update(5, "Updated");

        var dto = RatingDto.FromEntity(rating);

        dto.Value.Should().Be(5);
        dto.EditedAt.Should().NotBeNull();
    }

    [Fact]
    public void RatingDto_RecordEquality_ShouldWork()
    {
        var id = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var entityId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        var dto1 = new RatingDto(id, userId, entityId, "Course", 5, "Title", "Text", true, 10, RatingModerationStatus.Approved, now, null);
        var dto2 = new RatingDto(id, userId, entityId, "Course", 5, "Title", "Text", true, 10, RatingModerationStatus.Approved, now, null);

        dto1.Should().Be(dto2);
    }

    [Fact]
    public void RatingSummaryDto_FromEntity_ShouldMapAllProperties()
    {
        var entityId = Guid.NewGuid();
        var summary = RatingSummary.Create(entityId, "Course");
        var ratings = new List<Rating>
        {
            Rating.Create(Guid.NewGuid(), entityId, "Course", 5, "Great!"),
            Rating.Create(Guid.NewGuid(), entityId, "Course", 4),
            Rating.Create(Guid.NewGuid(), entityId, "Course", 3)
        };
        summary.Recalculate(ratings);

        var dto = RatingSummaryDto.FromEntity(summary);

        dto.EntityId.Should().Be(entityId);
        dto.EntityType.Should().Be("Course");
        dto.AverageRating.Should().Be(4.0m);
        dto.TotalRatings.Should().Be(3);
        dto.OneStar.Should().Be(0);
        dto.TwoStar.Should().Be(0);
        dto.ThreeStar.Should().Be(1);
        dto.FourStar.Should().Be(1);
        dto.FiveStar.Should().Be(1);
        dto.TotalReviews.Should().Be(1);
        dto.Distribution.Should().HaveCount(5);
    }

    [Fact]
    public void RatingSummaryDto_FromEntity_EmptySummary_ShouldReturnZeros()
    {
        var summary = RatingSummary.Create(Guid.NewGuid(), "Course");
        summary.Recalculate(Array.Empty<Rating>());

        var dto = RatingSummaryDto.FromEntity(summary);

        dto.AverageRating.Should().Be(0);
        dto.TotalRatings.Should().Be(0);
        dto.Distribution.Should().HaveCount(5);
        dto.Distribution.Values.Should().AllSatisfy(v => v.Should().Be(0));
    }
}

/// <summary>
/// Tests for request DTOs.
/// </summary>
public class RequestDtoTests
{
    [Fact]
    public void CreateRatingRequest_ShouldStoreAllProperties()
    {
        var entityId = Guid.NewGuid();
        var request = new CreateRatingRequest(entityId, "Course", 4, "Review", "Title");

        request.EntityId.Should().Be(entityId);
        request.EntityType.Should().Be("Course");
        request.Value.Should().Be(4);
        request.ReviewText.Should().Be("Review");
        request.ReviewTitle.Should().Be("Title");
    }

    [Fact]
    public void CreateRatingRequest_OptionalFieldsShouldDefaultToNull()
    {
        var request = new CreateRatingRequest(Guid.NewGuid(), "Course", 5);

        request.ReviewText.Should().BeNull();
        request.ReviewTitle.Should().BeNull();
    }

    [Fact]
    public void BatchSummaryRequest_ShouldStoreProperties()
    {
        var ids = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() };
        var request = new BatchSummaryRequest(ids, "Course");

        request.EntityIds.Should().BeEquivalentTo(ids);
        request.EntityType.Should().Be("Course");
    }

    [Fact]
    public void VoteHelpfulRequest_ShouldStoreIsHelpful()
    {
        var request = new VoteHelpfulRequest(true);
        request.IsHelpful.Should().BeTrue();

        var request2 = new VoteHelpfulRequest(false);
        request2.IsHelpful.Should().BeFalse();
    }

    [Fact]
    public void ReportRequest_ShouldStoreReason()
    {
        var request = new ReportRequest("Spam content");
        request.Reason.Should().Be("Spam content");
    }
}

/// <summary>
/// Tests for RatingErrors static class.
/// </summary>
public class RatingErrorsTests
{
    [Fact]
    public void NotFound_ShouldReturnNotFoundError()
    {
        var error = RatingErrors.NotFound;

        error.Code.Should().Be("Rating.NotFound");
        error.Description.Should().Be("Rating not found");
    }

    [Fact]
    public void VoteNotFound_ShouldReturnNotFoundError()
    {
        var error = RatingErrors.VoteNotFound;

        error.Code.Should().Be("Rating.VoteNotFound");
        error.Description.Should().Be("Helpful vote not found");
    }

    [Fact]
    public void CannotVoteOwnRating_ShouldReturnFailureError()
    {
        var error = RatingErrors.CannotVoteOwnRating;

        error.Code.Should().Be("Rating.CannotVoteOwnRating");
        error.Description.Should().Be("You cannot vote on your own rating");
    }

    [Fact]
    public void AlreadyRated_ShouldReturnFailureError()
    {
        var error = RatingErrors.AlreadyRated;

        error.Code.Should().Be("Rating.AlreadyRated");
        error.Description.Should().Be("You have already rated this item");
    }

    [Fact]
    public void InvalidValue_ShouldReturnValidationError()
    {
        var error = RatingErrors.InvalidValue;

        error.Code.Should().Be("Rating.InvalidValue");
        error.Description.Should().Be("Rating value must be between 1 and 5");
    }
}

/// <summary>
/// Additional entity edge cases.
/// </summary>
public class RatingEntityEdgeCaseTests
{
    [Fact]
    public void Create_WithIsVerifiedTrue_ShouldSetVerified()
    {
        var rating = Rating.Create(Guid.NewGuid(), Guid.NewGuid(), "Course", 5,
            isVerified: true);

        rating.IsVerified.Should().BeTrue();
    }

    [Fact]
    public void Create_WithNullReviewFields_ShouldBeNull()
    {
        var rating = Rating.Create(Guid.NewGuid(), Guid.NewGuid(), "Course", 3);

        rating.ReviewText.Should().BeNull();
        rating.ReviewTitle.Should().BeNull();
    }

    [Fact]
    public void DecrementHelpful_FromPositiveCount_ShouldDecrease()
    {
        var rating = Rating.Create(Guid.NewGuid(), Guid.NewGuid(), "Course", 5);
        rating.IncrementHelpful();
        rating.IncrementHelpful();
        rating.IncrementHelpful();
        rating.DecrementHelpful();

        rating.HelpfulCount.Should().Be(2);
    }

    [Fact]
    public void Update_ShouldTrimStrings()
    {
        var rating = Rating.Create(Guid.NewGuid(), Guid.NewGuid(), "Course", 3);
        rating.Update(4, " Updated review ", " New title ");

        rating.ReviewText.Should().Be("Updated review");
        rating.ReviewTitle.Should().Be("New title");
    }

    [Fact]
    public void Update_WithNullReview_ShouldClearReview()
    {
        var rating = Rating.Create(Guid.NewGuid(), Guid.NewGuid(), "Course", 3, "Initial review", "Title");
        rating.Update(4);

        rating.ReviewText.Should().BeNull();
        rating.ReviewTitle.Should().BeNull();
    }

    [Fact]
    public void Create_AllStarValues_ShouldBeValid()
    {
        for (int i = 1; i <= 5; i++)
        {
            var rating = Rating.Create(Guid.NewGuid(), Guid.NewGuid(), "Course", i);
            rating.Value.Should().Be(i);
        }
    }

    [Fact]
    public void SetModerationStatus_AllValues_ShouldWork()
    {
        var rating = Rating.Create(Guid.NewGuid(), Guid.NewGuid(), "Course", 5);

        foreach (var status in Enum.GetValues<RatingModerationStatus>())
        {
            rating.SetModerationStatus(status);
            rating.ModerationStatus.Should().Be(status);
        }
    }

    [Fact]
    public void RatingHelpfulVote_Create_WithNotHelpful_ShouldSetFalse()
    {
        var vote = RatingHelpfulVote.Create(Guid.NewGuid(), Guid.NewGuid(), false);
        vote.IsHelpful.Should().BeFalse();
    }

    [Fact]
    public void RatingHelpfulVote_UpdateVote_ShouldToggle()
    {
        var vote = RatingHelpfulVote.Create(Guid.NewGuid(), Guid.NewGuid(), false);
        vote.UpdateVote(true);
        vote.IsHelpful.Should().BeTrue();
        vote.UpdateVote(false);
        vote.IsHelpful.Should().BeFalse();
    }

    [Fact]
    public void RatingSummary_Create_ShouldTrimEntityType()
    {
        var summary = RatingSummary.Create(Guid.NewGuid(), " Course ");
        summary.EntityType.Should().Be("Course");
    }
}
