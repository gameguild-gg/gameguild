using FluentAssertions;
using GameGuild.Learning.Courses;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using System.Reflection;
using System.Text.Json;
using Xunit;

namespace GameGuild.Learning.Assessments.Tests;

/// <summary>
/// Unit tests for Assessment entity domain logic.
/// </summary>
public class AssessmentEntityTests
{
    [Fact]
    public void Create_ShouldNormalizeLegacyExamToQuizAndSetDefaultValues()
    {
        var courseId = Guid.NewGuid();
        var assessment = Assessment.Create(courseId, "Midterm Exam", AssessmentType.Exam, 100, 70);

        assessment.Id.Should().NotBeEmpty();
        assessment.CourseId.Should().Be(courseId);
        assessment.Title.Should().Be("Midterm Exam");
        assessment.Type.Should().Be(AssessmentType.Quiz);
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
    public void Create_WithContentId_ShouldLinkAssessmentToContent()
    {
        var contentId = Guid.NewGuid();

        var assessment = Assessment.Create(
            Guid.NewGuid(),
            "Linked quiz",
            AssessmentType.Quiz,
            100,
            70,
            contentId: contentId);

        assessment.ContentId.Should().Be(contentId);
    }

    [Fact]
    public void Create_WithoutContentId_ShouldDefaultToNull()
    {
        var assessment = Assessment.Create(Guid.NewGuid(), "Quiz", AssessmentType.Quiz, 100, 70);

        assessment.ContentId.Should().BeNull();
    }

    [Fact]
    public void Create_WithGradingMethods_ShouldPersistBitwiseCombination()
    {
        var assessment = Assessment.Create(
            Guid.NewGuid(),
            "Multi-graded quiz",
            AssessmentType.Quiz,
            100,
            70,
            gradingMethods: AssessmentGradingMethod.AutoGraded | AssessmentGradingMethod.InstructorGraded);

        assessment.GradingMethods.Should().Be(AssessmentGradingMethod.AutoGraded | AssessmentGradingMethod.InstructorGraded);
        ((int)assessment.GradingMethods).Should().Be(12);
    }

    [Fact]
    public void Create_WithoutGradingMethods_ShouldDefaultToInstructorGraded()
    {
        var assessment = Assessment.Create(Guid.NewGuid(), "Quiz", AssessmentType.Quiz, 100, 70);

        assessment.GradingMethods.Should().Be(AssessmentGradingMethod.InstructorGraded);
    }

    [Fact]
    public void Create_WithNoneGradingMethods_ShouldBeAccepted()
    {
        var assessment = Assessment.Create(
            Guid.NewGuid(),
            "Survey",
            AssessmentType.Quiz,
            100,
            70,
            gradingMethods: AssessmentGradingMethod.None);

        assessment.GradingMethods.Should().Be(AssessmentGradingMethod.None);
    }

    [Fact]
    public void Update_WithGradingMethods_ShouldPersistNewFlags()
    {
        var assessment = Assessment.Create(Guid.NewGuid(), "Quiz", AssessmentType.Quiz, 100, 70);

        assessment.Update(
            null, null, null, null, null, null, null, null, null,
            gradingMethods: AssessmentGradingMethod.PeerReview | AssessmentGradingMethod.AIGraded);

        assessment.GradingMethods.Should().Be(AssessmentGradingMethod.PeerReview | AssessmentGradingMethod.AIGraded);
    }

    [Fact]
    public void Update_WithoutGradingMethods_ShouldLeaveExistingFlagsUnchanged()
    {
        var assessment = Assessment.Create(
            Guid.NewGuid(),
            "Quiz",
            AssessmentType.Quiz,
            100,
            70,
            gradingMethods: AssessmentGradingMethod.PeerReview);

        assessment.Update(null, null, null, null, null, null, null, null, null);

        assessment.GradingMethods.Should().Be(AssessmentGradingMethod.PeerReview);
    }

    [Fact]
    public void SetDefinition_ShouldPersistStructuredPayloadAndSchemaVersion()
    {
        var assessment = Assessment.Create(Guid.NewGuid(), "Quiz", AssessmentType.Quiz, 100, 70);
        using var definition = JsonDocument.Parse("{\"blocks\":{\"question-1\":{\"kind\":\"multiple-choice\"}},\"order\":[\"question-1\"]}");

        assessment.SetDefinition(definition.RootElement, 2);

        assessment.DefinitionSchemaVersion.Should().Be(2);
        assessment.DefinitionPayload.Should().Be(definition.RootElement.GetRawText());
    }

    [Fact]
    public void SetDefinition_ShouldRejectUndefinedPayloadOrInvalidSchemaVersion()
    {
        var assessment = Assessment.Create(Guid.NewGuid(), "Quiz", AssessmentType.Quiz, 100, 70);
        using var definition = JsonDocument.Parse("{}");

        var undefinedAction = () => assessment.SetDefinition(default, 1);
        var versionAction = () => assessment.SetDefinition(definition.RootElement, 0);

        undefinedAction.Should().Throw<ArgumentException>();
        versionAction.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(-1, 0)]
    [InlineData(100, -1)]
    [InlineData(100, 101)]
    public void Create_WithInvalidScoreRange_Throws(int maxScore, int passingScore)
    {
        var action = () => Assessment.Create(Guid.NewGuid(), "Quiz", AssessmentType.Quiz, maxScore, passingScore);

        action.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Assessment_ShouldExposeWeightedGroupAssignment()
    {
        typeof(Assessment).GetProperty("AssessmentGroupId").Should().NotBeNull();
    }

    [Fact]
    public void AssessmentGroup_ShouldExposeCourseWeightAndOrder()
    {
        var type = typeof(Assessment).Assembly.GetType("GameGuild.Learning.Assessments.AssessmentGroup");

        type.Should().NotBeNull();
        type!.GetProperty("CourseId").Should().NotBeNull();
        type.GetProperty("WeightPercent").Should().NotBeNull();
        type.GetProperty("Order").Should().NotBeNull();
    }

    [Fact]
    public void AssessmentType_ShouldKeepExplicitPersistedValues()
    {
        ((int)AssessmentType.Quiz).Should().Be(0);
        ((int)AssessmentType.Assignment).Should().Be(2);
        ((int)AssessmentType.Project).Should().Be(3);
        ((int)AssessmentType.PeerReview).Should().Be(4);
        ((int)AssessmentType.SelfAssessment).Should().Be(5);
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
    public void Update_WhenNewMaximumWouldBeBelowPassingScore_Throws()
    {
        var assessment = Assessment.Create(Guid.NewGuid(), "Quiz", AssessmentType.Quiz, 100, 60);

        var action = () => assessment.Update(null, null, 50, null, null, null, null, null, null);

        action.Should().Throw<ArgumentOutOfRangeException>();
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
/// Service-level tests for AssessmentService.RestoreAssessmentAsync.
/// </summary>
public class AssessmentServiceRestoreTests
{
    [Fact]
    public async Task RestoreAssessmentAsync_OnSoftDeletedAssessment_MakesItFetchable()
    {
        await using var db = CreateContext();
        var assessment = Assessment.Create(Guid.NewGuid(), "Quiz", AssessmentType.Quiz, 100, 60);
        assessment.SetMaxAttempts(1);
        assessment.Version = 1;
        assessment.SoftDelete();
        db.Set<Assessment>().Add(assessment);
        await db.SaveChangesAsync();
        var service = new AssessmentService(db, Mock.Of<IProgramContentService>(), NullLogger<AssessmentService>.Instance);

        var before = await service.GetAssessmentByIdAsync(assessment.Id);
        before.Should().BeNull();

        var result = await service.RestoreAssessmentAsync(assessment.Id);

        result.IsSuccess.Should().BeTrue();
        var after = await service.GetAssessmentByIdAsync(assessment.Id);
        after.Should().NotBeNull();
        after!.DeletedAt.Should().BeNull();
        after.Title.Should().Be("Quiz");
    }

    [Fact]
    public async Task RestoreAssessmentAsync_OnUnknownId_ReturnsNotFound()
    {
        await using var db = CreateContext();
        var service = new AssessmentService(db, Mock.Of<IProgramContentService>(), NullLogger<AssessmentService>.Instance);

        var result = await service.RestoreAssessmentAsync(Guid.NewGuid());

        result.IsSuccess.Should().BeFalse();
        result.Error.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task RestoreAssessmentAsync_OnActiveAssessment_IsIdempotent()
    {
        await using var db = CreateContext();
        var assessment = Assessment.Create(Guid.NewGuid(), "Quiz", AssessmentType.Quiz, 100, 60);
        db.Set<Assessment>().Add(assessment);
        await db.SaveChangesAsync();
        var service = new AssessmentService(db, Mock.Of<IProgramContentService>(), NullLogger<AssessmentService>.Instance);

        var result = await service.RestoreAssessmentAsync(assessment.Id);

        result.IsSuccess.Should().BeTrue();
        var fetched = await service.GetAssessmentByIdAsync(assessment.Id);
        fetched.Should().NotBeNull();
        fetched!.DeletedAt.Should().BeNull();
    }

    private static TestAssessmentDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<TestAssessmentDbContext>()
            .UseInMemoryDatabase($"AssessmentRestore_{Guid.NewGuid()}")
            .Options;
        return new TestAssessmentDbContext(options);
    }

    private sealed class TestAssessmentDbContext(DbContextOptions<TestAssessmentDbContext> options)
        : DbContext(options), IApplicationDbContext
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            new AssessmentsModelConfiguration().Configure(modelBuilder);
        }

        public Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException("Transactions are not required for assessment restore tests.");
        }
    }
}

/// <summary>
/// Unit tests for AssessmentSubmission entity domain logic.
/// </summary>
public class AssessmentSubmissionEntityTests
{
    [Fact]
    public void Grade_RequiresAssessmentMaximumScore()
    {
        var gradeMethods = typeof(AssessmentSubmission)
            .GetMethods()
            .Where(method => method.Name == nameof(AssessmentSubmission.Grade))
            .ToArray();

        gradeMethods.Should().ContainSingle()
            .Which.GetParameters().Select(parameter => parameter.ParameterType)
            .Should().Equal(typeof(int), typeof(int), typeof(int), typeof(Guid?), typeof(string));
    }

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
        submission.Grade(85, 70, 100, graderId, "Good work!");

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
        submission.Grade(50, 70, 100);

        submission.Passed.Should().BeFalse();
    }

    [Fact]
    public void Grade_AtExactPassingScore_ShouldPass()
    {
        var submission = AssessmentSubmission.Start(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 1);
        submission.Submit();
        submission.Grade(70, 70, 100);

        submission.Passed.Should().BeTrue();
    }

    [Fact]
    public void Grade_WithScoreOutsideAssessmentMaximum_Throws()
    {
        var submission = AssessmentSubmission.Start(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 1);
        submission.Submit();
        var gradeWithMaximum = typeof(AssessmentSubmission).GetMethod(
            nameof(AssessmentSubmission.Grade),
            [typeof(int), typeof(int), typeof(int), typeof(Guid?), typeof(string)]);

        gradeWithMaximum.Should().NotBeNull();
        var action = () => gradeWithMaximum!.Invoke(submission, [101, 60, 100, null, null]);

        action.Should().Throw<TargetInvocationException>()
            .WithInnerException<ArgumentOutOfRangeException>();
    }

    [Fact]
    public async Task StartSubmissionAsync_UsesHighestHistoricalAttemptNumber()
    {
        await using var db = CreateContext();
        var assessment = Assessment.Create(Guid.NewGuid(), "Quiz", AssessmentType.Quiz, 100, 60);
        assessment.SetMaxAttempts(4);
        var enrollmentId = Guid.NewGuid();
        var historicalSubmission = AssessmentSubmission.Start(assessment.Id, enrollmentId, Guid.NewGuid(), 3);
        historicalSubmission.Version = 1;
        historicalSubmission.SoftDelete();
        db.AddRange(assessment, historicalSubmission);
        await db.SaveChangesAsync();
        var service = new AssessmentService(db, Mock.Of<IProgramContentService>(), NullLogger<AssessmentService>.Instance);

        var result = await service.StartSubmissionAsync(assessment.Id, enrollmentId, Guid.NewGuid());

        result.IsSuccess.Should().BeTrue();
        result.Value.AttemptNumber.Should().Be(4);
    }

    private static TestAssessmentDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<TestAssessmentDbContext>()
            .UseInMemoryDatabase($"AssessmentAttempt_{Guid.NewGuid()}")
            .Options;
        return new TestAssessmentDbContext(options);
    }

