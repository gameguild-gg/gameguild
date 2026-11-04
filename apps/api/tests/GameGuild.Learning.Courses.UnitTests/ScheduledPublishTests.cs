using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

using LearningProgram = GameGuild.Learning.Courses.Program;

namespace GameGuild.Learning.Courses.UnitTests;

/// <summary>
/// Scheduled publish (issue #1226 N10): scheduling must honor <c>publishAt</c> instead of
/// publishing immediately, and the schedule must be realized by the worker when due.
/// </summary>
public class ScheduledPublishTests
{
    [Fact]
    public async Task ScheduleProgramAsync_WithFuturePublishAt_DoesNotPublishImmediately()
    {
        await using var context = CreateContext();
        var program = SeedDraft(context);
        var service = CreateLifecycleService(context);

        var scheduled = await service.ScheduleProgramAsync(program.Id, SystemClock.UtcNow.AddHours(2));

        scheduled.Should().NotBeNull();
        scheduled!.Status.Should().Be(ContentStatus.Draft, "a future schedule must not publish early");
        scheduled.Visibility.Should().NotBe(ContentVisibility.Public);
        scheduled.ScheduledPublishAt.Should().NotBeNull().And.BeAfter(SystemClock.UtcNow.AddHours(1));
    }

    [Fact]
    public async Task ScheduleProgramAsync_WithDuePublishAt_PublishesImmediately()
    {
        await using var context = CreateContext();
        var program = SeedDraft(context);
        var service = CreateLifecycleService(context);

        var scheduled = await service.ScheduleProgramAsync(program.Id, SystemClock.UtcNow.AddHours(-1));

        scheduled.Should().NotBeNull();
        scheduled!.Status.Should().Be(ContentStatus.Published, "a schedule already due is applied now");
        scheduled.Visibility.Should().Be(ContentVisibility.Public);
        scheduled.ScheduledPublishAt.Should().BeNull("the schedule is consumed with the transition");
    }

    [Fact]
    public async Task SchedulePublishAsync_WithFuturePublishAt_DoesNotPublishImmediately()
    {
        // The required-program variant shares the same scheduling semantics.
        await using var context = CreateContext();
        var program = SeedDraft(context);
        var service = CreateLifecycleService(context);

        var scheduled = await service.SchedulePublishAsync(program.Id, SystemClock.UtcNow.AddHours(2));

        scheduled.Status.Should().Be(ContentStatus.Draft);
        scheduled.ScheduledPublishAt.Should().NotBeNull();
    }

    [Fact]
    public async Task PublishDueAsync_PublishesOnlyProgramsWhoseScheduleHasMatured()
    {
        await using var context = CreateContext();
        var due = await ScheduleFuture(context, SystemClock.UtcNow.AddMinutes(-5));
        var notYetDue = await ScheduleFuture(context, SystemClock.UtcNow.AddHours(1));
        var unscheduled = SeedDraft(context);
        var worker = new PublishDueProgramsWorker(context, NullLogger<PublishDueProgramsWorker>.Instance);

        var published = await worker.PublishDueAsync();

        published.Should().ContainSingle().Which.Should().Be(due.Id);
        context.Set<LearningProgram>().Single(p => p.Id == due.Id).Status.Should().Be(ContentStatus.Published);
        context.Set<LearningProgram>().Single(p => p.Id == notYetDue.Id).Status.Should().Be(ContentStatus.Draft);
        context.Set<LearningProgram>().Single(p => p.Id == unscheduled.Id).Status.Should().Be(ContentStatus.Draft);
    }

    [Fact]
    public async Task PublishDueAsync_ConsumesTheScheduleAndIsIdempotent()
    {
        await using var context = CreateContext();
        var due = await ScheduleFuture(context, SystemClock.UtcNow.AddMinutes(-5));
        var worker = new PublishDueProgramsWorker(context, NullLogger<PublishDueProgramsWorker>.Instance);

        await worker.PublishDueAsync();
        var secondSweep = await worker.PublishDueAsync();

        secondSweep.Should().BeEmpty("a realized schedule is cleared and never re-published");
        context.Set<LearningProgram>().Single(p => p.Id == due.Id).ScheduledPublishAt.Should().BeNull();
    }

