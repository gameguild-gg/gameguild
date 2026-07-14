namespace GameGuild.Learning.Cohorts;

public sealed class ScheduleConflictDetector
{
    public IReadOnlyList<CohortScheduleConflict> Detect(
        Guid cohortId,
        DateOnly cohortEndDate,
        IReadOnlyCollection<CohortSchedulePreviewItem> items,
        IReadOnlyCollection<InstructorScheduleSlot> instructorSchedule)
    {
        ArgumentNullException.ThrowIfNull(items);
        ArgumentNullException.ThrowIfNull(instructorSchedule);

        var conflicts = new List<CohortScheduleConflict>();

        foreach (var item in items)
        {
            DetectReleaseAfterDue(item, conflicts);
            DetectCohortEndOverflow(item, cohortEndDate, conflicts);

            if (item.Type == CohortScheduleItemType.LiveSession)
            {
                DetectInstructorOverlap(item, cohortId, instructorSchedule, conflicts);
            }
        }

        return conflicts;
    }

    private static void DetectReleaseAfterDue(
        CohortSchedulePreviewItem item,
        ICollection<CohortScheduleConflict> conflicts)
    {
        if (item.AvailableFrom is null || item.DueAt is null || item.AvailableFrom <= item.DueAt)
        {
            return;
        }

        conflicts.Add(new CohortScheduleConflict(
            ScheduleConflictCodes.ReleaseAfterDue,
            ScheduleConflictSeverity.Blocking,
            $"{item.Title} becomes available after its due date.",
            item.ProgramContentId,
            item.AssessmentId));
    }

    private static void DetectCohortEndOverflow(
        CohortSchedulePreviewItem item,
        DateOnly cohortEndDate,
        ICollection<CohortScheduleConflict> conflicts)
    {
        var lastDate = new[]
            {
                item.StartsAt,
                item.EndsAt,
                item.AvailableFrom,
                item.AvailableUntil,
                item.DueAt
            }
            .Where(value => value.HasValue)
            .Select(value => DateOnly.FromDateTime(value!.Value))
            .DefaultIfEmpty(DateOnly.MinValue)
            .Max();

        if (lastDate <= cohortEndDate)
        {
            return;
        }

        conflicts.Add(new CohortScheduleConflict(
            ScheduleConflictCodes.CohortEndOverflow,
            ScheduleConflictSeverity.Advisory,
            $"{item.Title} extends beyond the class end date.",
            item.ProgramContentId,
            item.AssessmentId));
    }

    private static void DetectInstructorOverlap(
        CohortSchedulePreviewItem item,
        Guid cohortId,
        IEnumerable<InstructorScheduleSlot> instructorSchedule,
        ICollection<CohortScheduleConflict> conflicts)
    {
        if (item.StartsAt is null || item.EndsAt is null)
        {
            return;
        }

        var overlaps = instructorSchedule.Any(slot =>
            slot.CohortId != cohortId &&
            item.StartsAt.Value < slot.EndsAt &&
            item.EndsAt.Value > slot.StartsAt);

        if (!overlaps)
        {
            return;
        }

        conflicts.Add(new CohortScheduleConflict(
            ScheduleConflictCodes.InstructorOverlap,
            ScheduleConflictSeverity.Blocking,
            $"{item.Title} overlaps another class assigned to this instructor.",
            item.ProgramContentId,
            item.AssessmentId));
    }
}
