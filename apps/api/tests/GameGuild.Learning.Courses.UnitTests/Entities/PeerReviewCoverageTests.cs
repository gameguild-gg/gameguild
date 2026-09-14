using FluentAssertions;
using Xunit;

namespace GameGuild.Learning.Courses.UnitTests.Entities;

public sealed class PeerReviewCoverageTests
{
    [Fact]
    public void SubmitAndAcceptReview_RecordSubmissionAndResponse()
    {
        var review = new PeerReview();

        review.SubmitReview(87.5m, "Helpful feedback", """{"criterion":5}""");

        review.Status.Should().Be(PeerReviewStatus.Submitted);
        review.Grade.Should().Be(87.5m);
        review.Feedback.Should().Be("Helpful feedback");
        review.ReviewData.Should().Be("""{"criterion":5}""");
        review.SubmittedAt.Should().NotBeNull();

        review.AcceptReview("Applied the suggestions");

        review.Status.Should().Be(PeerReviewStatus.Accepted);
        review.IsAccepted.Should().BeTrue();
        review.AcceptanceReason.Should().Be("Applied the suggestions");
        review.ResponseAt.Should().NotBeNull();
        review.UpdatedAt.Should().NotBe(default);
    }

    [Fact]
    public void RejectReview_RecordsReasonAndResponse()
    {
        var review = new PeerReview();

        review.RejectReview("The review concerns another submission");

        review.Status.Should().Be(PeerReviewStatus.Rejected);
        review.IsAccepted.Should().BeFalse();
        review.AcceptanceReason.Should().Be("The review concerns another submission");
        review.ResponseAt.Should().NotBeNull();
        review.UpdatedAt.Should().NotBe(default);
    }

    [Theory]
    [InlineData(-10, 1)]
    [InlineData(3, 3)]
    [InlineData(20, 5)]
    public void RateReview_ClampsQualityToSupportedRange(int requested, int expected)
    {
        var review = new PeerReview();

        review.RateReview(requested);

        review.ReviewQuality.Should().Be(expected);
        review.UpdatedAt.Should().NotBe(default);
    }
}
