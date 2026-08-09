namespace GameGuild.TestingLab;

internal static class TestingEventRecurrenceSchedule
{
    private const int MaxOccurrences = 104;

    public static IReadOnlyList<DateTime> Expand(
        DateTime startsAt,
        TestingEventRecurrenceRequest? recurrence)
    {
        if (recurrence == null) return [startsAt];

        Validate(startsAt, recurrence);
        var occurrences = new List<DateTime>();
        switch (recurrence.Frequency)
        {
            case TestingEventRecurrenceFrequency.Daily:
                AddIntervalOccurrences(occurrences, startsAt, recurrence, index => startsAt.AddDays(index * recurrence.Interval));
                break;
            case TestingEventRecurrenceFrequency.Weekly:
                AddWeeklyOccurrences(occurrences, startsAt, recurrence);
                break;
            case TestingEventRecurrenceFrequency.Monthly:
                AddIntervalOccurrences(occurrences, startsAt, recurrence, index => startsAt.AddMonths(index * recurrence.Interval));
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(recurrence), "Unsupported recurrence frequency.");
        }

        if (occurrences.Count == 0)
            throw new ArgumentException("The recurrence window does not include the event start.", nameof(recurrence));

        if (recurrence.OccurrenceCount == null && occurrences.Count == MaxOccurrences && recurrence.EndsAt > occurrences[^1])
            throw new ArgumentException($"A recurrence cannot create more than {MaxOccurrences} events.", nameof(recurrence));

        return occurrences;
    }

    private static void AddIntervalOccurrences(
        ICollection<DateTime> occurrences,
        DateTime startsAt,
        TestingEventRecurrenceRequest recurrence,
        Func<int, DateTime> occurrenceAt)
    {
        for (var index = 0; occurrences.Count < MaxOccurrences; index++)
        {
            var candidate = occurrenceAt(index);
            if (recurrence.EndsAt != null && candidate > recurrence.EndsAt.Value) break;

            occurrences.Add(candidate);
            if (recurrence.OccurrenceCount != null && occurrences.Count == recurrence.OccurrenceCount.Value) break;
        }
    }

    private static void AddWeeklyOccurrences(
        ICollection<DateTime> occurrences,
        DateTime startsAt,
        TestingEventRecurrenceRequest recurrence)
    {
        var daysOfWeek = (recurrence.DaysOfWeek ?? [startsAt.DayOfWeek])
            .Distinct()
            .OrderBy(day => day)
            .ToArray();
        var day = startsAt.Date;

        while (occurrences.Count < MaxOccurrences)
        {
            var candidate = new DateTime(
                day.Year,
                day.Month,
                day.Day,
                startsAt.Hour,
                startsAt.Minute,
                startsAt.Second,
                startsAt.Kind);
            if (recurrence.EndsAt != null && candidate > recurrence.EndsAt.Value) break;

            var weeksSinceStart = (day - startsAt.Date).Days / 7;
            if (candidate >= startsAt &&
                weeksSinceStart % recurrence.Interval == 0 &&
                daysOfWeek.Contains(candidate.DayOfWeek))
            {
                occurrences.Add(candidate);
                if (recurrence.OccurrenceCount != null && occurrences.Count == recurrence.OccurrenceCount.Value) break;
            }

            day = day.AddDays(1);
        }
    }

    private static void Validate(DateTime startsAt, TestingEventRecurrenceRequest recurrence)
    {
        if (!Enum.IsDefined(recurrence.Frequency))
            throw new ArgumentOutOfRangeException(nameof(recurrence), "A supported recurrence frequency is required.");
        if (recurrence.Interval is < 1 or > 52)
            throw new ArgumentOutOfRangeException(nameof(recurrence), "Recurrence interval must be between 1 and 52.");
        if (recurrence.OccurrenceCount is <= 0 or > MaxOccurrences)
            throw new ArgumentOutOfRangeException(nameof(recurrence), $"Occurrence count must be between 1 and {MaxOccurrences}.");
        if (recurrence.OccurrenceCount == null && recurrence.EndsAt == null)
            throw new ArgumentException("A recurring event requires an end date or occurrence count.", nameof(recurrence));
        if (recurrence.EndsAt != null && recurrence.EndsAt.Value < startsAt)
            throw new ArgumentException("Recurrence end must not precede the event start.", nameof(recurrence));
        if (recurrence.DaysOfWeek != null && recurrence.DaysOfWeek.Any(day => !Enum.IsDefined(day)))
            throw new ArgumentOutOfRangeException(nameof(recurrence), "Every recurrence day must be valid.");
    }
}
