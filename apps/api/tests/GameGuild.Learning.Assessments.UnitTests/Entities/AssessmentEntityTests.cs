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

    [Fact]
    public void Update_WithContentId_ShouldSetContentId()
    {
        var assessment = Assessment.Create(Guid.NewGuid(), "Title", AssessmentType.Quiz, 100, 50);
        var contentId = Guid.NewGuid();

        assessment.Update(null, null, null, null, null, null, null, null, null, contentId);

        assessment.ContentId.Should().Be(contentId);
    }

    [Fact]
    public void Update_WithClearContentId_ShouldClearContentId()
    {
        var assessment = Assessment.Create(Guid.NewGuid(), "Title", AssessmentType.Quiz, 100, 50);
        assessment.Update(null, null, null, null, null, null, null, null, null, Guid.NewGuid());

        assessment.Update(null, null, null, null, null, null, null, null, null, null, clearContentId: true);

        assessment.ContentId.Should().BeNull();
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

/// <summary>
/// Tests for AssessmentDto record and mapping.
/// </summary>
public class AssessmentDtoTests
{
    [Fact]
    public void FromEntity_ShouldMapAllProperties()
    {
        var assessment = Assessment.Create(Guid.NewGuid(), "Final Exam", AssessmentType.Exam, 100, 70);
        assessment.SetDescription("Comprehensive exam");
        assessment.SetTimeLimit(120);
        assessment.SetMaxAttempts(2);

        var dto = AssessmentDto.FromEntity(assessment);

        dto.Id.Should().Be(assessment.Id);
        dto.CourseId.Should().Be(assessment.CourseId);
        dto.Title.Should().Be("Final Exam");
        dto.Description.Should().Be("Comprehensive exam");
        dto.Type.Should().Be(AssessmentType.Exam);
        dto.MaxScore.Should().Be(100);
        dto.PassingScore.Should().Be(70);
        dto.TimeLimitMinutes.Should().Be(120);
        dto.MaxAttempts.Should().Be(2);
        dto.IsRequired.Should().BeTrue();
        dto.IsAvailable.Should().BeTrue();
    }

    [Fact]
    public void Constructor_ShouldSetAllProperties()
    {
        var id = Guid.NewGuid();
        var courseId = Guid.NewGuid();
        var dto = new AssessmentDto(id, courseId, null, "Quiz", "Desc",
            AssessmentType.Quiz, 50, 30, 15, 3, false, 1,
            DateTime.UtcNow, DateTime.UtcNow.AddDays(7), true);

        dto.Id.Should().Be(id);
        dto.CourseId.Should().Be(courseId);
        dto.ContentId.Should().BeNull();
        dto.Title.Should().Be("Quiz");
        dto.MaxScore.Should().Be(50);
        dto.TimeLimitMinutes.Should().Be(15);
        dto.MaxAttempts.Should().Be(3);
        dto.IsRequired.Should().BeFalse();
        dto.Order.Should().Be(1);
        dto.IsAvailable.Should().BeTrue();
    }
}

/// <summary>
/// Tests for AssessmentSubmissionDto record and mapping.
/// </summary>
public class AssessmentSubmissionDtoTests
{
    [Fact]
    public void FromEntity_ShouldMapAllProperties()
    {
        var submission = AssessmentSubmission.Start(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 2);
        submission.Submit();
        var graderId = Guid.NewGuid();
        submission.Grade(88, 70, graderId, "Excellent");

        var dto = AssessmentSubmissionDto.FromEntity(submission);

        dto.Id.Should().Be(submission.Id);
        dto.AssessmentId.Should().Be(submission.AssessmentId);
        dto.EnrollmentId.Should().Be(submission.EnrollmentId);
        dto.UserId.Should().Be(submission.UserId);
        dto.AttemptNumber.Should().Be(2);
        dto.Score.Should().Be(88);
        dto.Passed.Should().BeTrue();
        dto.SubmittedAt.Should().NotBeNull();
        dto.GradedAt.Should().NotBeNull();
        dto.GradedBy.Should().Be(graderId);
        dto.Feedback.Should().Be("Excellent");
        dto.Status.Should().Be(SubmissionStatus.Graded);
    }

    [Fact]
    public void FromEntity_InProgressSubmission_ShouldMapNullableFields()
    {
        var submission = AssessmentSubmission.Start(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 1);

        var dto = AssessmentSubmissionDto.FromEntity(submission);

        dto.Score.Should().BeNull();
        dto.Passed.Should().BeNull();
        dto.SubmittedAt.Should().BeNull();
        dto.GradedAt.Should().BeNull();
        dto.GradedBy.Should().BeNull();
        dto.Feedback.Should().BeNull();
        dto.Status.Should().Be(SubmissionStatus.InProgress);
    }
}

/// <summary>
/// Tests for request records.
/// </summary>
public class AssessmentRequestRecordTests
{
    [Fact]
    public void CreateAssessmentRequest_ShouldSetAllProperties()
    {
        var courseId = Guid.NewGuid();
        var request = new CreateAssessmentRequest(courseId, "Exam", "Final", AssessmentType.Exam,
            100, 70, 60, 3, true, DateTime.UtcNow, DateTime.UtcNow.AddDays(7));

        request.CourseId.Should().Be(courseId);
        request.Title.Should().Be("Exam");
        request.Description.Should().Be("Final");
        request.Type.Should().Be(AssessmentType.Exam);
        request.MaxScore.Should().Be(100);
        request.PassingScore.Should().Be(70);
        request.TimeLimitMinutes.Should().Be(60);
        request.MaxAttempts.Should().Be(3);
        request.IsRequired.Should().BeTrue();
    }

    [Fact]
    public void CreateAssessmentRequest_Defaults_ShouldBeCorrect()
    {
        var request = new CreateAssessmentRequest(Guid.NewGuid(), "Quiz", null, AssessmentType.Quiz, 50, 30);

        request.TimeLimitMinutes.Should().BeNull();
        request.MaxAttempts.Should().BeNull();
        request.IsRequired.Should().BeTrue();
        request.AvailableFrom.Should().BeNull();
        request.AvailableUntil.Should().BeNull();
    }

    [Fact]
    public void UpdateAssessmentRequest_ShouldSetAllProperties()
    {
        var request = new UpdateAssessmentRequest("New Title", "New Desc", 200, 100, 90, 5, false,
            DateTime.UtcNow, DateTime.UtcNow.AddDays(14));

        request.Title.Should().Be("New Title");
        request.Description.Should().Be("New Desc");
        request.MaxScore.Should().Be(200);
        request.PassingScore.Should().Be(100);
        request.TimeLimitMinutes.Should().Be(90);
        request.MaxAttempts.Should().Be(5);
        request.IsRequired.Should().BeFalse();
    }

    [Fact]
    public void UpdateAssessmentRequest_AllDefaults_ShouldBeNull()
    {
        var request = new UpdateAssessmentRequest();

        request.Title.Should().BeNull();
        request.Description.Should().BeNull();
        request.MaxScore.Should().BeNull();
        request.PassingScore.Should().BeNull();
        request.TimeLimitMinutes.Should().BeNull();
        request.MaxAttempts.Should().BeNull();
        request.IsRequired.Should().BeNull();
    }

    [Fact]
    public void GradeSubmissionRequest_ShouldSetAllProperties()
    {
        var graderId = Guid.NewGuid();
        var request = new GradeSubmissionRequest(85, graderId, "Well done");

        request.Score.Should().Be(85);
        request.GradedBy.Should().Be(graderId);
        request.Feedback.Should().Be("Well done");
    }

    [Fact]
    public void GradeSubmissionRequest_Defaults_ShouldBeNull()
    {
        var request = new GradeSubmissionRequest(70);

        request.GradedBy.Should().BeNull();
        request.Feedback.Should().BeNull();
    }
}
