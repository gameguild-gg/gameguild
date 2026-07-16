using FluentAssertions;
using GameGuild.Learning.Courses;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace GameGuild.Learning.Assessments.Tests;

public sealed class InteractiveVideoCueServiceTests
{
    private static readonly SemaphoreSlim ClockGate = new(1, 1);

    [Fact]
    public async Task SubmitAsync_AtDueBoundary_UsesOneCapturedTimestampForEligibilityAndPersistence()
    {
        await ClockGate.WaitAsync();
        try
        {
            await using var db = CreateContext();
            var dueAt = DateTime.UtcNow.AddHours(1);
            var assessment = Assessment.Create(Guid.NewGuid(), "Boundary", AssessmentType.Assignment, 10, 6);
            assessment.SetDeliverySchedule(null, dueAt, dueAt, false, null);
            var submission = AssessmentSubmission.Start(assessment.Id, Guid.NewGuid(), Guid.NewGuid(), 1);
            db.AddRange(assessment, submission);
            await db.SaveChangesAsync();
            SystemClock.SetProvider(new SequenceTimeProvider(dueAt, dueAt.AddTicks(1)));
            var service = new AssessmentService(db, Mock.Of<IProgramContentService>(), NullLogger<AssessmentService>.Instance);

            var result = await service.SubmitAsync(submission.Id);

            result.IsSuccess.Should().BeTrue();
            result.Value.SubmittedAt.Should().Be(dueAt);
            result.Value.Status.Should().Be(SubmissionStatus.Submitted);
        }
        finally
        {
            SystemClock.Reset();
            ClockGate.Release();
        }
    }
    [Fact]
    public async Task GetUserSubmissionsAsync_FiltersByEnrollmentAndActorUser()
    {
        await using var db = CreateContext();
        var enrollmentId = Guid.NewGuid();
        var actorUserId = Guid.NewGuid();
        db.Set<AssessmentSubmission>().AddRange(
            AssessmentSubmission.Start(Guid.NewGuid(), enrollmentId, actorUserId, 1),
            AssessmentSubmission.Start(Guid.NewGuid(), enrollmentId, Guid.NewGuid(), 1),
            AssessmentSubmission.Start(Guid.NewGuid(), Guid.NewGuid(), actorUserId, 1));
        await db.SaveChangesAsync();
        var service = new AssessmentService(
            db,
            Mock.Of<IProgramContentService>(),
            NullLogger<AssessmentService>.Instance);

        var submissions = await service.GetUserSubmissionsAsync(enrollmentId, actorUserId);

        submissions.Should().ContainSingle()
            .Which.UserId.Should().Be(actorUserId);
    }

