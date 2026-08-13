using FluentAssertions;
using GameGuild.API.Database;
using GameGuild.Learning.Assessments;
using GameGuild.Learning.Courses;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Npgsql;
using Testcontainers.PostgreSql;
using Xunit;

namespace GameGuild.API.UnitTests.Database;

[Collection(PostgreSqlTestCollection.Name)]
public sealed class AssessmentLifecyclePostgreSqlRaceTests
{
    [Fact]
    public async Task DeleteAssessment_WithGradeGroup_SoftDeletesOnlyAssessment()
    {
        var container = new PostgreSqlBuilder()
            .WithImage("postgres:16-alpine")
            .WithDatabase("assessment_group_delete")
            .WithUsername("test")
            .WithPassword("test")
            .WithCleanUp(true)
            .Build();
        await container.StartAsync();
        try
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseNpgsql(container.GetConnectionString(), npgsql => npgsql.EnableRetryOnFailure())
                .Options;
            Guid assessmentId;
            Guid groupId;
            int originalGroupVersion;
            await using (var setup = new ApplicationDbContext(options))
            {
                await setup.Database.EnsureCreatedAsync();
                var courseId = Guid.NewGuid();
                var group = AssessmentGroup.Create(courseId, "Final project", 40, 2);
                var assessment = Assessment.Create(
                    courseId,
                    "Vertical slice review",
                    AssessmentType.Project,
                    120,
                    assessmentGroupId: group.Id);
                assessment.SetDeliveryContract(
                    SubmissionModality.Project | SubmissionModality.StructuredAnswer,
                    AssessmentPresentationMode.Continuous);
                setup.AddRange(group, assessment);
                await setup.SaveChangesAsync();
                assessmentId = assessment.Id;
                groupId = group.Id;
                originalGroupVersion = group.Version;
            }

            var logger = new CapturingLogger<AssessmentService>();
            Result result;
            await using (var context = new ApplicationDbContext(options))
            {
                var service = new AssessmentService(context, Mock.Of<IProgramContentService>(), logger);
                (await service.GetAssessmentByIdAsync(assessmentId)).Should().NotBeNull();
                result = await service.DeleteAssessmentAsync(assessmentId);
            }

            result.IsSuccess.Should().BeTrue(logger.Exception?.ToString());
            await using var verify = new ApplicationDbContext(options);
            var assessmentAfterDelete = await verify.Set<Assessment>().SingleAsync(assessment => assessment.Id == assessmentId);
            var groupAfterDelete = await verify.Set<AssessmentGroup>().SingleAsync(group => group.Id == groupId);
            assessmentAfterDelete.DeletedAt.Should().NotBeNull();
            groupAfterDelete.DeletedAt.Should().BeNull();
            groupAfterDelete.Version.Should().Be(originalGroupVersion);
        }
        finally
        {
            await container.DisposeAsync();
        }
    }

    [Fact]
    public async Task ConcurrentDeleteAndCueLink_CannotLeaveAnActiveCueOnDeletedAssessment()
    {
        var container = new PostgreSqlBuilder()
            .WithImage("postgres:16-alpine")
            .WithDatabase("assessment_lifecycle_race")
            .WithUsername("test")
            .WithPassword("test")
            .WithCleanUp(true)
            .Build();
        await container.StartAsync();
        try
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseNpgsql(container.GetConnectionString())
                .Options;
            var courseId = Guid.NewGuid();
            var contentId = Guid.NewGuid();
            Guid assessmentId;
            await using (var setup = new ApplicationDbContext(options))
            {
                await setup.Database.EnsureCreatedAsync();
                var assessment = Assessment.Create(courseId, "Race", AssessmentType.Quiz, 100);
                assessmentId = assessment.Id;
                setup.Add(assessment);
                await setup.SaveChangesAsync();
            }

            await using var gateContext = new ApplicationDbContext(options);
            await using var contentGate = await ProgramContentLifecycleDatabaseLock.AcquireAsync(gateContext, [contentId]);
            contentGate.Should().NotBeNull();
            await using var observer = new NpgsqlConnection(container.GetConnectionString());
            await observer.OpenAsync();

            var linkTask = LinkAsync(options, courseId, assessmentId, contentId);
            await WaitForWaitingAdvisoryLocksAsync(observer, 1);
            var deleteTask = DeleteAsync(options, assessmentId);
            await WaitForWaitingAdvisoryLocksAsync(observer, 2);

            await ProgramContentLifecycleDatabaseLock.CommitAsync(contentGate);
            var deleteResult = await deleteTask;
            var linkResult = await linkTask;

            deleteResult.IsSuccess.Should().BeTrue();
            linkResult.IsSuccess.Should().BeTrue();
            await using var verify = new ApplicationDbContext(options);
            (await verify.Set<Assessment>().SingleAsync(assessment => assessment.Id == assessmentId)).DeletedAt.Should().NotBeNull();
            (await verify.Set<InteractiveVideoAssessmentCue>()
                .AnyAsync(cue => cue.AssessmentId == assessmentId && cue.DeletedAt == null))
                .Should().BeFalse();
        }
        finally
        {
            await container.DisposeAsync();
        }
    }

    private static async Task<Result> DeleteAsync(DbContextOptions<ApplicationDbContext> options, Guid assessmentId)
    {
        await using var context = new ApplicationDbContext(options);
        return await new AssessmentService(context, Mock.Of<IProgramContentService>(), NullLogger<AssessmentService>.Instance)
            .DeleteAssessmentAsync(assessmentId);
    }

    private static async Task<Result<InteractiveVideoAssessmentCue>> LinkAsync(
        DbContextOptions<ApplicationDbContext> options,
        Guid courseId,
        Guid assessmentId,
        Guid contentId)
    {
        await using var context = new ApplicationDbContext(options);
        var content = new ProgramContent
        {
            Id = contentId,
            ProgramId = courseId,
            Title = "Video",
            Type = ProgramContentType.Lesson,
            LessonFormat = LessonContentFormat.Video
        };
        var contents = new Mock<IProgramContentService>();
        contents.Setup(service => service.GetContentByIdAsync(contentId)).ReturnsAsync(content);
        return await new AssessmentService(context, contents.Object, NullLogger<AssessmentService>.Instance)
            .LinkInteractiveVideoCueAsync(assessmentId, new LinkInteractiveVideoCueRequest(contentId, "race"));
    }

    private static async Task WaitForWaitingAdvisoryLocksAsync(NpgsqlConnection connection, int minimumCount)
    {
        var deadline = DateTime.UtcNow.AddSeconds(10);
        while (DateTime.UtcNow < deadline)
        {
            await using var command = new NpgsqlCommand(
                "SELECT COUNT(*) FROM pg_locks WHERE locktype = 'advisory' AND NOT granted AND database = (SELECT oid FROM pg_database WHERE datname = current_database())",
                connection);
            if (Convert.ToInt32(await command.ExecuteScalarAsync()) >= minimumCount) return;
            await Task.Delay(25);
        }

        throw new TimeoutException($"Timed out waiting for {minimumCount} advisory lock waiters.");
    }
    private sealed class CapturingLogger<T> : ILogger<T>
    {
        public Exception? Exception { get; private set; }

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            if (logLevel >= LogLevel.Error && exception is not null)
            {
                Exception = exception;
            }
        }
    }
}
