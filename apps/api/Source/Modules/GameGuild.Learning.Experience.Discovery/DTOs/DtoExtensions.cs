namespace GameGuild.Learning.Experience.Discovery;

/// <summary>
/// Extension methods for converting entities to DTOs
/// </summary>
public static class DiscoveryDtoExtensions
{
    /// <summary>
    /// Convert FeaturedContent entity to DTO
    /// </summary>
    public static FeaturedContentDto ToDto(this FeaturedContent entity) =>
        new(
            Id: entity.Id,
            CourseId: entity.CourseId,
            LearningPathId: entity.LearningPathId,
            TenantId: entity.TenantId,
            Title: entity.Title,
            Subtitle: entity.Subtitle,
            ImageUrl: entity.ImageUrl,
            LinkUrl: entity.LinkUrl,
            Type: entity.Type,
            DisplayOrder: entity.DisplayOrder,
            StartsAt: entity.StartsAt,
            EndsAt: entity.EndsAt,
            IsActive: entity.IsActive,
            TargetAudience: entity.TargetAudience,
            CreatedAt: entity.CreatedAt,
            UpdatedAt: entity.UpdatedAt);

    /// <summary>
    /// Convert CourseCollection entity to DTO
    /// </summary>
    public static CourseCollectionDto ToDto(this CourseCollection entity) =>
        new(
            Id: entity.Id,
            TenantId: entity.TenantId,
            CuratorId: entity.CuratorId,
            Title: entity.Title,
            Slug: entity.Slug,
            Description: entity.Description,
            ImageUrl: entity.ImageUrl,
            IsPublished: entity.IsPublished,
            IsFeatured: entity.IsFeatured,
            CourseCount: entity.CourseCount,
            Type: entity.Type,
            CreatedAt: entity.CreatedAt,
            UpdatedAt: entity.UpdatedAt);

    /// <summary>
    /// Convert SearchHistory entity to DTO
    /// </summary>
    public static SearchHistoryDto ToDto(this SearchHistory entity) =>
        new(
            Id: entity.Id,
            UserId: entity.UserId,
            Query: entity.Query,
            ResultCount: entity.ResultCount,
            ClickedCourseId: entity.ClickedCourseId,
            Filters: entity.Filters,
            CreatedAt: entity.CreatedAt);
}