    [Fact]
    public async Task LinkInteractiveVideoCueAsync_WhenContentDoesNotResolve_ShouldReturnNotFound()
    {
        await using var db = CreateContext();
        var assessment = Assessment.Create(Guid.NewGuid(), "Video checkpoint", AssessmentType.Quiz, 10, 6);
        db.Set<Assessment>().Add(assessment);
        await db.SaveChangesAsync();
        var contents = new Mock<IProgramContentService>();
        contents.Setup(service => service.GetContentByIdAsync(It.IsAny<Guid>())).ReturnsAsync((ProgramContent?)null);
        var service = new AssessmentService(db, contents.Object, NullLogger<AssessmentService>.Instance);

        var result = await service.LinkInteractiveVideoCueAsync(
            assessment.Id,
            new LinkInteractiveVideoCueRequest(Guid.NewGuid(), "chapter-1"));

        result.IsSuccess.Should().BeFalse();
        result.Error.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task LinkInteractiveVideoCueAsync_WhenDeletedContentDoesNotResolve_ShouldReturnNotFound()
    {
        await using var db = CreateContext();
        var assessment = Assessment.Create(Guid.NewGuid(), "Video checkpoint", AssessmentType.Quiz, 10, 6);
        db.Set<Assessment>().Add(assessment);
        await db.SaveChangesAsync();
        var contents = new Mock<IProgramContentService>();
        contents.Setup(service => service.GetContentByIdAsync(It.IsAny<Guid>())).ReturnsAsync((ProgramContent?)null);
        var service = new AssessmentService(db, contents.Object, NullLogger<AssessmentService>.Instance);

        var result = await service.LinkInteractiveVideoCueAsync(
            assessment.Id,
            new LinkInteractiveVideoCueRequest(Guid.NewGuid(), "deleted-cue"));

        result.IsSuccess.Should().BeFalse();
        result.Error.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task LinkInteractiveVideoCueAsync_WhenContentIsInAnotherCourse_ShouldReturnValidationError()
    {
        await using var db = CreateContext();
        var assessment = Assessment.Create(Guid.NewGuid(), "Video checkpoint", AssessmentType.Quiz, 10, 6);
        db.Set<Assessment>().Add(assessment);
        await db.SaveChangesAsync();
        var content = CreateVideoLesson(Guid.NewGuid());
        var contents = new Mock<IProgramContentService>();
        contents.Setup(service => service.GetContentByIdAsync(content.Id)).ReturnsAsync(content);
        var service = new AssessmentService(db, contents.Object, NullLogger<AssessmentService>.Instance);

        var result = await service.LinkInteractiveVideoCueAsync(
            assessment.Id,
            new LinkInteractiveVideoCueRequest(content.Id, "other-course"));

        result.IsSuccess.Should().BeFalse();
        result.Error.Type.Should().Be(ErrorType.Validation);
    }

    [Fact]
    public async Task LinkInteractiveVideoCueAsync_WhenContentIsNotVideoLesson_ShouldReturnValidationError()
    {
        await using var db = CreateContext();
        var courseId = Guid.NewGuid();
        var assessment = Assessment.Create(courseId, "Video checkpoint", AssessmentType.Quiz, 10, 6);
        db.Set<Assessment>().Add(assessment);
        await db.SaveChangesAsync();
        var content = CreateVideoLesson(courseId);
        content.LessonFormat = LessonContentFormat.Markdown;
        var contents = new Mock<IProgramContentService>();
        contents.Setup(service => service.GetContentByIdAsync(content.Id)).ReturnsAsync(content);
        var service = new AssessmentService(db, contents.Object, NullLogger<AssessmentService>.Instance);

        var result = await service.LinkInteractiveVideoCueAsync(
            assessment.Id,
            new LinkInteractiveVideoCueRequest(content.Id, "not-video"));

        result.IsSuccess.Should().BeFalse();
        result.Error.Type.Should().Be(ErrorType.Validation);
    }

    [Fact]
    public async Task LinkInteractiveVideoCueAsync_WithVideoLessonInAssessmentCourse_ShouldPersistStableCue()
    {
        await using var db = CreateContext();
        var courseId = Guid.NewGuid();
        var assessment = Assessment.Create(courseId, "Video checkpoint", AssessmentType.Quiz, 10, 6);
        db.Set<Assessment>().Add(assessment);
        await db.SaveChangesAsync();
        var content = CreateVideoLesson(courseId);
        var contents = new Mock<IProgramContentService>();
        contents.Setup(service => service.GetContentByIdAsync(content.Id)).ReturnsAsync(content);
        var service = new AssessmentService(db, contents.Object, NullLogger<AssessmentService>.Instance);

        var result = await service.LinkInteractiveVideoCueAsync(
            assessment.Id,
            new LinkInteractiveVideoCueRequest(content.Id, " chapter-1 "));

        result.IsSuccess.Should().BeTrue();
        result.Value.CueId.Should().Be("chapter-1");
        (await db.Set<InteractiveVideoAssessmentCue>().SingleAsync()).CueId.Should().Be("chapter-1");
    }

    [Fact]
    public async Task GetInteractiveVideoCuesAsync_WhenLinkedContentNoLongerResolves_ShouldFilterStaleCue()
    {
        await using var db = CreateContext();
        var assessment = Assessment.Create(Guid.NewGuid(), "Video checkpoint", AssessmentType.Quiz, 10, 6);
        var cue = assessment.AddInteractiveVideoCue(Guid.NewGuid(), "chapter-1");
        db.AddRange(assessment, cue);
        await db.SaveChangesAsync();
        var contents = new Mock<IProgramContentService>();
        contents.Setup(service => service.GetContentByIdAsync(cue.ContentId)).ReturnsAsync((ProgramContent?)null);
        var service = new AssessmentService(db, contents.Object, NullLogger<AssessmentService>.Instance);

        var cues = await service.GetInteractiveVideoCuesAsync(assessment.Id);

        cues.Should().BeEmpty();
    }

    [Fact]
    public async Task UnlinkInteractiveVideoCueAsync_HardDeletesAndAllowsTheStableCueToBeRelinked()
    {
        await using var db = CreateContext();
        var courseId = Guid.NewGuid();
        var assessment = Assessment.Create(courseId, "Video checkpoint", AssessmentType.Quiz, 10, 6);
        db.Set<Assessment>().Add(assessment);
        await db.SaveChangesAsync();
        var content = CreateVideoLesson(courseId);
        var contents = new Mock<IProgramContentService>();
        contents.Setup(service => service.GetContentByIdAsync(content.Id)).ReturnsAsync(content);
        var service = new AssessmentService(db, contents.Object, NullLogger<AssessmentService>.Instance);

        var linked = await service.LinkInteractiveVideoCueAsync(
            assessment.Id,
            new LinkInteractiveVideoCueRequest(content.Id, "checkpoint"));
        var unlinked = await service.UnlinkInteractiveVideoCueAsync(assessment.Id, linked.Value.Id);
        var relinked = await service.LinkInteractiveVideoCueAsync(
            assessment.Id,
            new LinkInteractiveVideoCueRequest(content.Id, "checkpoint"));

        linked.IsSuccess.Should().BeTrue();
        unlinked.IsSuccess.Should().BeTrue();
        relinked.IsSuccess.Should().BeTrue();
        (await db.Set<InteractiveVideoAssessmentCue>().CountAsync()).Should().Be(1);
    }

    private static ProgramContent CreateVideoLesson(Guid courseId) => new()
    {
        Id = Guid.NewGuid(),
        ProgramId = courseId,
        Title = "Video lesson",
        Type = ProgramContentType.Lesson,
        LessonFormat = LessonContentFormat.Video,
    };

    private static TestAssessmentDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<TestAssessmentDbContext>()
            .UseInMemoryDatabase($"AssessmentCue_{Guid.NewGuid()}")
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
            throw new NotSupportedException("Transactions are not required for cue tests.");
        }
    }

    private sealed class SequenceTimeProvider(params DateTime[] timestamps) : TimeProvider
    {
        private int _index;

        public override DateTimeOffset GetUtcNow()
        {
            var timestamp = timestamps[Math.Min(_index++, timestamps.Length - 1)];
            return new DateTimeOffset(timestamp, TimeSpan.Zero);
        }
    }
}