    private sealed class TestAssessmentDbContext(DbContextOptions<TestAssessmentDbContext> options)
        : DbContext(options), IApplicationDbContext
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            new AssessmentsModelConfiguration().Configure(modelBuilder);
        }

        public Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException("Transactions are not required for assessment entity tests.");
        }
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
        dto.Type.Should().Be(AssessmentType.Quiz);
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
            DateTime.UtcNow, DateTime.UtcNow.AddDays(7), null, null, null, null, true);

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

    [Fact]
    public void AssessmentDto_ShouldExposeGroupMetadata()
    {
        typeof(AssessmentDto).GetProperty("AssessmentGroupId").Should().NotBeNull();
        typeof(AssessmentDto).GetProperty("AssessmentGroupName").Should().NotBeNull();
        typeof(AssessmentDto).GetProperty("AssessmentGroupWeightPercent").Should().NotBeNull();
        typeof(AssessmentDto).GetProperty("AssessmentGroupOrder").Should().NotBeNull();
    }

    [Fact]
    public void AssessmentAnalyticsDtos_ShouldExposeScoreDistributionContract()
    {
        typeof(CourseAssessmentAnalyticsDto).GetProperty("CourseId").Should().NotBeNull();
        typeof(CourseAssessmentAnalyticsDto).GetProperty("Distribution").Should().NotBeNull();
        typeof(CourseAssessmentAnalyticsDto).GetProperty("Groups").Should().NotBeNull();
        typeof(AssessmentGroupAnalyticsDto).GetProperty("AveragePercent").Should().NotBeNull();
        typeof(AssessmentScoreBucketDto).GetProperty("Count").Should().NotBeNull();
    }
}

