using FluentAssertions;
using Xunit;

namespace GameGuild.Social.Ratings.Tests;

/// <summary>
/// Unit tests for Rating entity domain logic.
/// </summary>
public class RatingEntityTests
{
    [Fact]
    public void Create_ShouldSetDefaultValues()
    {
        var userId = Guid.NewGuid();
        var entityId = Guid.NewGuid();

        var rating = Rating.Create(userId, entityId, "Course", 4, "Great course!", "Excellent");

        rating.Id.Should().NotBeEmpty();
        rating.UserId.Should().Be(userId);
        rating.EntityId.Should().Be(entityId);
        rating.EntityType.Should().Be("Course");
        rating.Value.Should().Be(4);
        rating.ReviewText.Should().Be("Great course!");
        rating.ReviewTitle.Should().Be("Excellent");
        rating.IsVerified.Should().BeFalse();
        rating.HelpfulCount.Should().Be(0);
        rating.ReportCount.Should().Be(0);
        rating.ModerationStatus.Should().Be(RatingModerationStatus.Approved);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    [InlineData(-1)]
    public void Create_WithInvalidValue_ShouldThrow(int value)
    {
        var act = () => Rating.Create(Guid.NewGuid(), Guid.NewGuid(), "Course", value);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Create_ShouldTrimStrings()
    {
        var rating = Rating.Create(Guid.NewGuid(), Guid.NewGuid(), " Course ", 3, " Review text ", " Title ");
        rating.EntityType.Should().Be("Course");
        rating.ReviewText.Should().Be("Review text");
        rating.ReviewTitle.Should().Be("Title");
    }

    [Fact]
    public void Update_ShouldChangeValueAndSetEditedAt()
    {
        var rating = Rating.Create(Guid.NewGuid(), Guid.NewGuid(), "Course", 3);
        rating.Update(5, "Updated review", "New title");

        rating.Value.Should().Be(5);
        rating.ReviewText.Should().Be("Updated review");
        rating.ReviewTitle.Should().Be("New title");
        rating.EditedAt.Should().NotBeNull();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    public void Update_WithInvalidValue_ShouldThrow(int value)
    {
        var rating = Rating.Create(Guid.NewGuid(), Guid.NewGuid(), "Course", 3);
        var act = () => rating.Update(value);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void MarkAsVerified_ShouldSetIsVerifiedTrue()
    {
        var rating = Rating.Create(Guid.NewGuid(), Guid.NewGuid(), "Course", 5);
        rating.MarkAsVerified();
        rating.IsVerified.Should().BeTrue();
    }

    [Fact]
    public void IncrementHelpful_ShouldIncreaseCount()
    {
        var rating = Rating.Create(Guid.NewGuid(), Guid.NewGuid(), "Course", 5);
        rating.IncrementHelpful();
        rating.IncrementHelpful();
        rating.HelpfulCount.Should().Be(2);
    }

    [Fact]
    public void DecrementHelpful_ShouldNotGoBelowZero()
    {
        var rating = Rating.Create(Guid.NewGuid(), Guid.NewGuid(), "Course", 5);
        rating.DecrementHelpful();
        rating.HelpfulCount.Should().Be(0);
    }

    [Fact]
    public void IncrementReport_ShouldIncreaseCount()
    {
        var rating = Rating.Create(Guid.NewGuid(), Guid.NewGuid(), "Course", 5);
        rating.IncrementReport();
        rating.ReportCount.Should().Be(1);
    }

    [Fact]
    public void SetModerationStatus_ShouldChangeStatus()
    {
        var rating = Rating.Create(Guid.NewGuid(), Guid.NewGuid(), "Course", 5);
        rating.SetModerationStatus(RatingModerationStatus.Flagged);
        rating.ModerationStatus.Should().Be(RatingModerationStatus.Flagged);
    }
}

/// <summary>
/// Unit tests for RatingSummary entity domain logic.
/// </summary>
public class RatingSummaryEntityTests
{
    [Fact]
    public void Create_ShouldSetDefaultValues()
    {
        var entityId = Guid.NewGuid();
        var summary = RatingSummary.Create(entityId, "Course");

        summary.EntityId.Should().Be(entityId);
        summary.EntityType.Should().Be("Course");
        summary.AverageRating.Should().Be(0);
        summary.TotalRatings.Should().Be(0);
        summary.TotalReviews.Should().Be(0);
    }

    [Fact]
    public void Recalculate_WithRatings_ShouldComputeCorrectStats()
    {
        var entityId = Guid.NewGuid();
        var summary = RatingSummary.Create(entityId, "Course");

        var ratings = new List<Rating>
        {
            Rating.Create(Guid.NewGuid(), entityId, "Course", 5, "Great!"),
            Rating.Create(Guid.NewGuid(), entityId, "Course", 4),
            Rating.Create(Guid.NewGuid(), entityId, "Course", 3, "Ok"),
            Rating.Create(Guid.NewGuid(), entityId, "Course", 5),
            Rating.Create(Guid.NewGuid(), entityId, "Course", 1)
        };

        summary.Recalculate(ratings);

        summary.TotalRatings.Should().Be(5);
        summary.OneStar.Should().Be(1);
        summary.TwoStar.Should().Be(0);
        summary.ThreeStar.Should().Be(1);
        summary.FourStar.Should().Be(1);
        summary.FiveStar.Should().Be(2);
        summary.TotalReviews.Should().Be(2); // only 2 have ReviewText
        summary.AverageRating.Should().Be(3.6m); // (5+4+3+5+1)/5 = 3.6
    }

    [Fact]
    public void Recalculate_WithNoRatings_ShouldBeZero()
    {
        var summary = RatingSummary.Create(Guid.NewGuid(), "Course");
        summary.Recalculate(new List<Rating>());

        summary.TotalRatings.Should().Be(0);
        summary.AverageRating.Should().Be(0);
    }

    [Fact]
    public void Recalculate_ShouldExcludeNonApprovedRatings()
    {
        var entityId = Guid.NewGuid();
        var summary = RatingSummary.Create(entityId, "Course");

        var approved = Rating.Create(Guid.NewGuid(), entityId, "Course", 5);
        var rejected = Rating.Create(Guid.NewGuid(), entityId, "Course", 1);
        rejected.SetModerationStatus(RatingModerationStatus.Rejected);

        summary.Recalculate(new List<Rating> { approved, rejected });

        summary.TotalRatings.Should().Be(1);
        summary.AverageRating.Should().Be(5.0m);
    }

    [Fact]
    public void GetDistributionPercentages_ShouldReturnCorrectPercentages()
    {
        var entityId = Guid.NewGuid();
        var summary = RatingSummary.Create(entityId, "Course");
        var ratings = new List<Rating>
        {
            Rating.Create(Guid.NewGuid(), entityId, "Course", 5),
            Rating.Create(Guid.NewGuid(), entityId, "Course", 5),
            Rating.Create(Guid.NewGuid(), entityId, "Course", 3),
            Rating.Create(Guid.NewGuid(), entityId, "Course", 1)
        };
        summary.Recalculate(ratings);

        var dist = summary.GetDistributionPercentages();

        dist[5].Should().Be(50.0); // 2/4 = 50%
        dist[3].Should().Be(25.0); // 1/4 = 25%
        dist[1].Should().Be(25.0);
        dist[2].Should().Be(0);
        dist[4].Should().Be(0);
    }

    [Fact]
    public void GetDistributionPercentages_WhenNoRatings_ShouldReturnZeros()
    {
        var summary = RatingSummary.Create(Guid.NewGuid(), "Course");
        summary.Recalculate(new List<Rating>());

        var dist = summary.GetDistributionPercentages();
        dist.Values.Should().AllSatisfy(v => v.Should().Be(0));
    }
}

/// <summary>
/// Unit tests for RatingHelpfulVote entity.
/// </summary>
public class RatingHelpfulVoteTests
{
    [Fact]
    public void Create_ShouldSetProperties()
    {
        var ratingId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var vote = RatingHelpfulVote.Create(ratingId, userId, true);

        vote.RatingId.Should().Be(ratingId);
        vote.UserId.Should().Be(userId);
        vote.IsHelpful.Should().BeTrue();
    }

    [Fact]
    public void UpdateVote_ShouldChangeIsHelpful()
    {
        var vote = RatingHelpfulVote.Create(Guid.NewGuid(), Guid.NewGuid(), true);
        vote.UpdateVote(false);
        vote.IsHelpful.Should().BeFalse();
    }
}
