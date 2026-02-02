namespace GameGuild.Learning.Abstractions;

/// <summary>
/// Interface for accessing user progress information across modules
/// </summary>
public interface IProgressInfoProvider
{
    /// <summary>
    /// Gets progress for a user on a specific course
    /// </summary>
    Task<ProgressInfo?> GetCourseProgressAsync(
        Guid userId,
        Guid courseId,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets progress for a user on multiple courses
    /// </summary>
    Task<IReadOnlyDictionary<Guid, ProgressInfo>> GetCourseProgressBatchAsync(
        Guid userId,
        IEnumerable<Guid> courseIds,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets progress for a user on a learning path
    /// </summary>
    Task<ProgressInfo?> GetLearningPathProgressAsync(
        Guid userId,
        Guid learningPathId,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets all courses the user has completed
    /// </summary>
    Task<IReadOnlyList<Guid>> GetCompletedCourseIdsAsync(
        Guid userId,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets courses the user is currently in progress with
    /// </summary>
    Task<IReadOnlyList<Guid>> GetInProgressCourseIdsAsync(
        Guid userId,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets the user's overall learning statistics
    /// </summary>
    Task<LearningStatistics> GetUserLearningStatisticsAsync(
        Guid userId,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Progress information shared across modules
/// </summary>
public record ProgressInfo
{
    public Guid EntityId { get; init; }
    public ProgressEntityType EntityType { get; init; }
    public decimal ProgressPercent { get; init; }
    public int CompletedItems { get; init; }
    public int TotalItems { get; init; }
    public DateTime? StartedAt { get; init; }
    public DateTime? LastActivityAt { get; init; }
    public DateTime? CompletedAt { get; init; }
    public int TimeSpentMinutes { get; init; }
    public bool IsCompleted => CompletedAt.HasValue;
    public bool IsStarted => StartedAt.HasValue;
}

/// <summary>
/// Entity type for progress tracking
/// </summary>
public enum ProgressEntityType
{
    Course = 0,
    LearningPath = 1,
    Content = 2,
    Module = 3,
    Quiz = 4,
    Assignment = 5
}

/// <summary>
/// User's overall learning statistics
/// </summary>
public record LearningStatistics
{
    public Guid UserId { get; init; }
    public int CoursesStarted { get; init; }
    public int CoursesCompleted { get; init; }
    public int LearningPathsStarted { get; init; }
    public int LearningPathsCompleted { get; init; }
    public int TotalLearningMinutes { get; init; }
    public int ContentItemsCompleted { get; init; }
    public int QuizzesCompleted { get; init; }
    public int AssignmentsCompleted { get; init; }
    public int CertificatesEarned { get; init; }
    public int AchievementsEarned { get; init; }
    public DateTime? FirstActivityAt { get; init; }
    public DateTime? LastActivityAt { get; init; }
    public int CurrentStreak { get; init; }
    public int LongestStreak { get; init; }
}
