using GameGuild.Abstractions;

namespace GameGuild.Learning.Enrollments;

/// <summary>
/// Represents a student's enrollment in a course
/// </summary>
public class Enrollment : EntityBase
{
    public Guid CourseId { get; private set; }
    public Guid UserId { get; private set; }
    public Guid? CohortId { get; private set; }
    public EnrollmentStatus Status { get; private set; }
    public DateTime EnrolledAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }
    public DateTime? DroppedAt { get; private set; }
    public int Progress { get; private set; } // 0-100
    public DateTime? LastActivityAt { get; private set; }

    private Enrollment() { } // EF Core

    public static Enrollment Create(Guid courseId, Guid userId, Guid? cohortId = null)
    {
        return new Enrollment
        {
            Id = Guid.NewGuid(),
            CourseId = courseId,
            UserId = userId,
            CohortId = cohortId,
            Status = EnrollmentStatus.Active,
            EnrolledAt = DateTime.UtcNow,
            Progress = 0,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    public void UpdateProgress(int progress)
    {
        Progress = Math.Clamp(progress, 0, 100);
        LastActivityAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;

        if (Progress == 100 && Status == EnrollmentStatus.Active)
        {
            Complete();
        }
    }

    public void Complete()
    {
        Status = EnrollmentStatus.Completed;
        CompletedAt = DateTime.UtcNow;
        Progress = 100;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Drop(string? reason = null)
    {
        Status = EnrollmentStatus.Dropped;
        DroppedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Pause()
    {
        Status = EnrollmentStatus.Paused;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Resume()
    {
        Status = EnrollmentStatus.Active;
        UpdatedAt = DateTime.UtcNow;
    }
}

public enum EnrollmentStatus
{
    Active,
    Paused,
    Completed,
    Dropped,
    Expired
}
