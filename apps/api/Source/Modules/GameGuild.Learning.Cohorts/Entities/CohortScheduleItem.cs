namespace GameGuild.Learning.Cohorts;

public sealed class CohortScheduleItem : EntityBase
{
    private CohortScheduleItem() { }

    public Guid CohortId { get; private set; }

    public Guid? ProgramContentId { get; private set; }

    public Guid? AssessmentId { get; private set; }

    public CohortScheduleItemType Type { get; private set; }

    public int InstructionalWeek { get; private set; }

    public int SortOrder { get; private set; }

    public DateTime? StartsAt { get; private set; }

    public DateTime? EndsAt { get; private set; }

    public DateTime? AvailableFrom { get; private set; }

    public DateTime? AvailableUntil { get; private set; }

    public DateTime? DueAt { get; private set; }

    public string? Title { get; private set; }

    public string? Location { get; private set; }

    public string? MeetingUrl { get; private set; }

    public CohortScheduleItemStatus Status { get; private set; }

    public CohortVisibilityOverride VisibilityOverride { get; private set; }

    public static CohortScheduleItem Create(
        Guid cohortId,
        Guid? programContentId,
        Guid? assessmentId,
        CohortScheduleItemType type,
        string? exceptionalTitle,
        int instructionalWeek = 0,
        int sortOrder = 0,
        DateTime? startsAt = null,
        DateTime? endsAt = null,
        DateTime? availableFrom = null,
        DateTime? availableUntil = null,
        DateTime? dueAt = null,
        string? location = null,
        string? meetingUrl = null,
        CohortScheduleItemStatus status = CohortScheduleItemStatus.Draft,
        CohortVisibilityOverride visibilityOverride = CohortVisibilityOverride.Inherited,
        Guid? tenantId = null)
    {
        if (cohortId == Guid.Empty)
        {
            throw new ArgumentException("Cohort ID is required.", nameof(cohortId));
        }

        var title = exceptionalTitle?.Trim();
        if (programContentId is null && assessmentId is null && string.IsNullOrWhiteSpace(title))
        {
            throw new ArgumentException(
                "A content reference, assessment reference, or exceptional title is required.",
                nameof(exceptionalTitle));
        }

        if (instructionalWeek < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(instructionalWeek));
        }

        if (sortOrder < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(sortOrder));
        }

        return new CohortScheduleItem
        {
            CohortId = cohortId,
            ProgramContentId = programContentId,
            AssessmentId = assessmentId,
            Type = type,
            InstructionalWeek = instructionalWeek,
            SortOrder = sortOrder,
            StartsAt = startsAt,
            EndsAt = endsAt,
            AvailableFrom = availableFrom,
            AvailableUntil = availableUntil,
            DueAt = dueAt,
            Title = title,
            Location = location?.Trim(),
            MeetingUrl = meetingUrl?.Trim(),
            Status = status,
            VisibilityOverride = visibilityOverride,
            TenantId = tenantId
        };
    }

    public void Shift(TimeSpan offset)
    {
        StartsAt = StartsAt?.Add(offset);
        EndsAt = EndsAt?.Add(offset);
        AvailableFrom = AvailableFrom?.Add(offset);
        AvailableUntil = AvailableUntil?.Add(offset);
        DueAt = DueAt?.Add(offset);
        Touch();
    }
}
