using FluentAssertions;
using Xunit;

namespace GameGuild.Learning.Assessments.Tests;

/// <summary>
/// Unit tests for AssessmentPeerReview entity + assessment/submission peer-review extensions.
/// </summary>
public class AssessmentPeerReviewEntityTests
{
    [Fact]
    public void Create_ShouldSetFieldsAndDefaultToAssigned()
    {
        var assessmentId = Guid.NewGuid();
        var submissionId = Guid.NewGuid();
        var reviewerUserId = Guid.NewGuid();

        var review = AssessmentPeerReview.Create(assessmentId, submissionId, reviewerUserId);

        review.Id.Should().NotBeEmpty();
        review.AssessmentId.Should().Be(assessmentId);
        review.SubmissionId.Should().Be(submissionId);
        review.ReviewerUserId.Should().Be(reviewerUserId);
        review.Status.Should().Be(PeerReviewStatus.Assigned);
        review.AssignedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
        review.SubmittedAt.Should().BeNull();
        review.Score.Should().BeNull();
        review.Feedback.Should().BeNull();
        review.RubricScoresPayload.Should().BeNull();
    }

    [Fact]
    public void SubmitReview_ShouldSetScoreFeedbackRubricScoresAndMarkSubmitted()
    {
        var review = AssessmentPeerReview.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        var rubricScores = "{\"c1\":{\"points\":4,\"comment\":\"clear thesis\"}}";

        review.SubmitReview(4, "Nice work", rubricScores);

        review.Status.Should().Be(PeerReviewStatus.Submitted);
        review.Score.Should().Be(4);
        review.Feedback.Should().Be("Nice work");
        review.RubricScoresPayload.Should().Be(rubricScores);
        review.SubmittedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
    }

    [Fact]
    public void SubmitReview_WhenAlreadySubmitted_Throws()
    {
        var review = AssessmentPeerReview.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        review.SubmitReview(3, "ok", null);

        var action = () => review.SubmitReview(5, "again", null);

        action.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Create_DefaultsPeerReviewsRequiredCountToZero()
    {
        var assessment = Assessment.Create(Guid.NewGuid(), "Peer essay", AssessmentType.Assignment, 100);

        assessment.PeerReviewsRequiredCount.Should().Be(0);
    }

    [Fact]
    public void SetPeerReviewPolicy_WithValidCount_SetsCount()
    {
        var assessment = Assessment.Create(Guid.NewGuid(), "Peer essay", AssessmentType.Assignment, 100);

        assessment.SetPeerReviewPolicy(3);

        assessment.PeerReviewsRequiredCount.Should().Be(3);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void SetPeerReviewPolicy_WithCountBelowOne_Throws(int requiredCount)
    {
        var assessment = Assessment.Create(Guid.NewGuid(), "Peer essay", AssessmentType.Assignment, 100);

        var action = () => assessment.SetPeerReviewPolicy(requiredCount);

        action.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void StampCourseGroup_ShouldSetCourseGroupIdAndTouchUpdatedAt()
    {
        var submission = AssessmentSubmission.Start(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 1);
        var originalUpdatedAt = submission.UpdatedAt;
        Thread.Sleep(20);
        var courseGroupId = Guid.NewGuid();

        submission.StampCourseGroup(courseGroupId);

        submission.CourseGroupId.Should().Be(courseGroupId);
        submission.UpdatedAt.Should().BeAfter(originalUpdatedAt);
    }

    [Fact]
    public void Grade_WithRubricScores_ShouldPersistRubricScoresPayloadOnSubmittedSubmission()
    {
        var submission = AssessmentSubmission.Start(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 1);
        submission.SetPayload(new SubmitAssessmentRequest(TextPayload: "My essay"), SubmissionModality.Text);
        submission.Submit();
        var rubricScores = "{\"c1\":{\"points\":5,\"comment\":\"thesis\"},\"c2\":{\"points\":3}}";

        submission.Grade(8, 5, 10, gradedBy: Guid.NewGuid(), feedback: "Solid", rubricScores: rubricScores);

        submission.Score.Should().Be(8);
        submission.Status.Should().Be(SubmissionStatus.Graded);
        submission.Feedback.Should().Be("Solid");
        submission.RubricScoresPayload.Should().Be(rubricScores);
    }
}
