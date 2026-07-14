using FluentAssertions;
using GameGuild.Learning.Cohorts;
using Xunit;

namespace GameGuild.Learning.Cohorts.UnitTests;

public class ScheduleConflictDetectorTests
{
    private readonly ScheduleConflictDetector _detector = new();

    [Fact]
    public void InstructorOverlap_IsBlocking()
    {
        var cohortId = Guid.NewGuid();
        var items = new[]
        {
            PreviewItem(
                CohortScheduleItemType.LiveSession,
                startsAt: new DateTime(2026, 8, 12, 12, 0, 0, DateTimeKind.Utc),
                endsAt: new DateTime(2026, 8, 12, 13, 30, 0, DateTimeKind.Utc))
        };
        var occupied = new[]
        {
            new InstructorScheduleSlot(
                Guid.NewGuid(),
                new DateTime(2026, 8, 12, 13, 0, 0, DateTimeKind.Utc),
                new DateTime(2026, 8, 12, 14, 0, 0, DateTimeKind.Utc))
        };

        var conflicts = _detector.Detect(cohortId, new DateOnly(2026, 12, 18), items, occupied);

        conflicts.Should().ContainSingle(conflict =>
            conflict.Code == ScheduleConflictCodes.InstructorOverlap &&
            conflict.Severity == ScheduleConflictSeverity.Blocking);
    }

    [Fact]
    public void ReleaseAfterDue_IsBlocking()
    {
        var items = new[]
        {
            PreviewItem(
                CohortScheduleItemType.AssessmentWindow,
                availableFrom: new DateTime(2026, 8, 20, 9, 0, 0, DateTimeKind.Utc),
                dueAt: new DateTime(2026, 8, 19, 23, 59, 0, DateTimeKind.Utc))
        };

        var conflicts = _detector.Detect(Guid.NewGuid(), new DateOnly(2026, 12, 18), items, []);

        conflicts.Should().ContainSingle(conflict =>
            conflict.Code == ScheduleConflictCodes.ReleaseAfterDue &&
            conflict.Severity == ScheduleConflictSeverity.Blocking);
    }

    [Fact]
    public void CohortEndOverflow_IsAdvisory()
    {
        var items = new[]
        {
            PreviewItem(
                CohortScheduleItemType.ContentRelease,
                availableFrom: new DateTime(2026, 9, 2, 9, 0, 0, DateTimeKind.Utc))
        };

        var conflicts = _detector.Detect(Guid.NewGuid(), new DateOnly(2026, 8, 31), items, []);

        conflicts.Should().ContainSingle(conflict =>
            conflict.Code == ScheduleConflictCodes.CohortEndOverflow &&
            conflict.Severity == ScheduleConflictSeverity.Advisory);
    }

    private static CohortSchedulePreviewItem PreviewItem(
        CohortScheduleItemType type,
        DateTime? startsAt = null,
        DateTime? endsAt = null,
        DateTime? availableFrom = null,
        DateTime? dueAt = null) =>
        new(
            Guid.NewGuid(),
            null,
            type,
            1,
            0,
            startsAt,
            endsAt,
            availableFrom,
            null,
            dueAt,
            "Scheduled item");
}
