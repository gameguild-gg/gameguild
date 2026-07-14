using FluentAssertions;
using GameGuild.Learning.Cohorts;
using Xunit;

namespace GameGuild.Learning.Cohorts.UnitTests;

public class CohortScheduleItemTests
{
    [Fact]
    public void Create_RequiresReferenceOrExceptionalTitle()
    {
        var act = () => CohortScheduleItem.Create(
            Guid.NewGuid(),
            null,
            null,
            CohortScheduleItemType.LiveSession,
            null);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_WithExceptionalTitle_AllowsStandaloneMeeting()
    {
        var cohortId = Guid.NewGuid();

        var item = CohortScheduleItem.Create(
            cohortId,
            null,
            null,
            CohortScheduleItemType.LiveSession,
            "Guest review session");

        item.CohortId.Should().Be(cohortId);
        item.Title.Should().Be("Guest review session");
        item.Status.Should().Be(CohortScheduleItemStatus.Draft);
        item.VisibilityOverride.Should().Be(CohortVisibilityOverride.Inherited);
    }

    [Fact]
    public void Shift_MovesEveryDateAndPreservesTheirRelationships()
    {
        var item = CohortScheduleItem.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            CohortScheduleItemType.AssessmentWindow,
            "Final project",
            instructionalWeek: 4,
            sortOrder: 2,
            startsAt: new DateTime(2026, 9, 7, 19, 0, 0, DateTimeKind.Utc),
            endsAt: new DateTime(2026, 9, 7, 21, 0, 0, DateTimeKind.Utc),
            availableFrom: new DateTime(2026, 9, 1, 12, 0, 0, DateTimeKind.Utc),
            availableUntil: new DateTime(2026, 9, 14, 23, 59, 0, DateTimeKind.Utc),
            dueAt: new DateTime(2026, 9, 14, 21, 0, 0, DateTimeKind.Utc));

        item.Shift(TimeSpan.FromDays(7));

        item.StartsAt.Should().Be(new DateTime(2026, 9, 14, 19, 0, 0, DateTimeKind.Utc));
        item.EndsAt.Should().Be(new DateTime(2026, 9, 14, 21, 0, 0, DateTimeKind.Utc));
        item.AvailableFrom.Should().Be(new DateTime(2026, 9, 8, 12, 0, 0, DateTimeKind.Utc));
        item.AvailableUntil.Should().Be(new DateTime(2026, 9, 21, 23, 59, 0, DateTimeKind.Utc));
        item.DueAt.Should().Be(new DateTime(2026, 9, 21, 21, 0, 0, DateTimeKind.Utc));
    }

    [Fact]
    public void Shift_PreservesNullDates()
    {
        var item = CohortScheduleItem.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            null,
            CohortScheduleItemType.ContentRelease,
            null);

        item.Shift(TimeSpan.FromDays(2));

        item.StartsAt.Should().BeNull();
        item.EndsAt.Should().BeNull();
        item.AvailableFrom.Should().BeNull();
        item.AvailableUntil.Should().BeNull();
        item.DueAt.Should().BeNull();
    }
}
