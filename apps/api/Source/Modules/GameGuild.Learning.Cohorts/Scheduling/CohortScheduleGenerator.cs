using GameGuild.Learning.Courses;

namespace GameGuild.Learning.Cohorts;

public sealed class CohortScheduleGenerator
{
    private readonly ScheduleConflictDetector _conflictDetector;

    public CohortScheduleGenerator() : this(new ScheduleConflictDetector()) { }

    public CohortScheduleGenerator(ScheduleConflictDetector conflictDetector)
    {
        _conflictDetector = conflictDetector;
    }

    public CohortSchedulePreview Generate(CohortScheduleGenerationRequest request)
    {
        Validate(request);

        if (request.PacingMode == CohortPacingMode.Manual)
        {
            return new CohortSchedulePreview([], [], request.FirstInstructionalDate);
        }

        var timezone = TimeZoneInfo.FindSystemTimeZoneById(request.TimezoneId);
        var skippedDates = request.SkippedDates.ToHashSet();
        var items = new List<CohortSchedulePreviewItem>();

        AddLiveSessions(request, timezone, skippedDates, items);
        AddContentItems(request, timezone, skippedDates, items);

        var conflicts = _conflictDetector.Detect(request.CohortId, request.CohortEndDate, items, []);
        var calculatedEndDate = CalculateEndDate(request.FirstInstructionalDate, items);

        return new CohortSchedulePreview(items, conflicts, calculatedEndDate);
    }

    private static void AddLiveSessions(
        CohortScheduleGenerationRequest request,
        TimeZoneInfo timezone,
        IReadOnlySet<DateOnly> skippedDates,
        ICollection<CohortSchedulePreviewItem> items)
    {
        var meetingDays = request.MeetingDays.ToHashSet();
        var sortOrder = 0;

        for (var date = request.FirstInstructionalDate; date <= request.CohortEndDate; date = date.AddDays(1))
        {
            if (!meetingDays.Contains(date.DayOfWeek) || skippedDates.Contains(date))
            {
                continue;
            }

            var startsAt = ToUtc(date, request.MeetingStartTime, timezone);
            items.Add(new CohortSchedulePreviewItem(
                null,
                null,
                CohortScheduleItemType.LiveSession,
                InstructionalWeek(request.FirstInstructionalDate, date),
                sortOrder++,
                startsAt,
                startsAt.AddMinutes(request.MeetingDurationMinutes),
                null,
                null,
                null,
                "Class meeting"));
        }
    }

    private static void AddContentItems(
        CohortScheduleGenerationRequest request,
        TimeZoneInfo timezone,
        IReadOnlySet<DateOnly> skippedDates,
        ICollection<CohortSchedulePreviewItem> items)
    {
        var content = SelectSchedulableContent(request);

        for (var index = 0; index < content.Length; index++)
        {
            var scheduled = content[index];
            var canonical = scheduled.Content;
            var releaseDate = CalculateReleaseDate(request, skippedDates, scheduled.PeriodIndex);
            var releaseAt = ToUtc(releaseDate, request.MeetingStartTime, timezone);
            releaseAt = request.ReleasePolicy switch
            {
                CohortReleasePolicy.BeforeMeeting => releaseAt.AddDays(-1),
                CohortReleasePolicy.Immediately => ToUtc(
                    request.FirstInstructionalDate,
                    TimeOnly.MinValue,
                    timezone),
                _ => releaseAt
            };

            var type = canonical.AssessmentId.HasValue
                ? CohortScheduleItemType.AssessmentWindow
                : CohortScheduleItemType.ContentRelease;
            DateTime? dueAt = canonical.AssessmentId.HasValue
                ? releaseAt.AddDays(request.AssessmentDueOffsetDays)
                : null;

            items.Add(new CohortSchedulePreviewItem(
                canonical.ContentId,
                canonical.AssessmentId,
                type,
                InstructionalWeek(request.FirstInstructionalDate, releaseDate),
                index,
                null,
                null,
                releaseAt,
                null,
                dueAt,
                canonical.Title));
        }
    }

    private static ScheduledContent[] SelectSchedulableContent(
        CohortScheduleGenerationRequest request)
    {
        var ordered = request.Content
            .OrderBy(item => item.SortOrder)
            .ThenBy(item => item.ContentId)
            .ToArray();
        if (request.PacingMode != CohortPacingMode.OneModulePerWeek)
        {
            return ordered
                .Where(item => item.Type != ProgramContentType.Module)
                .Select((item, index) => new ScheduledContent(item, index))
                .ToArray();
        }

        var modules = ordered
            .Where(item => item.Type == ProgramContentType.Module && item.ParentId is null)
            .ToArray();
        if (modules.Length == 0)
        {
            modules = ordered.Where(item => item.Type == ProgramContentType.Module).ToArray();
        }
        if (modules.Length == 0)
        {
            return ordered.Select((item, index) => new ScheduledContent(item, index)).ToArray();
        }

        var children = ordered
            .Where(item => item.ParentId.HasValue)
            .GroupBy(item => item.ParentId!.Value)
            .ToDictionary(group => group.Key, group => group.ToArray());
        var scheduled = new List<ScheduledContent>(ordered.Length);
        var visited = new HashSet<Guid>();

        void AddTree(CanonicalScheduleContent item, int periodIndex)
        {
            if (!visited.Add(item.ContentId))
            {
                return;
            }

            scheduled.Add(new ScheduledContent(item, periodIndex));
            if (!children.TryGetValue(item.ContentId, out var childItems))
            {
                return;
            }

            foreach (var child in childItems)
            {
                AddTree(child, periodIndex);
            }
        }

        for (var moduleIndex = 0; moduleIndex < modules.Length; moduleIndex++)
        {
            AddTree(modules[moduleIndex], moduleIndex);
        }

        var nextPeriod = modules.Length;
        foreach (var item in ordered.Where(item => !visited.Contains(item.ContentId)))
        {
            AddTree(item, nextPeriod++);
        }

        return scheduled.ToArray();
    }

