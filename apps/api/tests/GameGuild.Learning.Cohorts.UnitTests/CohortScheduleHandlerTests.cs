using FluentAssertions;
using GameGuild.CQRS;
using GameGuild.Learning.Cohorts;
using GameGuild.Learning.Courses;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Xunit;

namespace GameGuild.Learning.Cohorts.UnitTests;

public sealed class CohortScheduleHandlerTests
{
    [Fact]
    public async Task Preview_DoesNotPersistSchedule()
    {
        await using var context = CreateContext();
        var (courseId, cohort) = await SeedCourseAsync(context);
        context.ResetSaveChangesCalls();
        var handler = CreatePreviewHandler(context);

        var result = await handler.Handle(
            new PreviewCohortScheduleQuery(courseId, cohort.Id, Rules()),
            CancellationToken.None);

        result.Items.Should().NotBeEmpty();
        context.Set<CohortSchedule>().Should().BeEmpty();
        context.SaveChangesCalls.Should().Be(0);
    }

    [Fact]
    public async Task Apply_RejectsBlockingConflicts()
    {
        await using var context = CreateContext();
        var instructorId = Guid.NewGuid();
        var (courseId, cohort) = await SeedCourseAsync(context, instructorId);
        var otherCohort = Cohort.Create(
            courseId,
            "Morning class",
            new DateTime(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 12, 18, 0, 0, 0, DateTimeKind.Utc),
            20,
            instructorId: instructorId);
        var otherMeeting = CohortScheduleItem.Create(
            otherCohort.Id,
            null,
            null,
            CohortScheduleItemType.LiveSession,
            "Morning meeting",
            startsAt: new DateTime(2026, 8, 12, 9, 0, 0, DateTimeKind.Utc),
            endsAt: new DateTime(2026, 8, 12, 10, 30, 0, DateTimeKind.Utc));
        context.AddRange(otherCohort, otherMeeting);
        await context.SaveChangesAsync();
        context.ResetSaveChangesCalls();
        var handler = CreateApplyHandler(context);

        var act = () => handler.Handle(
            new ApplyCohortScheduleCommand(courseId, cohort.Id, 0, Rules(), true),
            CancellationToken.None);

        await act.Should().ThrowAsync<RequestValidationException>()
            .WithMessage("*blocking conflict*");
        context.Set<CohortSchedule>().Should().BeEmpty();
        context.SaveChangesCalls.Should().Be(0);
    }

    [Fact]
    public async Task Apply_PersistsScheduleAndCommitsTransaction()
    {
        await using var context = CreateContext();
        var (courseId, cohort) = await SeedCourseAsync(context);
        context.ResetSaveChangesCalls();
        var handler = CreateApplyHandler(context);

        var result = await handler.Handle(
            new ApplyCohortScheduleCommand(courseId, cohort.Id, 0, Rules(), true),
            CancellationToken.None);

        result.Version.Should().Be(1);
        result.Items.Should().NotBeEmpty();
        context.Set<CohortSchedule>().Should().ContainSingle();
        context.Set<CohortScheduleItem>().Should().HaveCount(result.Items.Count);
        context.SaveChangesCalls.Should().Be(1);
        context.LastTransaction.Should().NotBeNull();
        context.LastTransaction!.CommitCalled.Should().BeTrue();
        context.LastTransaction.RollbackCalled.Should().BeFalse();
    }

    [Fact]
    public async Task Apply_WithStaleVersion_ThrowsVersionConflict()
    {
        await using var context = CreateContext();
        var (courseId, cohort) = await SeedCourseAsync(context);
        var schedule = CohortSchedule.Create(
            cohort.Id,
            "UTC",
            [DayOfWeek.Wednesday],
            new TimeOnly(9, 0),
            90,
            CohortPacingMode.OneModulePerWeek,
            1,
            CohortReleasePolicy.Weekly);
        schedule.Version = 3;
        context.Add(schedule);
        await context.SaveChangesAsync();
        context.ResetSaveChangesCalls();
        var handler = CreateApplyHandler(context);

        var act = () => handler.Handle(
            new ApplyCohortScheduleCommand(courseId, cohort.Id, 2, Rules(), true),
            CancellationToken.None);

        await act.Should().ThrowAsync<CohortScheduleVersionConflictException>();
        context.SaveChangesCalls.Should().Be(0);
    }

