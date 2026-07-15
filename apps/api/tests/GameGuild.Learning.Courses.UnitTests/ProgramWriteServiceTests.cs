using FluentAssertions;
using GameGuild.Learning.Courses;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Xunit;

namespace GameGuild.Learning.Courses.UnitTests;

public sealed class ProgramWriteServiceTests
{
    [Fact]
    public async Task UpdateProgramAsync_ShouldClearNullableEnrollmentControls()
    {
        await using var context = CreateContext();
        var program = new Program
        {
            Id = Guid.NewGuid(),
            Title = "Enrollment controls",
            Slug = "enrollment-controls",
            MaxEnrollments = 25,
            EnrollmentDeadline = DateTime.UtcNow.AddDays(10),
        };
        context.Set<Program>().Add(program);
        await context.SaveChangesAsync();

        var service = new ProgramWriteService(context);
        var updated = await service.UpdateProgramAsync(program.Id, new UpdateProgramDto
        {
            ClearMaxEnrollments = true,
            ClearEnrollmentDeadline = true,
        });

        updated.Should().NotBeNull();
        updated!.MaxEnrollments.Should().BeNull();
        updated.EnrollmentDeadline.Should().BeNull();
    }

    [Fact]
    public async Task UpdateProgramAsync_ShouldSetNullableEnrollmentControls()
    {
        await using var context = CreateContext();
        var program = new Program
        {
            Id = Guid.NewGuid(),
            Title = "Enrollment controls",
            Slug = "enrollment-controls",
        };
        context.Set<Program>().Add(program);
        await context.SaveChangesAsync();
        var deadline = DateTime.UtcNow.AddDays(10);

        var service = new ProgramWriteService(context);
        var updated = await service.UpdateProgramAsync(program.Id, new UpdateProgramDto
        {
            MaxEnrollments = 40,
            EnrollmentDeadline = deadline,
        });

        updated.Should().NotBeNull();
        updated!.MaxEnrollments.Should().Be(40);
        updated.EnrollmentDeadline.Should().Be(deadline);
    }

    [Fact]
    public async Task UpdateContentAsync_WhenBodyChanges_ShouldPreserveExplicitLessonFormat()
    {
        await using var context = CreateContext();
        var program = CreateProgram();
        var content = new ProgramContent
        {
            Id = Guid.NewGuid(),
            ProgramId = program.Id,
            Title = "Slides",
            Type = ProgramContentType.Lesson,
            LessonFormat = LessonContentFormat.RevealJs,
            Body = "old slides",
        };
        context.AddRange(program, content);
        await context.SaveChangesAsync();
        var service = new ProgramWriteService(context);

        var updated = await service.UpdateContentAsync(
            program.Id,
            content.Id,
            new UpdateContentDto(Body: "updated slides"));

        updated.Should().NotBeNull();
        updated!.LessonFormat.Should().Be(LessonContentFormat.RevealJs);
    }

    [Fact]
    public async Task SubmitUserContentAsync_ShouldSubmitTheCurrentActiveAttempt()
    {
        await using var context = CreateContext();
        var graph = CreateAttemptGraph();
        graph.OldAttempt.SubmittedAt = graph.OldAttempt.CreatedAt.AddMinutes(1);
        graph.OldAttempt.SubmissionData = "old submission";
        context.AddRange(
            graph.Program,
            graph.Content,
            graph.Enrollment,
            graph.OldAttempt,
            graph.CurrentAttempt);
        await context.SaveChangesAsync();
        var service = new ProgramWriteService(context);

        var submitted = await service.SubmitUserContentAsync(
            graph.Program.Id,
            graph.Enrollment.UserId,
            graph.Content.Id,
            "current submission");

        submitted!.Id.Should().Be(graph.CurrentAttempt.Id);
        graph.CurrentAttempt.SubmissionData.Should().Be("current submission");
        graph.CurrentAttempt.SubmittedAt.Should().NotBeNull();
        graph.OldAttempt.SubmissionData.Should().Be("old submission");
    }

    [Fact]
    public async Task UpdateUserProgressAsync_ShouldUpdateTheCurrentActiveAttempt()
    {
        await using var context = CreateContext();
        var graph = CreateAttemptGraph();
        graph.OldAttempt.SubmittedAt = graph.OldAttempt.CreatedAt.AddMinutes(1);
        graph.OldAttempt.Status = ProgressStatus.Completed;
        graph.OldAttempt.IsCompleted = true;
        context.AddRange(
            graph.Program,
            graph.Content,
            graph.Enrollment,
            graph.OldAttempt,
            graph.CurrentAttempt);
        await context.SaveChangesAsync();
        var service = new ProgramWriteService(context);

        await service.UpdateUserProgressAsync(
            graph.Program.Id,
            graph.Enrollment.UserId,
            graph.Content.Id,
            ProgressStatus.Completed);

        graph.CurrentAttempt.Status.Should().Be(ProgressStatus.Completed);
        graph.CurrentAttempt.IsCompleted.Should().BeTrue();
        graph.CurrentAttempt.CompletionPercentage.Should().Be(100);
    }

