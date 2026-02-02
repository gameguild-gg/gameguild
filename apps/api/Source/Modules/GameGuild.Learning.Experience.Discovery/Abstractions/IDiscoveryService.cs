namespace GameGuild.Learning.Experience.Discovery;

/// <summary>
/// Service interface for Discovery operations
/// </summary>
public interface IDiscoveryService
{
    // Featured Content
    Task<FeaturedContent?> GetFeaturedContentByIdAsync(Guid id);
    Task<IEnumerable<FeaturedContent>> GetActiveFeaturedContentAsync(Guid? tenantId = null, int skip = 0, int take = 50);
    Task<IEnumerable<FeaturedContent>> GetFeaturedContentByTypeAsync(FeaturedContentType type, Guid? tenantId = null, int skip = 0, int take = 50);
    Task<FeaturedContent> CreateFeaturedContentAsync(CreateFeaturedContentDto dto, Guid? tenantId = null);
    Task<FeaturedContent?> UpdateFeaturedContentAsync(Guid id, UpdateFeaturedContentDto dto);
    Task<bool> DeleteFeaturedContentAsync(Guid id);
    Task<FeaturedContent?> ToggleFeaturedContentAsync(Guid id, bool isActive);

    // Course Collections
    Task<CourseCollection?> GetCollectionByIdAsync(Guid id);
    Task<CourseCollection?> GetCollectionBySlugAsync(string slug, Guid? tenantId = null);
    Task<IEnumerable<CourseCollection>> GetPublishedCollectionsAsync(Guid? tenantId = null, CollectionType? type = null, int skip = 0, int take = 50);
    Task<IEnumerable<CourseCollection>> GetFeaturedCollectionsAsync(Guid? tenantId = null, int take = 10);
    Task<IEnumerable<CourseCollection>> GetCollectionsByCuratorAsync(Guid curatorId, bool includeUnpublished = false, int skip = 0, int take = 50);
    Task<CourseCollection> CreateCollectionAsync(CreateCourseCollectionDto dto, Guid curatorId, Guid? tenantId = null);
    Task<CourseCollection?> UpdateCollectionAsync(Guid id, UpdateCourseCollectionDto dto);
    Task<CourseCollection?> PublishCollectionAsync(Guid id);
    Task<CourseCollection?> UnpublishCollectionAsync(Guid id);
    Task<bool> DeleteCollectionAsync(Guid id);

    // Search Analytics
    Task<SearchHistory> RecordSearchAsync(RecordSearchDto dto, Guid? userId = null);
    Task<bool> RecordSearchClickAsync(Guid searchId, Guid clickedCourseId);
    Task<IEnumerable<SearchHistory>> GetUserSearchHistoryAsync(Guid userId, int take = 20);
    Task<IEnumerable<PopularSearchResult>> GetPopularSearchesAsync(int daysBack = 30, int take = 20);
}