    [Fact]
    public async Task Get_ReturnsPersistedItemsAndUnscheduledCanonicalContent()
    {
        await using var context = CreateContext();
        var (courseId, cohort) = await SeedCourseAsync(context);
        var scheduledContentId = await context.Set<ProgramContent>()
            .Select(content => content.Id)
            .SingleAsync();
        var unscheduledContent = new ProgramContent
        {
            ProgramId = courseId,
            Title = "Advanced topics",
            Type = ProgramContentType.Lesson,
            SortOrder = 1
        };
        var schedule = Schedule(cohort.Id, version: 2);
        var item = CohortScheduleItem.Create(
            cohort.Id,
            scheduledContentId,
            null,
            CohortScheduleItemType.ContentRelease,
            "Foundations",
            availableFrom: new DateTime(2026, 8, 12, 9, 0, 0, DateTimeKind.Utc),
            status: CohortScheduleItemStatus.Scheduled);
        context.AddRange(unscheduledContent, schedule, item);
        await context.SaveChangesAsync();
        context.ResetSaveChangesCalls();
        var handler = new GetCohortScheduleQueryHandler(context);

        var result = await handler.Handle(
            new GetCohortScheduleQuery(courseId, cohort.Id),
            CancellationToken.None);

        result.Should().NotBeNull();
        result!.Version.Should().Be(2);
        result.Items.Should().ContainSingle(entry => entry.Id == item.Id);
        result.UnscheduledContentIds.Should().Equal(unscheduledContent.Id);
        context.SaveChangesCalls.Should().Be(0);
    }

    [Fact]
    public async Task UpdateItem_ChangesDeliveryDetailsAndIncrementsScheduleVersion()
    {
        await using var context = CreateContext();
        var (courseId, cohort) = await SeedCourseAsync(context);
        var schedule = Schedule(cohort.Id, version: 4);
        var item = CohortScheduleItem.Create(
            cohort.Id,
            null,
            null,
            CohortScheduleItemType.LiveSession,
            "Original class",
            startsAt: new DateTime(2026, 8, 12, 9, 0, 0, DateTimeKind.Utc),
            endsAt: new DateTime(2026, 8, 12, 10, 30, 0, DateTimeKind.Utc));
        context.AddRange(schedule, item);
        await context.SaveChangesAsync();
        context.ResetSaveChangesCalls();
        var handler = new UpdateCohortScheduleItemCommandHandler(context);

        var result = await handler.Handle(
            new UpdateCohortScheduleItemCommand(
                courseId,
                cohort.Id,
                item.Id,
                4,
                new UpdateCohortScheduleItemRequest(
                    "Studio session",
                    item.StartsAt,
                    item.EndsAt,
                    null,
                    null,
                    null,
                    "Lab 2",
                    "https://meet.example/studio",
                    CohortScheduleItemStatus.Published,
                    CohortVisibilityOverride.Visible)),
            CancellationToken.None);

        result.Version.Should().Be(5);
        result.Items.Single(entry => entry.Id == item.Id).Should().BeEquivalentTo(
            new
            {
                Title = "Studio session",
                Location = "Lab 2",
                MeetingUrl = "https://meet.example/studio",
                Status = CohortScheduleItemStatus.Published,
                VisibilityOverride = CohortVisibilityOverride.Visible
            });
        context.SaveChangesCalls.Should().Be(1);
    }

    [Fact]
    public async Task ShiftFollowing_MovesAnchorAndLaterItemsOnly()
    {
        await using var context = CreateContext();
        var (courseId, cohort) = await SeedCourseAsync(context);
        var schedule = Schedule(cohort.Id, version: 1);
        var previous = Meeting(cohort.Id, "Previous", 1, 0, new DateTime(2026, 8, 5, 9, 0, 0, DateTimeKind.Utc));
        var anchor = Meeting(cohort.Id, "Anchor", 2, 0, new DateTime(2026, 8, 12, 9, 0, 0, DateTimeKind.Utc));
        var following = Meeting(cohort.Id, "Following", 3, 0, new DateTime(2026, 8, 19, 9, 0, 0, DateTimeKind.Utc));
        var previousStartsAt = previous.StartsAt;
        var anchorStartsAt = anchor.StartsAt;
        var followingStartsAt = following.StartsAt;
        context.AddRange(schedule, previous, anchor, following);
        await context.SaveChangesAsync();
        context.ResetSaveChangesCalls();
        var handler = new ShiftCohortScheduleItemsCommandHandler(context);

        var result = await handler.Handle(
            new ShiftCohortScheduleItemsCommand(
                courseId,
                cohort.Id,
                anchor.Id,
                1,
                2,
                ScheduleShiftScope.Following),
            CancellationToken.None);

        result.Version.Should().Be(2);
        result.Items.Single(entry => entry.Id == previous.Id).StartsAt.Should().Be(previousStartsAt);
        result.Items.Single(entry => entry.Id == anchor.Id).StartsAt.Should().Be(anchorStartsAt!.Value.AddDays(2));
        result.Items.Single(entry => entry.Id == following.Id).StartsAt.Should().Be(followingStartsAt!.Value.AddDays(2));
        context.SaveChangesCalls.Should().Be(1);
    }

