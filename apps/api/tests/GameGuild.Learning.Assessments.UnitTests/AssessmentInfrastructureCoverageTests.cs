using FluentAssertions;
using GameGuild.Learning.Courses;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using System.Collections;
using System.Reflection;
using Xunit;

namespace GameGuild.Learning.Assessments.Tests;

public sealed class AssessmentInfrastructureCoverageTests
{
    [Fact]
    public async Task GradingSync_UpdatesMatchingAssessmentAndIgnoresMissingContent()
    {
        await using var db = CreateContext();
        var contentId = Guid.NewGuid();
        var assessment = Assessment.Create(
            Guid.NewGuid(), "Quiz", AssessmentType.Quiz, Score(100), contentId: contentId);
        db.Add(assessment);
        await db.SaveChangesAsync();
        var sync = new AssessmentGradingSync(db);

        await sync.SyncAsync(Guid.NewGuid(), 25);
        await sync.SyncAsync(contentId, 75);

        assessment.MaxScore.Should().Be(Score(75));
    }

    [Fact]
    public async Task GradingSync_CreatesTheContentOwnedAssessmentForANewCodingAssignment()
    {
        await using var db = CreateContext();
        var courseId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var content = new ProgramContent
        {
            Id = Guid.NewGuid(),
            ProgramId = courseId,
            TenantId = tenantId,
            Title = "Pathfinding challenge",
            Slug = "pathfinding-challenge",
            Type = ProgramContentType.Code,
            IsRequired = true,
        };
        db.Add(content);
        await db.SaveChangesAsync();
        var sync = new AssessmentGradingSync(db);

        await sync.SyncAsync(content.Id, 100);

        var assessment = await db.Set<Assessment>().SingleAsync();
        assessment.CourseId.Should().Be(courseId);
        assessment.ContentId.Should().Be(content.Id);
        assessment.TenantId.Should().Be(tenantId);
        assessment.Title.Should().Be(content.Title);
        assessment.Slug.Should().Be(content.Slug);
        assessment.Type.Should().Be(AssessmentType.Assignment);
        assessment.SubmissionModalities.Should().Be(SubmissionModality.Code);
        assessment.ReviewMethods.Should().Be(
            Grading.Contracts.ReviewMethods.AutomatedReview |
            Grading.Contracts.ReviewMethods.InstructorReview);
        assessment.MaxScore.Should().Be(Score(100));
    }

    [Fact]
    public async Task LifecycleGuard_ReportsOnlyActiveVideoCueReferences()
    {
        await using var db = CreateContext();
        var contentId = Guid.NewGuid();
        var assessment = Assessment.Create(Guid.NewGuid(), "Video quiz", AssessmentType.Quiz, Score(100));
        var cue = assessment.AddInteractiveVideoCue(contentId, "checkpoint");
        db.AddRange(assessment, cue);
        await db.SaveChangesAsync();
        var guard = new AssessmentProgramContentLifecycleGuard(db);

        (await guard.HasBlockingDeleteReference(contentId)).Should().BeTrue();
        (await guard.HasBlockingDeleteReference(Guid.NewGuid())).Should().BeFalse();
        (await guard.HasBlockingIncompatibleUpdateReference(
            contentId, ProgramContentType.Lesson, LessonContentFormat.Video)).Should().BeFalse();
        (await guard.HasBlockingIncompatibleUpdateReference(
            contentId, ProgramContentType.Lesson, LessonContentFormat.Markdown)).Should().BeTrue();
        (await guard.HasBlockingIncompatibleUpdateReference(
            contentId, ProgramContentType.Assignment, LessonContentFormat.Video)).Should().BeTrue();
    }

