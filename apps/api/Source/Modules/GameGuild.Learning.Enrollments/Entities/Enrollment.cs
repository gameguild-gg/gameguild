
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
            EnrolledAt = SystemClock.UtcNow,
            Progress = 0
        };
    }

    public void UpdateProgress(int progress)
    {
        Progress = Math.Clamp(progress, 0, 100);
        LastActivityAt = SystemClock.UtcNow;
        UpdatedAt = SystemClock.UtcNow;

        if (Progress == 100 && Status == EnrollmentStatus.Active)
        {
            Complete();
        }
    }

    public void Complete()
    {
        Status = EnrollmentStatus.Completed;
        CompletedAt = SystemClock.UtcNow;
        Progress = 100;
        UpdatedAt = SystemClock.UtcNow;
    }

    public void Drop(string? reason = null)
    {
        Status = EnrollmentStatus.Dropped;
        DroppedAt = SystemClock.UtcNow;
        UpdatedAt = SystemClock.UtcNow;
    }

    public void Pause()
    {
        Status = EnrollmentStatus.Paused;
        UpdatedAt = SystemClock.UtcNow;
    }

    public void Resume()
    {
        Status = EnrollmentStatus.Active;
        UpdatedAt = SystemClock.UtcNow;
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
