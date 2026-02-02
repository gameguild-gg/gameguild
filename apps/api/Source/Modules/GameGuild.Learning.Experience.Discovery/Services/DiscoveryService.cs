using GameGuild.CQRS;

namespace GameGuild.Learning.Experience.Discovery;

/// <summary>
/// Service implementation for Discovery operations
/// </summary>
public class DiscoveryService(IMediator mediator) : IDiscoveryService
{
    // ===== FEATURED CONTENT =====

    public async Task<FeaturedContent?> GetFeaturedContentByIdAsync(Guid id)
    {
        return await mediator.Send(new GetFeaturedContentByIdQuery(id));
    }

    public async Task<IEnumerable<FeaturedContent>> GetActiveFeaturedContentAsync(Guid? tenantId = null, int skip = 0, int take = 50)
    {
        return await mediator.Send(new GetActiveFeaturedContentQuery(tenantId, skip, take));
    }

    public async Task<IEnumerable<FeaturedContent>> GetFeaturedContentByTypeAsync(FeaturedContentType type, Guid? tenantId = null, int skip = 0, int take = 50)
    {
        return await mediator.Send(new GetFeaturedContentByTypeQuery(type, tenantId, skip, take));
    }

    public async Task<FeaturedContent> CreateFeaturedContentAsync(CreateFeaturedContentDto dto, Guid? tenantId = null)
    {
        return await mediator.Send(new CreateFeaturedContentCommand(
            Type: dto.Type,
            Title: dto.Title,
            DisplayOrder: dto.DisplayOrder,
            CourseId: dto.CourseId,
            LearningPathId: dto.LearningPathId,
            TenantId: tenantId,
            Subtitle: dto.Subtitle,
            ImageUrl: dto.ImageUrl,
            LinkUrl: dto.LinkUrl,
            StartsAt: dto.StartsAt,
            EndsAt: dto.EndsAt,
            TargetAudience: dto.TargetAudience
        ));
    }

    public async Task<FeaturedContent?> UpdateFeaturedContentAsync(Guid id, UpdateFeaturedContentDto dto)
    {
        return await mediator.Send(new UpdateFeaturedContentCommand(
            Id: id,
            Title: dto.Title,
            Subtitle: dto.Subtitle,
            ImageUrl: dto.ImageUrl,
            LinkUrl: dto.LinkUrl,
            DisplayOrder: dto.DisplayOrder,
            StartsAt: dto.StartsAt,
            EndsAt: dto.EndsAt,
            IsActive: dto.IsActive,
            TargetAudience: dto.TargetAudience
        ));
    }

    public async Task<bool> DeleteFeaturedContentAsync(Guid id)
    {
        return await mediator.Send(new DeleteFeaturedContentCommand(id));
    }

    public async Task<FeaturedContent?> ToggleFeaturedContentAsync(Guid id, bool isActive)
    {
        return await mediator.Send(new ToggleFeaturedContentCommand(id, isActive));
    }

    // ===== COURSE COLLECTIONS =====

    public async Task<CourseCollection?> GetCollectionByIdAsync(Guid id)
    {
        return await mediator.Send(new GetCollectionByIdQuery(id));
    }

    public async Task<CourseCollection?> GetCollectionBySlugAsync(string slug, Guid? tenantId = null)
    {
        return await mediator.Send(new GetCollectionBySlugQuery(slug, tenantId));
    }

    public async Task<IEnumerable<CourseCollection>> GetPublishedCollectionsAsync(Guid? tenantId = null, CollectionType? type = null, int skip = 0, int take = 50)
    {
        return await mediator.Send(new GetPublishedCollectionsQuery(tenantId, type, skip, take));
    }

    public async Task<IEnumerable<CourseCollection>> GetFeaturedCollectionsAsync(Guid? tenantId = null, int take = 10)
    {
        return await mediator.Send(new GetFeaturedCollectionsQuery(tenantId, take));
    }

    public async Task<IEnumerable<CourseCollection>> GetCollectionsByCuratorAsync(Guid curatorId, bool includeUnpublished = false, int skip = 0, int take = 50)
    {
        return await mediator.Send(new GetCollectionsByCuratorQuery(curatorId, includeUnpublished, skip, take));
    }

    public async Task<CourseCollection> CreateCollectionAsync(CreateCourseCollectionDto dto, Guid curatorId, Guid? tenantId = null)
    {
        return await mediator.Send(new CreateCourseCollectionCommand(
            CuratorId: curatorId,
            Title: dto.Title,
            Type: dto.Type,
            TenantId: tenantId,
            Description: dto.Description,
            ImageUrl: dto.ImageUrl
        ));
    }

    public async Task<CourseCollection?> UpdateCollectionAsync(Guid id, UpdateCourseCollectionDto dto)
    {
        return await mediator.Send(new UpdateCourseCollectionCommand(
            Id: id,
            Title: dto.Title,
            Description: dto.Description,
            ImageUrl: dto.ImageUrl,
            IsFeatured: dto.IsFeatured
        ));
    }

    public async Task<CourseCollection?> PublishCollectionAsync(Guid id)
    {
        return await mediator.Send(new PublishCourseCollectionCommand(id));
    }

    public async Task<CourseCollection?> UnpublishCollectionAsync(Guid id)
    {
        return await mediator.Send(new UnpublishCourseCollectionCommand(id));
    }

    public async Task<bool> DeleteCollectionAsync(Guid id)
    {
        return await mediator.Send(new DeleteCourseCollectionCommand(id));
    }

    // ===== SEARCH ANALYTICS =====

    public async Task<SearchHistory> RecordSearchAsync(RecordSearchDto dto, Guid? userId = null)
    {
        return await mediator.Send(new RecordSearchCommand(
            Query: dto.Query,
            ResultCount: dto.ResultCount,
            UserId: userId,
            Filters: dto.Filters
        ));
    }

    public async Task<bool> RecordSearchClickAsync(Guid searchId, Guid clickedCourseId)
    {
        return await mediator.Send(new RecordSearchClickCommand(searchId, clickedCourseId));
    }

    public async Task<IEnumerable<SearchHistory>> GetUserSearchHistoryAsync(Guid userId, int take = 20)
    {
        return await mediator.Send(new GetUserSearchHistoryQuery(userId, take));
    }

    public async Task<IEnumerable<PopularSearchResult>> GetPopularSearchesAsync(int daysBack = 30, int take = 20)
    {
        return await mediator.Send(new GetPopularSearchesQuery(daysBack, take));
    }
}