    [Fact]
    public async Task DatabaseLocks_CoverNonRelationalAndCommitPaths()
    {
        await using var db = CreateContext();
        var transaction = new Mock<IDbContextTransaction>(MockBehavior.Strict);
        transaction.Setup(t => t.CommitAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        (await AssessmentLifecycleDatabaseLock.AcquireAsync(db, Guid.NewGuid())).Should().BeNull();
        (await AssessmentSubmissionDatabaseLock.AcquireAsync(db, Guid.NewGuid(), Guid.NewGuid())).Should().BeNull();
        await AssessmentLifecycleDatabaseLock.CommitAsync(null);
        await AssessmentLifecycleDatabaseLock.CommitAsync(transaction.Object);
        await AssessmentSubmissionDatabaseLock.CommitAsync(null);
        await AssessmentSubmissionDatabaseLock.CommitAsync(transaction.Object);

        transaction.Verify(t => t.CommitAsync(It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public void DatabaseLocks_CreateStableDistinctKeys()
    {
        var assessmentId = Guid.NewGuid();
        var enrollmentId = Guid.NewGuid();
        var lifecycleMethod = typeof(AssessmentLifecycleDatabaseLock)
            .GetMethod("CreateLockKey", BindingFlags.NonPublic | BindingFlags.Static)!;
        var submissionMethod = typeof(AssessmentSubmissionDatabaseLock)
            .GetMethod("CreateLockKey", BindingFlags.NonPublic | BindingFlags.Static)!;

        var lifecycleKey = (long)lifecycleMethod.Invoke(null, [assessmentId])!;
        var sameLifecycleKey = (long)lifecycleMethod.Invoke(null, [assessmentId])!;
        var submissionKey = (long)submissionMethod.Invoke(null, [assessmentId, enrollmentId])!;

        lifecycleKey.Should().Be(sameLifecycleKey);
        submissionKey.Should().NotBe(lifecycleKey);
    }

    [Fact]
    public void UniqueConstraintDetection_WalksTheExceptionChain()
    {
        var method = typeof(AssessmentService)
            .GetMethod("IsUniqueConstraintViolation", BindingFlags.NonPublic | BindingFlags.Static)!;
        var unique = new DbUpdateException("outer", new SqlStateException("23505"));
        var other = new DbUpdateException("outer", new SqlStateException("22000"));

        ((bool)method.Invoke(null, [unique])!).Should().BeTrue();
        ((bool)method.Invoke(null, [other])!).Should().BeFalse();
        ((bool)method.Invoke(null, [new DbUpdateException()])!).Should().BeFalse();
    }

    [Fact]
    public void ScoreFacts_FallBackToLegacyPassingPercentAndHandleEmptyAggregates()
    {
        var assessment = Assessment.Create(Guid.NewGuid(), "Quiz", AssessmentType.Quiz, Score(100));
        var submission = AssessmentSubmission.Start(assessment.Id, Guid.NewGuid(), Guid.NewGuid(), 1);
        SetProperty<ScoreValue?>(submission, nameof(AssessmentSubmission.Score), Score(60));
        SetProperty<bool?>(submission, nameof(AssessmentSubmission.Passed), null);

        var buildFacts = typeof(AssessmentService)
            .GetMethod("BuildScoreFacts", BindingFlags.NonPublic | BindingFlags.Static)!;
        var facts = (IEnumerable)buildFacts.Invoke(null, [new[] { assessment }, new[] { submission }])!;
        facts.Cast<object>().Should().ContainSingle();

        var factList = buildFacts.ReturnType.GetConstructor(Type.EmptyTypes)!.Invoke(null);
        var average = typeof(AssessmentService)
            .GetMethod("AveragePercent", BindingFlags.NonPublic | BindingFlags.Static)!;
        var passRate = typeof(AssessmentService)
            .GetMethod("PassRate", BindingFlags.NonPublic | BindingFlags.Static)!;
        ((PercentValue)average.Invoke(null, [factList])!).Should().Be(PercentValue.Zero);
        ((PercentValue)passRate.Invoke(null, [factList])!).Should().Be(PercentValue.Zero);
    }

    private static void SetProperty<T>(AssessmentSubmission submission, string name, T value) =>
        typeof(AssessmentSubmission).GetProperty(name)!.SetValue(submission, value);

    private static AssessmentCoverageDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AssessmentCoverageDbContext>()
            .UseInMemoryDatabase($"AssessmentCoverage_{Guid.NewGuid()}")
            .Options;
        return new AssessmentCoverageDbContext(options);
    }

    private sealed class AssessmentCoverageDbContext(DbContextOptions<AssessmentCoverageDbContext> options)
        : DbContext(options), IApplicationDbContext
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            new AssessmentsModelConfiguration().Configure(modelBuilder);
            modelBuilder.Entity<ProgramContent>().Ignore(content => content.Program);
            modelBuilder.Entity<ProgramContent>().Ignore(content => content.Parent);
            modelBuilder.Entity<ProgramContent>().Ignore(content => content.Children);
            modelBuilder.Entity<ProgramContent>().Ignore(content => content.ContentInteractions);
        }

        public Task<IDbContextTransaction> BeginTransactionAsync(
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }

    private sealed class SqlStateException(string sqlState) : Exception
    {
        public string SqlState { get; } = sqlState;
    }
}
