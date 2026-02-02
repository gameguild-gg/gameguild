namespace GameGuild.Learning.Abstractions;

/// <summary>
/// Interface for accessing course information across modules
/// </summary>
public interface ICourseInfoProvider
{
    /// <summary>
    /// Gets basic course information by ID
    /// </summary>
    Task<CourseBasicInfo?> GetCourseBasicInfoAsync(Guid courseId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets basic course information for multiple courses
    /// </summary>
    Task<IReadOnlyDictionary<Guid, CourseBasicInfo>> GetCourseBasicInfoBatchAsync(
        IEnumerable<Guid> courseIds,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Checks if a course exists and is published
    /// </summary>
    Task<bool> IsCourseAvailableAsync(Guid courseId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets course IDs matching the specified criteria
    /// </summary>
    Task<IReadOnlyList<Guid>> FindCourseIdsAsync(
        CourseSearchCriteria criteria,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Basic course information shared across modules
/// </summary>
public record CourseBasicInfo
{
    public Guid Id { get; init; }
    public Guid TenantId { get; init; }
    public string Title { get; init; } = string.Empty;
    public string? Slug { get; init; }
    public string? Description { get; init; }
    public string? ThumbnailUrl { get; init; }
    public Guid? InstructorId { get; init; }
    public string? DifficultyLevel { get; init; }
    public int? DurationMinutes { get; init; }
    public bool IsPublished { get; init; }
    public bool IsFree { get; init; }
    public IReadOnlyList<string> Tags { get; init; } = Array.Empty<string>();
    public IReadOnlyList<Guid> SkillIds { get; init; } = Array.Empty<Guid>();
    public DateTime CreatedAt { get; init; }
    public DateTime? PublishedAt { get; init; }
}

/// <summary>
/// Criteria for searching courses
/// </summary>
public record CourseSearchCriteria
{
    public Guid? TenantId { get; init; }
    public IReadOnlyList<Guid>? SkillIds { get; init; }
    public IReadOnlyList<string>? Tags { get; init; }
    public IReadOnlyList<string>? DifficultyLevels { get; init; }
    public Guid? InstructorId { get; init; }
    public bool? IsPublished { get; init; }
    public bool? IsFree { get; init; }
    public int? MaxResults { get; init; }
}