public sealed class AssessmentServiceAnalyticsTests
{
    [Fact]
    public async Task GetCourseAssessmentAnalyticsAsync_ReturnsOverallAndGroupScoreDistribution()
    {
        await using var db = CreateContext();
        var courseId = Guid.NewGuid();
        var quizGroup = AssessmentGroup.Create(courseId, "Quizzes", 20, 1);
        var projectGroup = AssessmentGroup.Create(courseId, "Final Project", 30, 2);
        var quiz = Assessment.Create(courseId, "Intro quiz", AssessmentType.Quiz, 10, 6, assessmentGroupId: quizGroup.Id);
        var project = Assessment.Create(courseId, "Final build", AssessmentType.Project, 100, 70, assessmentGroupId: projectGroup.Id);
        var attendance = Assessment.Create(courseId, "Attendance", AssessmentType.Assignment, 10, 6);
        var ignoredOtherCourse = Assessment.Create(Guid.NewGuid(), "Other", AssessmentType.Quiz, 10, 6);

        var quizSubmission = AssessmentSubmission.Start(quiz.Id, Guid.NewGuid(), Guid.NewGuid(), 1);
        quizSubmission.Submit();
        quizSubmission.Grade(8, quiz.PassingScore, quiz.MaxScore);
        var projectSubmission = AssessmentSubmission.Start(project.Id, Guid.NewGuid(), Guid.NewGuid(), 1);
        projectSubmission.Submit();
        projectSubmission.Grade(50, project.PassingScore, project.MaxScore);
        var ignoredSubmission = AssessmentSubmission.Start(ignoredOtherCourse.Id, Guid.NewGuid(), Guid.NewGuid(), 1);
        ignoredSubmission.Submit();
        ignoredSubmission.Grade(10, ignoredOtherCourse.PassingScore, ignoredOtherCourse.MaxScore);

        db.Set<AssessmentGroup>().AddRange(quizGroup, projectGroup);
        db.Set<Assessment>().AddRange(quiz, project, attendance, ignoredOtherCourse);
        db.Set<AssessmentSubmission>().AddRange(quizSubmission, projectSubmission, ignoredSubmission);
        await db.SaveChangesAsync();

        var service = new AssessmentService(db, Mock.Of<IProgramContentService>(), NullLogger<AssessmentService>.Instance);

        var analytics = await service.GetCourseAssessmentAnalyticsAsync(courseId);

        analytics.CourseId.Should().Be(courseId);
        analytics.AssessmentCount.Should().Be(3);
        analytics.GradedCount.Should().Be(2);
        analytics.UngradedCount.Should().Be(1);
        analytics.AveragePercent.Should().Be(65);
        analytics.PassRate.Should().Be(50);
        analytics.Distribution.Single(bucket => bucket.Label == "80-89").Count.Should().Be(1);
        analytics.Distribution.Single(bucket => bucket.Label == "0-59").Count.Should().Be(1);
        analytics.Groups.Single(group => group.GroupName == "Quizzes").AveragePercent.Should().Be(80);
        analytics.Groups.Single(group => group.GroupName == "Final Project").PassRate.Should().Be(0);
        analytics.Groups.Single(group => group.GroupName == "Ungrouped").AssessmentCount.Should().Be(1);
    }

    private static TestAssessmentDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<TestAssessmentDbContext>()
            .UseInMemoryDatabase($"AssessmentAnalytics_{Guid.NewGuid()}")
            .Options;
        return new TestAssessmentDbContext(options);
    }

    private sealed class TestAssessmentDbContext(DbContextOptions<TestAssessmentDbContext> options)
        : DbContext(options), IApplicationDbContext
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            new AssessmentsModelConfiguration().Configure(modelBuilder);
        }

        public Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException("Transactions are not required for assessment analytics tests.");
        }
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
        submission.Grade(88, 70, 100, graderId, "Excellent");

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
    public void CreateAssessmentGroupRequest_ShouldExposeWeightedGroupFields()
    {
        var type = typeof(CreateAssessmentRequest).Assembly.GetType("GameGuild.Learning.Assessments.CreateAssessmentGroupRequest");

        type.Should().NotBeNull();
        type!.GetProperty("CourseId").Should().NotBeNull();
        type.GetProperty("Name").Should().NotBeNull();
        type.GetProperty("WeightPercent").Should().NotBeNull();
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