    private sealed record ScheduledContent(CanonicalScheduleContent Content, int PeriodIndex);

    private static DateOnly CalculateReleaseDate(
        CohortScheduleGenerationRequest request,
        IReadOnlySet<DateOnly> skippedDates,
        int contentIndex)
    {
        return request.PacingMode switch
        {
            CohortPacingMode.OneModulePerWeek => MoveSkippedDate(
                request.FirstInstructionalDate.AddDays(contentIndex * 7),
                request.MeetingDays,
                skippedDates),
            CohortPacingMode.OneLessonPerMeeting => FindMeetingDate(
                request.FirstInstructionalDate,
                request.MeetingDays,
                skippedDates,
                contentIndex),
            CohortPacingMode.FixedLessonsPerWeek => MoveSkippedDate(
                request.FirstInstructionalDate.AddDays((contentIndex / request.UnitsPerPeriod) * 7),
                request.MeetingDays,
                skippedDates),
            _ => request.FirstInstructionalDate
        };
    }

    private static DateOnly FindMeetingDate(
        DateOnly firstDate,
        IReadOnlyCollection<DayOfWeek> meetingDays,
        IReadOnlySet<DateOnly> skippedDates,
        int targetIndex)
    {
        var allowedDays = meetingDays.ToHashSet();
        var matchIndex = -1;

        for (var date = firstDate; date < firstDate.AddYears(20); date = date.AddDays(1))
        {
            if (!allowedDays.Contains(date.DayOfWeek) || skippedDates.Contains(date))
            {
                continue;
            }

            matchIndex++;
            if (matchIndex == targetIndex)
            {
                return date;
            }
        }

        throw new InvalidOperationException("A meeting date could not be calculated within twenty years.");
    }

    private static DateOnly MoveSkippedDate(
        DateOnly date,
        IReadOnlyCollection<DayOfWeek> meetingDays,
        IReadOnlySet<DateOnly> skippedDates)
    {
        if (!skippedDates.Contains(date))
        {
            return date;
        }

        return FindMeetingDate(date.AddDays(1), meetingDays, skippedDates, 0);
    }

    private static DateTime ToUtc(DateOnly date, TimeOnly time, TimeZoneInfo timezone)
    {
        var local = DateTime.SpecifyKind(date.ToDateTime(time), DateTimeKind.Unspecified);
        return TimeZoneInfo.ConvertTimeToUtc(local, timezone);
    }

    private static int InstructionalWeek(DateOnly firstDate, DateOnly date) =>
        Math.Max(1, ((date.DayNumber - firstDate.DayNumber) / 7) + 1);

    private static DateOnly CalculateEndDate(
        DateOnly firstInstructionalDate,
        IEnumerable<CohortSchedulePreviewItem> items) =>
        items.SelectMany(item => new[]
            {
                item.StartsAt,
                item.EndsAt,
                item.AvailableFrom,
                item.AvailableUntil,
                item.DueAt
            })
            .Where(value => value.HasValue)
            .Select(value => DateOnly.FromDateTime(value!.Value))
            .DefaultIfEmpty(firstInstructionalDate)
            .Max();

    private static void Validate(CohortScheduleGenerationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.MeetingDays);
        ArgumentNullException.ThrowIfNull(request.SkippedDates);
        ArgumentNullException.ThrowIfNull(request.Content);

        if (request.CohortId == Guid.Empty)
        {
            throw new ArgumentException("Cohort ID is required.", nameof(request));
        }

        if (request.CohortEndDate < request.FirstInstructionalDate)
        {
            throw new ArgumentException("Class end date must not precede the first instructional date.", nameof(request));
        }

        if (request.MeetingDays.Count == 0)
        {
            throw new ArgumentException("At least one meeting day is required.", nameof(request));
        }

        if (request.MeetingDurationMinutes <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(request), "Meeting duration must be greater than zero.");
        }

        if (request.UnitsPerPeriod <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(request), "Units per period must be greater than zero.");
        }

        if (request.AssessmentDueOffsetDays < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(request), "Assessment due offset cannot be negative.");
        }

        _ = TimeZoneInfo.FindSystemTimeZoneById(request.TimezoneId);
    }
}
