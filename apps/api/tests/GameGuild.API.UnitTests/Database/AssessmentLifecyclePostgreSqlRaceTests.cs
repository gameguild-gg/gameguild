using System.Security.Cryptography;
using FluentAssertions;
using GameGuild.API.Database;
using GameGuild.Learning.Assessments;
using GameGuild.Learning.Courses;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Npgsql;
using Testcontainers.PostgreSql;
using Xunit;

namespace GameGuild.API.UnitTests.Database;

public sealed class AssessmentLifecyclePostgreSqlRaceTests
{
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
                var assessment = Assessment.Create(courseId, "Race", AssessmentType.Quiz, 100, 60);
                assessmentId = assessment.Id;
                setup.Add(assessment);
                await setup.SaveChangesAsync();
            }

            await using var gateConnection = new NpgsqlConnection(container.GetConnectionString());
            await gateConnection.OpenAsync();
            await using var gateTransaction = await gateConnection.BeginTransactionAsync();
            await using (var lockCommand = new NpgsqlCommand("SELECT pg_advisory_xact_lock(@key)", gateConnection, gateTransaction))
            {
                lockCommand.Parameters.AddWithValue("key", AssessmentLifecycleLockKey(assessmentId));
                await lockCommand.ExecuteNonQueryAsync();
            }
            var deleteTask = DeleteAsync(options, assessmentId);
            var linkTask = LinkAsync(options, courseId, assessmentId, contentId);
            await Task.Delay(300);
            deleteTask.IsCompleted.Should().BeFalse("assessment deletion must acquire the assessment lifecycle lock");
            linkTask.IsCompleted.Should().BeFalse("cue linking must acquire the same assessment lifecycle lock");
            await gateTransaction.CommitAsync();
            var deleteResult = await deleteTask;
            var linkResult = await linkTask;

            deleteResult.IsSuccess.Should().BeTrue();
            linkResult.IsSuccess.Should().BeFalse();
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

    private static long AssessmentLifecycleLockKey(Guid assessmentId)
    {
        Span<byte> source = stackalloc byte[36];
        "assessment-lifecycle"u8.CopyTo(source);
        assessmentId.TryWriteBytes(source[20..]);
        Span<byte> hash = stackalloc byte[32];
        SHA256.HashData(source, hash);
        return BitConverter.ToInt64(hash);
    }
}
