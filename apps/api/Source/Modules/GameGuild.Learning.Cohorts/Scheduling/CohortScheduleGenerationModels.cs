using GameGuild.Learning.Courses;

namespace GameGuild.Learning.Cohorts;

public sealed record CohortScheduleGenerationRequest(
    Guid CohortId,
    DateOnly FirstInstructionalDate,
    DateOnly CohortEndDate,
    string TimezoneId,
    IReadOnlyCollection<DayOfWeek> MeetingDays,
    TimeOnly MeetingStartTime,
    int MeetingDurationMinutes,
    CohortPacingMode PacingMode,
    int UnitsPerPeriod,
    CohortReleasePolicy ReleasePolicy,
    IReadOnlyCollection<DateOnly> SkippedDates,
    IReadOnlyCollection<CanonicalScheduleContent> Content,
    int AssessmentDueOffsetDays = 7);

public sealed record CanonicalScheduleContent(
    Guid ContentId,
    Guid? AssessmentId,
    Guid? ParentId,
    string Title,
    ProgramContentType Type,
    int SortOrder,
    int? EstimatedMinutes);

public sealed record CohortSchedulePreviewItem(
    Guid? ProgramContentId,
    Guid? AssessmentId,
    CohortScheduleItemType Type,
    int InstructionalWeek,
    int SortOrder,
    DateTime? StartsAt,
    DateTime? EndsAt,
    DateTime? AvailableFrom,
    DateTime? AvailableUntil,
    DateTime? DueAt,
    string Title);

public sealed record CohortScheduleConflict(
    string Code,
    ScheduleConflictSeverity Severity,
    string Message,
    Guid? ProgramContentId,
    Guid? AssessmentId);

public sealed record InstructorScheduleSlot(Guid CohortId, DateTime StartsAt, DateTime EndsAt);

public sealed record CohortSchedulePreview(
    IReadOnlyList<CohortSchedulePreviewItem> Items,
    IReadOnlyList<CohortScheduleConflict> Conflicts,
    DateOnly CalculatedEndDate)
{
    public bool HasBlockingConflicts =>
        Conflicts.Any(conflict => conflict.Severity == ScheduleConflictSeverity.Blocking);
}

public static class ScheduleConflictCodes
{
    public const string InstructorOverlap = "instructor-overlap";
    public const string ReleaseAfterDue = "release-after-due";
    public const string CohortEndOverflow = "cohort-end-overflow";
}
