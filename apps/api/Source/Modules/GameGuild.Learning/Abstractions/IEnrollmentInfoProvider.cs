namespace GameGuild.Learning.Abstractions;

/// <summary>
/// Interface for accessing enrollment information across modules
/// </summary>
public interface IEnrollmentInfoProvider
{
    /// <summary>
    /// Checks if a user is enrolled in a course
    /// </summary>
    Task<bool> IsUserEnrolledAsync(
        Guid userId, 
        Guid courseId, 
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets all course IDs the user is enrolled in
    /// </summary>
    Task<IReadOnlyList<Guid>> GetUserEnrolledCourseIdsAsync(
        Guid userId,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets enrollment information for a user and course
    /// </summary>
    Task<EnrollmentInfo?> GetEnrollmentInfoAsync(
        Guid userId,
        Guid courseId,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets enrollment information for multiple courses
    /// </summary>
    Task<IReadOnlyDictionary<Guid, EnrollmentInfo>> GetEnrollmentInfoBatchAsync(
        Guid userId,
        IEnumerable<Guid> courseIds,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets the count of users enrolled in a course
    /// </summary>
    Task<int> GetEnrollmentCountAsync(
        Guid courseId,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Enrollment information shared across modules
/// </summary>
public record EnrollmentInfo
{
    public Guid Id { get; init; }
    public Guid UserId { get; init; }
    public Guid CourseId { get; init; }
    public DateTime EnrolledAt { get; init; }
    public decimal ProgressPercent { get; init; }
    public int CompletedContentCount { get; init; }
    public int TotalContentCount { get; init; }
    public DateTime? LastAccessedAt { get; init; }
    public DateTime? CompletedAt { get; init; }
    public bool IsCompleted => CompletedAt.HasValue;
    public EnrollmentStatus Status { get; init; }
}

/// <summary>
/// Enrollment status enum
/// </summary>
public enum EnrollmentStatus
{
    Active = 0,
    Completed = 1,
    Paused = 2,
    Expired = 3,
    Cancelled = 4
}
