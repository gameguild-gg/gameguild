using FluentAssertions;
using Xunit;

namespace GameGuild.Learning.Assessments.Tests;

/// <summary>
/// Unit tests for Assessment entity domain logic.
/// </summary>
public class AssessmentEntityTests
{
    [Fact]
    public void Create_ShouldSetDefaultValues()
    {
        var courseId = Guid.NewGuid();
        var assessment = Assessment.Create(courseId, "Midterm Exam", AssessmentType.Exam, 100, 70);

        assessment.Id.Should().NotBeEmpty();
        assessment.CourseId.Should().Be(courseId);
        assessment.Title.Should().Be("Midterm Exam");
        assessment.Type.Should().Be(AssessmentType.Exam);
        assessment.MaxScore.Should().Be(100);
        assessment.PassingScore.Should().Be(70);
        assessment.IsRequired.Should().BeTrue();
        assessment.Order.Should().Be(0);
        assessment.TimeLimitMinutes.Should().BeNull();
        assessment.MaxAttempts.Should().BeNull();
    }

    [Fact]
    public void Create_WithIsRequiredFalse_ShouldSetFalse()
    {
        var assessment = Assessment.Create(Guid.NewGuid(), "Quiz", AssessmentType.Quiz, 50, 25, isRequired: false);
        assessment.IsRequired.Should().BeFalse();
    }

    [Fact]
    public void IsAvailable_WhenNoDateRestrictions_ShouldReturnTrue()
    {
        var assessment = Assessment.Create(Guid.NewGuid(), "Test", AssessmentType.Quiz, 100, 50);
        assessment.IsAvailable().Should().BeTrue();
    }

    [Fact]
    public void IsAvailable_WhenBeforeAvailableFrom_ShouldReturnFalse()
    {
        var assessment = Assessment.Create(Guid.NewGuid(), "Test", AssessmentType.Quiz, 100, 50);
        assessment.SetAvailability(DateTime.UtcNow.AddDays(1), null);
        assessment.IsAvailable().Should().BeFalse();
    }

    [Fact]
    public void IsAvailable_WhenAfterAvailableUntil_ShouldReturnFalse()
    {
        var assessment = Assessment.Create(Guid.NewGuid(), "Test", AssessmentType.Quiz, 100, 50);
        assessment.SetAvailability(null, DateTime.UtcNow.AddDays(-1));
        assessment.IsAvailable().Should().BeFalse();
    }

    [Fact]
    public void IsAvailable_WhenWithinWindow_ShouldReturnTrue()
    {
        var assessment = Assessment.Create(Guid.NewGuid(), "Test", AssessmentType.Quiz, 100, 50);
        assessment.SetAvailability(DateTime.UtcNow.AddDays(-1), DateTime.UtcNow.AddDays(1));
        assessment.IsAvailable().Should().BeTrue();
    }

    [Fact]
    public void SetDescription_ShouldUpdateDescription()
    {
        var assessment = Assessment.Create(Guid.NewGuid(), "Test", AssessmentType.Quiz, 100, 50);
        assessment.SetDescription("A comprehensive quiz");
        assessment.Description.Should().Be("A comprehensive quiz");
    }

    [Fact]
    public void SetTimeLimit_ShouldUpdateTimeLimit()
    {
        var assessment = Assessment.Create(Guid.NewGuid(), "Test", AssessmentType.Exam, 100, 50);
        assessment.SetTimeLimit(90);
        assessment.TimeLimitMinutes.Should().Be(90);
    }

    [Fact]
    public void SetMaxAttempts_ShouldUpdateMaxAttempts()
    {
        var assessment = Assessment.Create(Guid.NewGuid(), "Test", AssessmentType.Quiz, 100, 50);
        assessment.SetMaxAttempts(3);
        assessment.MaxAttempts.Should().Be(3);
    }

    [Fact]
    public void Update_ShouldModifyMultipleFields()
    {
        var assessment = Assessment.Create(Guid.NewGuid(), "Old Title", AssessmentType.Quiz, 100, 50);
        assessment.Update("New Title", "New Desc", 200, 100, 60, 5, false, null, null);

        assessment.Title.Should().Be("New Title");
        assessment.Description.Should().Be("New Desc");
        assessment.MaxScore.Should().Be(200);
        assessment.PassingScore.Should().Be(100);
        assessment.TimeLimitMinutes.Should().Be(60);
        assessment.MaxAttempts.Should().Be(5);
        assessment.IsRequired.Should().BeFalse();
    }
}

/// <summary>
/// Unit tests for AssessmentSubmission entity domain logic.
/// </summary>
public class AssessmentSubmissionEntityTests
{
    [Fact]
    public void Start_ShouldSetDefaultValues()
    {
        var assessmentId = Guid.NewGuid();
        var enrollmentId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var submission = AssessmentSubmission.Start(assessmentId, enrollmentId, userId, 1);

        submission.Id.Should().NotBeEmpty();
        submission.AssessmentId.Should().Be(assessmentId);
        submission.EnrollmentId.Should().Be(enrollmentId);
        submission.UserId.Should().Be(userId);
        submission.AttemptNumber.Should().Be(1);
        submission.Status.Should().Be(SubmissionStatus.InProgress);
        submission.StartedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
        submission.Score.Should().BeNull();
        submission.Passed.Should().BeNull();
        submission.SubmittedAt.Should().BeNull();
    }

    [Fact]
    public void Submit_ShouldChangeStatusAndSetSubmittedAt()
    {
        var submission = AssessmentSubmission.Start(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 1);
        submission.Submit();

        submission.Status.Should().Be(SubmissionStatus.Submitted);
        submission.SubmittedAt.Should().NotBeNull();
    }

    [Fact]
    public void Grade_ShouldSetScoreAndPassStatus_WhenPassing()
    {
        var submission = AssessmentSubmission.Start(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 1);
        submission.Submit();

        var graderId = Guid.NewGuid();
        submission.Grade(85, 70, graderId, "Good work!");

        submission.Score.Should().Be(85);
        submission.Passed.Should().BeTrue();
        submission.Status.Should().Be(SubmissionStatus.Graded);
        submission.GradedAt.Should().NotBeNull();
        submission.GradedBy.Should().Be(graderId);
        submission.Feedback.Should().Be("Good work!");
    }

    [Fact]
    public void Grade_ShouldSetPassedFalse_WhenFailing()
    {
        var submission = AssessmentSubmission.Start(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 1);
        submission.Submit();
        submission.Grade(50, 70);

        submission.Passed.Should().BeFalse();
    }

    [Fact]
    public void Grade_AtExactPassingScore_ShouldPass()
    {
        var submission = AssessmentSubmission.Start(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 1);
        submission.Submit();
        submission.Grade(70, 70);

        submission.Passed.Should().BeTrue();
    }
}
