using GameGuild.Entities;

namespace GameGuild.Learning.Cohorts;

/// <summary>
/// Represents a group of students learning together in a time-bounded course session
/// </summary>
public class Cohort : EntityBase
{
    public Guid CourseId { get; private set; }
    public new Guid? TenantId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public DateTime StartDate { get; private set; }
    public DateTime EndDate { get; private set; }
    public int MaxCapacity { get; private set; }
    public int CurrentEnrollmentCount { get; private set; }
    public CohortStatus Status { get; private set; }
    public bool IsOpen { get; private set; }
    public Guid? InstructorId { get; private set; }
    public string? MeetingSchedule { get; private set; } // JSON or CRON expression

    private Cohort() { } // EF Core

    public static Cohort Create(
        Guid courseId,
        string name,
        DateTime startDate,
        DateTime endDate,
        int maxCapacity,
        Guid? tenantId = null,
        Guid? instructorId = null)
    {
        return new Cohort
        {
            Id = Guid.NewGuid(),
            CourseId = courseId,
            TenantId = tenantId,
            Name = name,
            StartDate = startDate,
            EndDate = endDate,
            MaxCapacity = maxCapacity,
            CurrentEnrollmentCount = 0,
            Status = CohortStatus.Scheduled,
            IsOpen = false,
            InstructorId = instructorId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    public bool CanEnroll() => IsOpen && CurrentEnrollmentCount < MaxCapacity && Status == CohortStatus.Active;

    public void Open()
    {
        IsOpen = true;
        Status = CohortStatus.Active;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Close()
    {
        IsOpen = false;
        UpdatedAt = DateTime.UtcNow;
    }

    public void IncrementEnrollment()
    {
        CurrentEnrollmentCount++;
        UpdatedAt = DateTime.UtcNow;
    }

    public void DecrementEnrollment()
    {
        if (CurrentEnrollmentCount > 0)
            CurrentEnrollmentCount--;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Complete()
    {
        Status = CohortStatus.Completed;
        IsOpen = false;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Cancel()
    {
        Status = CohortStatus.Cancelled;
        IsOpen = false;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetDescription(string? description)
    {
        Description = description;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetMeetingSchedule(string? schedule)
    {
        MeetingSchedule = schedule;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Update(
        string? name,
        string? description,
        DateTime? startDate,
        DateTime? endDate,
        int? maxCapacity,
        Guid? instructorId,
        string? meetingSchedule)
    {
        if (name != null) Name = name;
        Description = description;
        if (startDate.HasValue) StartDate = startDate.Value;
        if (endDate.HasValue) EndDate = endDate.Value;
        if (maxCapacity.HasValue) MaxCapacity = maxCapacity.Value;
        InstructorId = instructorId;
        MeetingSchedule = meetingSchedule;
        UpdatedAt = DateTime.UtcNow;
    }
}

public enum CohortStatus
{
    Scheduled,
    Active,
    Completed,
    Cancelled
}