    [Fact]
    public async Task MarkContentCompletedAsync_ShouldCountDistinctRequiredContent()
    {
        await using var context = CreateContext();
        var graph = CreateAttemptGraph();
        graph.Content.IsRequired = true;
        var remainingContent = new ProgramContent
        {
            Id = Guid.NewGuid(),
            ProgramId = graph.Program.Id,
            Title = "Remaining lesson",
            Type = ProgramContentType.Lesson,
            IsRequired = true,
        };
        graph.OldAttempt.SubmittedAt = graph.OldAttempt.CreatedAt.AddMinutes(1);
        graph.OldAttempt.Status = ProgressStatus.Completed;
        graph.OldAttempt.IsCompleted = true;
        graph.OldAttempt.ProgressPercentage = 100;
        context.AddRange(
            graph.Program,
            graph.Content,
            remainingContent,
            graph.Enrollment,
            graph.OldAttempt,
            graph.CurrentAttempt);
        await context.SaveChangesAsync();
        var service = new ProgramWriteService(context);

        var completed = await service.MarkContentCompletedAsync(
            graph.Program.Id,
            graph.Enrollment.UserId,
            graph.Content.Id);

        completed.Should().BeTrue();
        graph.CurrentAttempt.IsCompleted.Should().BeTrue();
        graph.Enrollment.CompletionPercentage.Should().Be(50);
    }

    [Fact]
    public async Task GetCompletionRatesAsync_ShouldCountEachLearnerOncePerContent()
    {
        await using var context = CreateContext();
        var graph = CreateAttemptGraph();
        graph.OldAttempt.IsCompleted = true;
        graph.OldAttempt.Status = ProgressStatus.Completed;
        graph.CurrentAttempt.IsCompleted = true;
        graph.CurrentAttempt.Status = ProgressStatus.Completed;
        context.AddRange(
            graph.Program,
            graph.Content,
            graph.Enrollment,
            graph.OldAttempt,
            graph.CurrentAttempt);
        await context.SaveChangesAsync();
        var service = new ProgramReadService(context);

        var rates = await service.GetCompletionRatesAsync(graph.Program.Id);

        rates.Should().NotBeNull();
        rates!.ContentCompletionRates[graph.Content.Id].Should().Be(100);
    }

    private static Program CreateProgram() =>
        new()
        {
            Id = Guid.NewGuid(),
            Title = "Course",
            Slug = $"course-{Guid.NewGuid():N}",
        };

    private static AttemptGraph CreateAttemptGraph()
    {
        var program = CreateProgram();
        var content = new ProgramContent
        {
            Id = Guid.NewGuid(),
            ProgramId = program.Id,
            Title = "Assignment",
            Type = ProgramContentType.Assignment,
        };
        var enrollment = new ProgramUser
        {
            Id = Guid.NewGuid(),
            ProgramId = program.Id,
            UserId = Guid.NewGuid(),
            JoinedAt = SystemClock.UtcNow.AddDays(-1),
        };
        var oldAttempt = new ContentInteraction
        {
            Id = Guid.NewGuid(),
            ProgramUserId = enrollment.Id,
            UserId = enrollment.UserId,
            ContentId = content.Id,
            CreatedAt = SystemClock.UtcNow.AddHours(-1),
        };
        var currentAttempt = new ContentInteraction
        {
            Id = Guid.NewGuid(),
            ProgramUserId = enrollment.Id,
            UserId = enrollment.UserId,
            ContentId = content.Id,
            CreatedAt = SystemClock.UtcNow,
            Status = ProgressStatus.InProgress,
        };

        return new AttemptGraph(program, content, enrollment, oldAttempt, currentAttempt);
    }

    private static LearningCoursesTestContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<LearningCoursesTestContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new LearningCoursesTestContext(options);
    }

    private sealed class LearningCoursesTestContext(DbContextOptions<LearningCoursesTestContext> options)
        : DbContext(options), IApplicationDbContext
    {
        public DbSet<Program> Programs => Set<Program>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Program>(entity =>
            {
                entity.Ignore(program => program.ProgramContents);
                entity.Ignore(program => program.ProgramUsers);
                entity.Ignore(program => program.ProgramRatings);
                entity.Ignore(program => program.ProgramWishlists);
            });
            modelBuilder.Entity<ProgramContent>(entity =>
            {
                entity.Ignore(content => content.Program);
                entity.Ignore(content => content.Parent);
                entity.Ignore(content => content.Children);
                entity.Ignore(content => content.ContentInteractions);
            });
            modelBuilder.Entity<ProgramUser>(entity =>
            {
                entity.Ignore(enrollment => enrollment.User);
                entity.Ignore(enrollment => enrollment.Program);
                entity.Ignore(enrollment => enrollment.ContentInteractions);
                entity.Ignore(enrollment => enrollment.ReceivedGrades);
                entity.Ignore(enrollment => enrollment.GivenGrades);
                entity.Ignore(enrollment => enrollment.ProgramRatings);
            });
            modelBuilder.Entity<ContentInteraction>(entity =>
            {
                entity.Ignore(interaction => interaction.User);
                entity.Ignore(interaction => interaction.Content);
                entity.Ignore(interaction => interaction.ProgramUser);
                entity.Ignore(interaction => interaction.ActivityGrades);
                entity.Ignore(interaction => interaction.Events);
            });
        }

        public Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException("Transactions are not needed for these service tests.");
        }
    }

    private sealed record AttemptGraph(
        Program Program,
        ProgramContent Content,
        ProgramUser Enrollment,
        ContentInteraction OldAttempt,
        ContentInteraction CurrentAttempt);
}
