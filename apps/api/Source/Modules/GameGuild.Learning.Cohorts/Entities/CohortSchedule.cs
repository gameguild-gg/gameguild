namespace GameGuild.Learning.Cohorts;

public sealed class CohortSchedule : EntityBase
{
    private CohortSchedule() { }

    public Guid CohortId { get; private set; }

    public string TimezoneId { get; private set; } = "UTC";

    public DayOfWeek[] MeetingDays { get; private set; } = [];

    public TimeOnly MeetingStartTime { get; private set; }

    public int MeetingDurationMinutes { get; private set; }

    public CohortPacingMode PacingMode { get; private set; }

    public int UnitsPerPeriod { get; private set; }

    public CohortReleasePolicy ReleasePolicy { get; private set; }

    public static CohortSchedule Create(
        Guid cohortId,
        string timezoneId,
        IReadOnlyCollection<DayOfWeek> meetingDays,
        TimeOnly meetingStartTime,
        int meetingDurationMinutes,
        CohortPacingMode pacingMode,
        int unitsPerPeriod,
        CohortReleasePolicy releasePolicy,
        Guid? tenantId = null)
    {
        if (cohortId == Guid.Empty)
        {
            throw new ArgumentException("Cohort ID is required.", nameof(cohortId));
        }

        var schedule = new CohortSchedule
        {
            CohortId = cohortId,
            TenantId = tenantId
        };

        schedule.SetRules(
            timezoneId,
            meetingDays,
            meetingStartTime,
            meetingDurationMinutes,
            pacingMode,
            unitsPerPeriod,
            releasePolicy);

        return schedule;
    }

    public void UpdateRules(
        string timezoneId,
        IReadOnlyCollection<DayOfWeek> meetingDays,
        TimeOnly meetingStartTime,
        int meetingDurationMinutes,
        CohortPacingMode pacingMode,
        int unitsPerPeriod,
        CohortReleasePolicy releasePolicy)
    {
        SetRules(
            timezoneId,
            meetingDays,
            meetingStartTime,
            meetingDurationMinutes,
            pacingMode,
            unitsPerPeriod,
            releasePolicy);
        Touch();
    }

    private void SetRules(
        string timezoneId,
        IReadOnlyCollection<DayOfWeek> meetingDays,
        TimeOnly meetingStartTime,
        int meetingDurationMinutes,
        CohortPacingMode pacingMode,
        int unitsPerPeriod,
        CohortReleasePolicy releasePolicy)
    {
        TimezoneId = ValidateTimezone(timezoneId);

        ArgumentNullException.ThrowIfNull(meetingDays);
        if (meetingDays.Count == 0)
        {
            throw new ArgumentException("At least one meeting day is required.", nameof(meetingDays));
        }

        if (meetingDurationMinutes <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(meetingDurationMinutes),
                meetingDurationMinutes,
                "Meeting duration must be greater than zero.");
        }

        if (unitsPerPeriod <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(unitsPerPeriod),
                unitsPerPeriod,
                "Units per period must be greater than zero.");
        }

        MeetingDays = meetingDays.Distinct().ToArray();
        MeetingStartTime = meetingStartTime;
        MeetingDurationMinutes = meetingDurationMinutes;
        PacingMode = pacingMode;
        UnitsPerPeriod = unitsPerPeriod;
        ReleasePolicy = releasePolicy;
    }

    private static string ValidateTimezone(string timezoneId)
    {
        if (string.IsNullOrWhiteSpace(timezoneId))
        {
            throw new ArgumentException("Timezone is required.", nameof(timezoneId));
        }

        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(timezoneId.Trim()).Id;
        }
        catch (TimeZoneNotFoundException exception)
        {
            throw new ArgumentException("Timezone is not recognized.", nameof(timezoneId), exception);
        }
        catch (InvalidTimeZoneException exception)
        {
            throw new ArgumentException("Timezone is invalid.", nameof(timezoneId), exception);
        }
    }
}