    [Fact]
    public async Task PublishDueAsync_NeverTouchesTerminalOrAlreadyPublishedPrograms()
    {
        await using var context = CreateContext();
        var archived = SeedProgram(context, ContentStatus.Archived);
        archived.ScheduledPublishAt = SystemClock.UtcNow.AddMinutes(-5);
        var published = SeedProgram(context, ContentStatus.Published);
        published.ScheduledPublishAt = SystemClock.UtcNow.AddMinutes(-5);
        await context.SaveChangesAsync();
        var worker = new PublishDueProgramsWorker(context, NullLogger<PublishDueProgramsWorker>.Instance);

        var result = await worker.PublishDueAsync();

        result.Should().BeEmpty("a stale schedule must neither resurrect nor re-publish");
        context.Set<LearningProgram>().Single(p => p.Id == archived.Id).Status.Should().Be(ContentStatus.Archived);
        context.Set<LearningProgram>().Single(p => p.Id == published.Id).Status.Should().Be(ContentStatus.Published);
    }

    [Fact]
    public void ScheduledPublishAt_RoundTripsThroughMetadataAndReadsTheLegacyFormat()
    {
        // Compatibility: rows scheduled by the legacy path (raw boxed DateTime under the
        // same metadata key) must still be understood by the scheduled-publish worker.
        var program = SeedDraft(CreateContext());
        var legacy = new DateTime(2026, 5, 1, 9, 30, 0, DateTimeKind.Utc);

        program.SetMetadata(LearningProgram.ScheduledPublishAtMetadataKey, legacy);
        program.ScheduledPublishAt.Should().Be(legacy);

        program.ScheduledPublishAt = legacy.AddHours(3);
        program.ScheduledPublishAt.Should().Be(legacy.AddHours(3));
    }

    [Fact]
    public void AddCoursesModule_RegistersTheScheduledPublishWorkerAndHost()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
        services.AddScoped<IApplicationDbContext>(_ => Moq.Mock.Of<IApplicationDbContext>());
        services.AddLogging();

        services.AddCoursesModule();
        using var provider = services.BuildServiceProvider();

        provider.GetRequiredService<IPublishDueProgramsWorker>().Should().BeOfType<PublishDueProgramsWorker>();
        provider.GetServices<IHostedService>()
            .Should().Contain(service => service is ScheduledPublishBackgroundService);
    }

    private static ProgramLifecycleService CreateLifecycleService(ScheduledPublishDbContext context) =>
        new(context);

    private static LearningProgram SeedDraft(ScheduledPublishDbContext context) =>
        SeedProgram(context, ContentStatus.Draft);

    private static LearningProgram SeedProgram(ScheduledPublishDbContext context, ContentStatus status)
    {
        var program = new LearningProgram
        {
            Id = Guid.NewGuid(),
            Title = "Scheduled publish course",
            Status = status,
            Visibility = ContentVisibility.Private
        };
        context.Set<LearningProgram>().Add(program);
        context.SaveChanges();
        return program;
    }

    private static async Task<LearningProgram> ScheduleFuture(ScheduledPublishDbContext context, DateTime publishAt)
    {
        var program = SeedDraft(context);
        program.ScheduledPublishAt = publishAt;
        await context.SaveChangesAsync();
        return program;
    }

    private static ScheduledPublishDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ScheduledPublishDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var context = new ScheduledPublishDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }

    private sealed class ScheduledPublishDbContext(DbContextOptions<ScheduledPublishDbContext> options)
        : DbContext(options), IApplicationDbContext
    {
        public Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
            => Database.BeginTransactionAsync(cancellationToken);

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<LearningProgram>(entity =>
            {
                entity.HasKey(program => program.Id);
                entity.Ignore(program => program.ProgramContents);
                entity.Ignore(program => program.ProgramUsers);
                entity.Ignore(program => program.ProgramRatings);
                entity.Ignore(program => program.ProgramWishlists);
            });
        }
    }
}
