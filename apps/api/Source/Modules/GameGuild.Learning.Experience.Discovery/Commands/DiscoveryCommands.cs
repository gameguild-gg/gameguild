using GameGuild.CQRS;

namespace GameGuild.Learning.Experience.Discovery;

// ===== FEATURED CONTENT COMMANDS =====

/// <summary>
/// Command to create featured content
/// </summary>
public record CreateFeaturedContentCommand(
    FeaturedContentType Type,
    string Title,
    int DisplayOrder,
    Guid? CourseId = null,
    Guid? LearningPathId = null,
    Guid? TenantId = null,
    string? Subtitle = null,
    string? ImageUrl = null,
    string? LinkUrl = null,
    DateTime? StartsAt = null,
    DateTime? EndsAt = null,
    string? TargetAudience = null
) : ICommand<FeaturedContent>;

/// <summary>
/// Command to update featured content
/// </summary>
public record UpdateFeaturedContentCommand(
    Guid Id,
    string? Title = null,
    string? Subtitle = null,
    string? ImageUrl = null,
    string? LinkUrl = null,
    int? DisplayOrder = null,
    DateTime? StartsAt = null,
    DateTime? EndsAt = null,
    bool? IsActive = null,
    string? TargetAudience = null
) : ICommand<FeaturedContent?>;

/// <summary>
/// Command to delete featured content
/// </summary>
public record DeleteFeaturedContentCommand(Guid Id) : ICommand<bool>;

/// <summary>
/// Command to activate/deactivate featured content
/// </summary>
public record ToggleFeaturedContentCommand(Guid Id, bool IsActive) : ICommand<FeaturedContent?>;

// ===== COURSE COLLECTION COMMANDS =====

/// <summary>
/// Command to create a course collection
/// </summary>
public record CreateCourseCollectionCommand(
    Guid CuratorId,
    string Title,
    CollectionType Type = CollectionType.Curated,
    Guid? TenantId = null,
    string? Description = null,
    string? ImageUrl = null
) : ICommand<CourseCollection>;

/// <summary>
/// Command to update a course collection
/// </summary>
public record UpdateCourseCollectionCommand(
    Guid Id,
    string? Title = null,
    string? Description = null,
    string? ImageUrl = null,
    bool? IsFeatured = null
) : ICommand<CourseCollection?>;

/// <summary>
/// Command to publish a course collection
/// </summary>
public record PublishCourseCollectionCommand(Guid Id) : ICommand<CourseCollection?>;

/// <summary>
/// Command to unpublish a course collection
/// </summary>
public record UnpublishCourseCollectionCommand(Guid Id) : ICommand<CourseCollection?>;

/// <summary>
/// Command to delete a course collection
/// </summary>
public record DeleteCourseCollectionCommand(Guid Id) : ICommand<bool>;

// ===== SEARCH HISTORY COMMANDS =====

/// <summary>
/// Command to record a search
/// </summary>
public record RecordSearchCommand(
    string Query,
    int ResultCount,
    Guid? UserId = null,
    string? Filters = null
) : ICommand<SearchHistory>;

/// <summary>
/// Command to record a click from search results
/// </summary>
public record RecordSearchClickCommand(
    Guid SearchId,
    Guid ClickedCourseId
) : ICommand<bool>;
