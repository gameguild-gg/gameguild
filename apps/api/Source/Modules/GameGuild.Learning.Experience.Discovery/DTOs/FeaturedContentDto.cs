namespace GameGuild.Learning.Experience.Discovery;

/// <summary>
/// DTO for featured content response
/// </summary>
public sealed record FeaturedContentDto(
    Guid Id,
    Guid? CourseId,
    Guid? LearningPathId,
    Guid? TenantId,
    string Title,
    string? Subtitle,
    string? ImageUrl,
    string? LinkUrl,
    FeaturedContentType Type,
    int DisplayOrder,
    DateTime? StartsAt,
    DateTime? EndsAt,
    bool IsActive,
    string? TargetAudience,
    DateTime CreatedAt,
    DateTime UpdatedAt);

/// <summary>
/// DTO for creating featured content
/// </summary>
public sealed record CreateFeaturedContentDto(
    FeaturedContentType Type,
    string Title,
    int DisplayOrder,
    Guid? CourseId = null,
    Guid? LearningPathId = null,
    string? Subtitle = null,
    string? ImageUrl = null,
    string? LinkUrl = null,
    DateTime? StartsAt = null,
    DateTime? EndsAt = null,
    string? TargetAudience = null);

/// <summary>
/// DTO for updating featured content
/// </summary>
public sealed record UpdateFeaturedContentDto(
    string? Title = null,
    string? Subtitle = null,
    string? ImageUrl = null,
    string? LinkUrl = null,
    int? DisplayOrder = null,
    DateTime? StartsAt = null,
    DateTime? EndsAt = null,
    bool? IsActive = null,
    string? TargetAudience = null);

/// <summary>
/// DTO for course collection response
/// </summary>
public sealed record CourseCollectionDto(
    Guid Id,
    Guid? TenantId,
    Guid CuratorId,
    string Title,
    string Slug,
    string? Description,
    string? ImageUrl,
    bool IsPublished,
    bool IsFeatured,
    int CourseCount,
    CollectionType Type,
    DateTime CreatedAt,
    DateTime UpdatedAt);

/// <summary>
/// DTO for creating a course collection
/// </summary>
public sealed record CreateCourseCollectionDto(
    string Title,
    CollectionType Type = CollectionType.Curated,
    string? Description = null,
    string? ImageUrl = null);

/// <summary>
/// DTO for updating a course collection
/// </summary>
public sealed record UpdateCourseCollectionDto(
    string? Title = null,
    string? Description = null,
    string? ImageUrl = null,
    bool? IsFeatured = null);

/// <summary>
/// DTO for search history (analytics)
/// </summary>
public sealed record SearchHistoryDto(
    Guid Id,
    Guid? UserId,
    string Query,
    int ResultCount,
    Guid? ClickedCourseId,
    string? Filters,
    DateTime CreatedAt);

/// <summary>
/// DTO for recording a search
/// </summary>
public sealed record RecordSearchDto(
    string Query,
    int ResultCount,
    string? Filters = null);

/// <summary>
/// DTO for recording a search click
/// </summary>
public sealed record RecordSearchClickDto(
    Guid SearchId,
    Guid ClickedCourseId);
