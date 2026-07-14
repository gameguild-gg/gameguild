using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace GameGuild.Learning.Cohorts.UnitTests;

public sealed class ProgramContentScheduleGuardTests
{
    [Fact]
    public async Task ScheduledFutureClass_ProtectsReferencedContent()
    {
        await using var context = CreateContext();
        var contentId = Guid.NewGuid();
        var cohort = Cohort.Create(
            Guid.NewGuid(),
            "Morning class",
            new DateTime(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 12, 18, 0, 0, 0, DateTimeKind.Utc),
            20);
        var item = CohortScheduleItem.Create(
            cohort.Id,
            contentId,
            null,
            CohortScheduleItemType.ContentRelease,
            "Lesson",
            availableFrom: new DateTime(2026, 8, 12, 9, 0, 0, DateTimeKind.Utc),
            status: CohortScheduleItemStatus.Scheduled);
        context.AddRange(cohort, item);
        await context.SaveChangesAsync();
        var guard = new ProgramContentScheduleGuard(context);

        var result = await guard.HasActiveScheduleReference(contentId);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task CancelledClass_DoesNotProtectReferencedContent()
    {
        await using var context = CreateContext();
        var contentId = Guid.NewGuid();
        var cohort = Cohort.Create(
            Guid.NewGuid(),
            "Cancelled class",
            new DateTime(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 12, 18, 0, 0, 0, DateTimeKind.Utc),
            20);
        cohort.Cancel();
        var item = CohortScheduleItem.Create(
            cohort.Id,
            contentId,
            null,
            CohortScheduleItemType.ContentRelease,
            "Lesson",
            status: CohortScheduleItemStatus.Scheduled);
        context.AddRange(cohort, item);
        await context.SaveChangesAsync();
        var guard = new ProgramContentScheduleGuard(context);

        var result = await guard.HasActiveScheduleReference(contentId);

        result.Should().BeFalse();
    }

    private static CohortScheduleTestDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<CohortScheduleTestDbContext>()
            .UseInMemoryDatabase($"ProgramContentScheduleGuard_{Guid.NewGuid()}")
            .Options;
        return new CohortScheduleTestDbContext(options);
    }
}
