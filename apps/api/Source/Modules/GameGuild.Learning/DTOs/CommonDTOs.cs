namespace GameGuild.Learning.DTOs;

/// <summary>
/// Common course summary DTO used across learning modules
/// </summary>
public record CourseSummaryDto
{
    public Guid Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public string? Slug { get; init; }
    public string? Description { get; init; }
    public string? ThumbnailUrl { get; init; }
    public Guid? InstructorId { get; init; }
    public string? InstructorName { get; init; }
    public decimal? Rating { get; init; }
    public int ReviewCount { get; init; }
    public int EnrollmentCount { get; init; }
    public string? DifficultyLevel { get; init; }
    public int? DurationMinutes { get; init; }
    public IReadOnlyList<string> Tags { get; init; } = Array.Empty<string>();
    public bool IsFree { get; init; }
    public decimal? Price { get; init; }
    public string? Currency { get; init; }
}

/// <summary>
/// Common content item summary DTO
/// </summary>
public record ContentSummaryDto
{
    public Guid Id { get; init; }
    public Guid CourseId { get; init; }
    public string Title { get; init; } = string.Empty;
    public string ContentType { get; init; } = string.Empty; // video, article, quiz, assignment
    public int? DurationMinutes { get; init; }
    public int OrderIndex { get; init; }
    public bool IsPreview { get; init; }
}

/// <summary>
/// Common user learning profile summary
/// </summary>
public record LearnerSummaryDto
{
    public Guid UserId { get; init; }
    public string? DisplayName { get; init; }
    public string? AvatarUrl { get; init; }
    public int CoursesCompleted { get; init; }
    public int TotalLearningMinutes { get; init; }
    public IReadOnlyList<string> SkillInterests { get; init; } = Array.Empty<string>();
}

/// <summary>
/// Common enrollment summary DTO
/// </summary>
public record EnrollmentSummaryDto
{
    public Guid Id { get; init; }
    public Guid UserId { get; init; }
    public Guid CourseId { get; init; }
    public string? CourseTitle { get; init; }
    public DateTime EnrolledAt { get; init; }
    public decimal ProgressPercent { get; init; }
    public DateTime? LastAccessedAt { get; init; }
    public DateTime? CompletedAt { get; init; }
    public bool IsCompleted { get; init; }
}

/// <summary>
/// Common progress tracking DTO
/// </summary>
public record ProgressDto
{
    public Guid UserId { get; init; }
    public Guid EntityId { get; init; } // Course, LearningPath, or Content ID
    public string EntityType { get; init; } = string.Empty; // "course", "learning-path", "content"
    public decimal ProgressPercent { get; init; }
    public int CompletedItems { get; init; }
    public int TotalItems { get; init; }
    public DateTime? StartedAt { get; init; }
    public DateTime? LastActivityAt { get; init; }
    public DateTime? CompletedAt { get; init; }
    public int TimeSpentMinutes { get; init; }
}

/// <summary>
/// Common skill DTO
/// </summary>
public record SkillDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Slug { get; init; }
    public string? Category { get; init; }
    public string? Description { get; init; }
    public int CourseCount { get; init; }
}

/// <summary>
/// Common tag DTO for categorization
/// </summary>
public record TagDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Slug { get; init; }
    public string? Category { get; init; }
    public int UsageCount { get; init; }
}
