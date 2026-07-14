namespace GameGuild.Learning.Cohorts;

public sealed record PreviewCohortScheduleRequest(
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
    int AssessmentDueOffsetDays = 7);

public sealed record CohortScheduleDto(
    Guid Id,
    Guid CohortId,
    int Version,
    string TimezoneId,
    IReadOnlyCollection<DayOfWeek> MeetingDays,
    TimeOnly MeetingStartTime,
    int MeetingDurationMinutes,
    CohortPacingMode PacingMode,
    int UnitsPerPeriod,
    CohortReleasePolicy ReleasePolicy,
    IReadOnlyList<CohortScheduleItemDto> Items,
    IReadOnlyList<Guid> UnscheduledContentIds);

public sealed record CohortSchedulePreviewDto(
    IReadOnlyList<CohortSchedulePreviewItemDto> Items,
    IReadOnlyList<CohortScheduleConflictDto> Conflicts,
    DateOnly CalculatedEndDate,
    bool HasBlockingConflicts);

public sealed record CohortScheduleItemDto(
    Guid Id,
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
    string Title,
    string? Location,
    string? MeetingUrl,
    CohortScheduleItemStatus Status,
    CohortVisibilityOverride VisibilityOverride);

public sealed record CohortSchedulePreviewItemDto(
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

public sealed record CohortScheduleConflictDto(
    string Code,
    ScheduleConflictSeverity Severity,
    string Message,
    Guid? ProgramContentId,
    Guid? AssessmentId);

public sealed record CourseCohortCalendarDto(
    Guid CourseId,
    IReadOnlyList<CohortCalendarEntryDto> Entries);

public sealed record CohortCalendarEntryDto(
    Guid CohortId,
    string CohortName,
    Guid ItemId,
    CohortScheduleItemType Type,
    string Title,
    DateTime? StartsAt,
    DateTime? EndsAt,
    DateTime? AvailableFrom,
    DateTime? DueAt,
    CohortScheduleItemStatus Status);

internal static class CohortScheduleDtoMapper
{
    internal static CohortSchedulePreviewDto ToDto(this CohortSchedulePreview preview) =>
        new(
            preview.Items.Select(ToDto).ToArray(),
            preview.Conflicts.Select(ToDto).ToArray(),
            preview.CalculatedEndDate,
            preview.HasBlockingConflicts);

    internal static CohortSchedulePreviewItemDto ToDto(CohortSchedulePreviewItem item) =>
        new(
            item.ProgramContentId,
            item.AssessmentId,
            item.Type,
            item.InstructionalWeek,
            item.SortOrder,
            item.StartsAt,
            item.EndsAt,
            item.AvailableFrom,
            item.AvailableUntil,
            item.DueAt,
            item.Title);

    internal static CohortScheduleConflictDto ToDto(CohortScheduleConflict conflict) =>
        new(
            conflict.Code,
            conflict.Severity,
            conflict.Message,
            conflict.ProgramContentId,
            conflict.AssessmentId);

    internal static CohortScheduleItemDto ToDto(CohortScheduleItem item) =>
        new(
            item.Id,
            item.ProgramContentId,
            item.AssessmentId,
            item.Type,
            item.InstructionalWeek,
            item.SortOrder,
            item.StartsAt,
            item.EndsAt,
            item.AvailableFrom,
            item.AvailableUntil,
            item.DueAt,
            item.Title ?? string.Empty,
            item.Location,
            item.MeetingUrl,
            item.Status,
            item.VisibilityOverride);

    internal static CohortScheduleDto ToDto(
        CohortSchedule schedule,
        IReadOnlyList<CohortScheduleItem> items,
        IReadOnlyList<Guid> unscheduledContentIds) =>
        new(
            schedule.Id,
            schedule.CohortId,
            schedule.Version,
            schedule.TimezoneId,
            schedule.MeetingDays,
            schedule.MeetingStartTime,
            schedule.MeetingDurationMinutes,
            schedule.PacingMode,
            schedule.UnitsPerPeriod,
            schedule.ReleasePolicy,
            items.Select(ToDto).ToArray(),
            unscheduledContentIds);
}
