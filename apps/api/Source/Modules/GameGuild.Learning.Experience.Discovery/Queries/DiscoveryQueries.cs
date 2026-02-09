using GameGuild.CQRS;

namespace GameGuild.Learning.Experience.Discovery;

// ===== FEATURED CONTENT QUERIES =====

/// <summary>
/// Query to get all active featured content for a tenant
/// </summary>
public sealed record GetActiveFeaturedContentQuery(
    Guid? TenantId = null,
    int Skip = 0,
    int Take = 50
) : IQuery<IEnumerable<FeaturedContent>>;

/// <summary>
/// Query to get featured content by type
/// </summary>
public sealed record GetFeaturedContentByTypeQuery(
    FeaturedContentType Type,
    Guid? TenantId = null,
    int Skip = 0,
    int Take = 50
) : IQuery<IEnumerable<FeaturedContent>>;

/// <summary>
/// Query to get featured content by ID
/// </summary>
public sealed record GetFeaturedContentByIdQuery(Guid Id) : IQuery<FeaturedContent?>;

/// <summary>
/// Query to get all featured content (admin view)
/// </summary>
public sealed record GetAllFeaturedContentQuery(
    Guid? TenantId = null,
    bool IncludeInactive = false,
    int Skip = 0,
    int Take = 50
) : IQuery<IEnumerable<FeaturedContent>>;

// ===== COURSE COLLECTION QUERIES =====

/// <summary>
/// Query to get published course collections
/// </summary>
public sealed record GetPublishedCollectionsQuery(
    Guid? TenantId = null,
    CollectionType? Type = null,
    int Skip = 0,
    int Take = 50
) : IQuery<IEnumerable<CourseCollection>>;

/// <summary>
/// Query to get a course collection by slug
/// </summary>
public sealed record GetCollectionBySlugQuery(string Slug, Guid? TenantId = null) : IQuery<CourseCollection?>;

/// <summary>
/// Query to get a course collection by ID
/// </summary>
public sealed record GetCollectionByIdQuery(Guid Id) : IQuery<CourseCollection?>;

/// <summary>
/// Query to get featured collections
/// </summary>
public sealed record GetFeaturedCollectionsQuery(
    Guid? TenantId = null,
    int Take = 10
) : IQuery<IEnumerable<CourseCollection>>;

/// <summary>
/// Query to get collections curated by a specific user
/// </summary>
public sealed record GetCollectionsByCuratorQuery(
    Guid CuratorId,
    bool IncludeUnpublished = false,
    int Skip = 0,
    int Take = 50
) : IQuery<IEnumerable<CourseCollection>>;

/// <summary>
/// Query to get all collections (admin view)
/// </summary>
public sealed record GetAllCollectionsQuery(
    Guid? TenantId = null,
    bool IncludeUnpublished = true,
    int Skip = 0,
    int Take = 50
) : IQuery<IEnumerable<CourseCollection>>;

// ===== SEARCH QUERIES =====

/// <summary>
/// Query to get search history for a user
/// </summary>
public sealed record GetUserSearchHistoryQuery(
    Guid UserId,
    int Take = 20
) : IQuery<IEnumerable<SearchHistory>>;

/// <summary>
/// Query to get popular searches (analytics)
/// </summary>
public sealed record GetPopularSearchesQuery(
    int DaysBack = 30,
    int Take = 20
) : IQuery<IEnumerable<PopularSearchResult>>;

/// <summary>
/// Result for popular searches query
/// </summary>
public sealed record PopularSearchResult(
    string Query,
    int SearchCount,
    int TotalClicks,
    double ClickThroughRate);