    [Fact]
    public async Task Calendar_ReturnsEntriesFromEveryCourseClass()
    {
        await using var context = CreateContext();
        var (courseId, firstCohort) = await SeedCourseAsync(context);
        var secondCohort = Cohort.Create(
            courseId,
            "Night class",
            new DateTime(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 12, 18, 0, 0, 0, DateTimeKind.Utc),
            20);
        var firstMeeting = Meeting(firstCohort.Id, "Morning", 1, 0, new DateTime(2026, 8, 12, 9, 0, 0, DateTimeKind.Utc));
        var secondMeeting = Meeting(secondCohort.Id, "Night", 1, 0, new DateTime(2026, 8, 13, 22, 0, 0, DateTimeKind.Utc));
        context.AddRange(secondCohort, firstMeeting, secondMeeting);
        await context.SaveChangesAsync();
        context.ResetSaveChangesCalls();
        var handler = new GetCourseCohortCalendarQueryHandler(context);

        var result = await handler.Handle(
            new GetCourseCohortCalendarQuery(courseId),
            CancellationToken.None);

        result.Entries.Should().HaveCount(2);
        result.Entries.Select(entry => entry.CohortName).Should().BeEquivalentTo("Evening class", "Night class");
        context.SaveChangesCalls.Should().Be(0);
    }

    private static PreviewCohortScheduleQueryHandler CreatePreviewHandler(CohortScheduleTestDbContext context) =>
        new(context, new CohortScheduleGenerator(), new ScheduleConflictDetector());

    private static ApplyCohortScheduleCommandHandler CreateApplyHandler(CohortScheduleTestDbContext context) =>
        new(context, new CohortScheduleGenerator(), new ScheduleConflictDetector());

    private static PreviewCohortScheduleRequest Rules() =>
        new(
            new DateOnly(2026, 8, 12),
            new DateOnly(2026, 12, 18),
            "UTC",
            [DayOfWeek.Wednesday],
            new TimeOnly(9, 0),
            90,
            CohortPacingMode.OneModulePerWeek,
            1,
            CohortReleasePolicy.Weekly,
            [],
            7);

    private static CohortSchedule Schedule(Guid cohortId, int version)
    {
        var schedule = CohortSchedule.Create(
            cohortId,
            "UTC",
            [DayOfWeek.Wednesday],
            new TimeOnly(9, 0),
            90,
            CohortPacingMode.OneModulePerWeek,
            1,
            CohortReleasePolicy.Weekly);
        schedule.Version = version;
        return schedule;
    }

    private static CohortScheduleItem Meeting(
        Guid cohortId,
        string title,
        int instructionalWeek,
        int sortOrder,
        DateTime startsAt) =>
        CohortScheduleItem.Create(
            cohortId,
            null,
            null,
            CohortScheduleItemType.LiveSession,
            title,
            instructionalWeek,
            sortOrder,
            startsAt,
            startsAt.AddMinutes(90));

    private static async Task<(Guid CourseId, Cohort Cohort)> SeedCourseAsync(
        CohortScheduleTestDbContext context,
        Guid? instructorId = null)
    {
        var courseId = Guid.NewGuid();
        var cohort = Cohort.Create(
            courseId,
            "Evening class",
            new DateTime(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 12, 18, 0, 0, 0, DateTimeKind.Utc),
            20,
            instructorId: instructorId);
        var module = new ProgramContent
        {
            ProgramId = courseId,
            Title = "Foundations",
            Type = ProgramContentType.Module,
            SortOrder = 0
        };

        context.AddRange(cohort, module);
        await context.SaveChangesAsync();
        return (courseId, cohort);
    }

    private static CohortScheduleTestDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<CohortScheduleTestDbContext>()
            .UseInMemoryDatabase($"CohortSchedules_{Guid.NewGuid()}")
            .Options;
        return new CohortScheduleTestDbContext(options);
    }
}

internal sealed class CohortScheduleTestDbContext(DbContextOptions<CohortScheduleTestDbContext> options)
    : DbContext(options), IApplicationDbContext
{
    public int SaveChangesCalls { get; private set; }
    public RecordingCohortTransaction? LastTransaction { get; private set; }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        SaveChangesCalls++;
        return await base.SaveChangesAsync(cancellationToken);
    }

    public void ResetSaveChangesCalls() => SaveChangesCalls = 0;

    public Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        LastTransaction = new RecordingCohortTransaction();
        return Task.FromResult<IDbContextTransaction>(LastTransaction);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Cohort>().HasKey(entity => entity.Id);
        modelBuilder.Entity<CohortSchedule>().HasKey(entity => entity.Id);
        modelBuilder.Entity<CohortScheduleItem>().HasKey(entity => entity.Id);
        modelBuilder.Entity<ProgramContent>(entity =>
        {
            entity.HasKey(content => content.Id);
            entity.Ignore(content => content.Program);
            entity.Ignore(content => content.Parent);
            entity.Ignore(content => content.Children);
            entity.Ignore(content => content.ContentInteractions);
        });
    }
}

internal sealed class RecordingCohortTransaction : IDbContextTransaction
{
    public Guid TransactionId { get; } = Guid.NewGuid();
    public bool CommitCalled { get; private set; }
    public bool RollbackCalled { get; private set; }

    public void Commit() => CommitCalled = true;

    public Task CommitAsync(CancellationToken cancellationToken = default)
    {
        CommitCalled = true;
        return Task.CompletedTask;
    }

    public void Rollback() => RollbackCalled = true;

    public Task RollbackAsync(CancellationToken cancellationToken = default)
    {
        RollbackCalled = true;
        return Task.CompletedTask;
    }

    public void Dispose() { }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
