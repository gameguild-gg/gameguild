using GameGuild.CQRS;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GameGuild.Learning.Experience.Discovery;

/// <summary>
/// Query handlers for Discovery module
/// </summary>
public sealed class DiscoveryQueryHandlers(IApplicationDbContext context, ILogger<DiscoveryQueryHandlers> logger)
    : IRequestHandler<GetActiveFeaturedContentQuery, IEnumerable<FeaturedContent>>,
      IRequestHandler<GetFeaturedContentByTypeQuery, IEnumerable<FeaturedContent>>,
      IRequestHandler<GetFeaturedContentByIdQuery, FeaturedContent?>,
      IRequestHandler<GetAllFeaturedContentQuery, IEnumerable<FeaturedContent>>,
      IRequestHandler<GetPublishedCollectionsQuery, IEnumerable<CourseCollection>>,
      IRequestHandler<GetCollectionBySlugQuery, CourseCollection?>,
      IRequestHandler<GetCollectionByIdQuery, CourseCollection?>,
      IRequestHandler<GetFeaturedCollectionsQuery, IEnumerable<CourseCollection>>,
      IRequestHandler<GetCollectionsByCuratorQuery, IEnumerable<CourseCollection>>,
      IRequestHandler<GetAllCollectionsQuery, IEnumerable<CourseCollection>>,
      IRequestHandler<GetUserSearchHistoryQuery, IEnumerable<SearchHistory>>,
      IRequestHandler<GetPopularSearchesQuery, IEnumerable<PopularSearchResult>>
{
    // ===== FEATURED CONTENT QUERY HANDLERS =====

    public async Task<IEnumerable<FeaturedContent>> Handle(GetActiveFeaturedContentQuery request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Getting active featured content for tenant: {TenantId}", request.TenantId);

        var now = DateTime.UtcNow;
        var query = context.Set<FeaturedContent>()
            .Where(fc => fc.DeletedAt == null && fc.IsActive)
            .Where(fc => !fc.StartsAt.HasValue || fc.StartsAt <= now)
            .Where(fc => !fc.EndsAt.HasValue || fc.EndsAt >= now);

        if (request.TenantId.HasValue)
        {
            query = query.Where(fc => fc.TenantId == request.TenantId || fc.TenantId == null);
        }

        var result = await query
            .OrderBy(fc => fc.DisplayOrder)
            .Skip(request.Skip)
            .Take(request.Take)
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        logger.LogInformation("Found {Count} active featured content items", result.Count);
        return result;
    }

    public async Task<IEnumerable<FeaturedContent>> Handle(GetFeaturedContentByTypeQuery request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Getting featured content by type: {Type}", request.Type);

        var now = DateTime.UtcNow;
        var query = context.Set<FeaturedContent>()
            .Where(fc => fc.DeletedAt == null && fc.IsActive && fc.Type == request.Type)
            .Where(fc => !fc.StartsAt.HasValue || fc.StartsAt <= now)
            .Where(fc => !fc.EndsAt.HasValue || fc.EndsAt >= now);

        if (request.TenantId.HasValue)
        {
            query = query.Where(fc => fc.TenantId == request.TenantId || fc.TenantId == null);
        }

        var result = await query
            .OrderBy(fc => fc.DisplayOrder)
            .Skip(request.Skip)
            .Take(request.Take)
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        logger.LogInformation("Found {Count} featured content items of type {Type}", result.Count, request.Type);
        return result;
    }

    public async Task<FeaturedContent?> Handle(GetFeaturedContentByIdQuery request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Getting featured content by ID: {Id}", request.Id);

        return await context.Set<FeaturedContent>()
            .Where(fc => fc.DeletedAt == null)
            .FirstOrDefaultAsync(fc => fc.Id == request.Id, cancellationToken).ConfigureAwait(false);
    }

    public async Task<IEnumerable<FeaturedContent>> Handle(GetAllFeaturedContentQuery request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Getting all featured content (admin)");

        var query = context.Set<FeaturedContent>()
            .Where(fc => fc.DeletedAt == null);

        if (!request.IncludeInactive)
        {
            query = query.Where(fc => fc.IsActive);
        }

        if (request.TenantId.HasValue)
        {
            query = query.Where(fc => fc.TenantId == request.TenantId);
        }

        return await query
            .OrderBy(fc => fc.Type)
            .ThenBy(fc => fc.DisplayOrder)
            .Skip(request.Skip)
            .Take(request.Take)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    // ===== COURSE COLLECTION QUERY HANDLERS =====

    public async Task<IEnumerable<CourseCollection>> Handle(GetPublishedCollectionsQuery request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Getting published collections");

        var query = context.Set<CourseCollection>()
            .Where(cc => cc.DeletedAt == null && cc.IsPublished);

        if (request.TenantId.HasValue)
        {
            query = query.Where(cc => cc.TenantId == request.TenantId || cc.TenantId == null);
        }

        if (request.Type.HasValue)
        {
            query = query.Where(cc => cc.Type == request.Type.Value);
        }

        var result = await query
            .OrderByDescending(cc => cc.IsFeatured)
            .ThenByDescending(cc => cc.CreatedAt)
            .Skip(request.Skip)
            .Take(request.Take)
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        logger.LogInformation("Found {Count} published collections", result.Count);
        return result;
    }

    public async Task<CourseCollection?> Handle(GetCollectionBySlugQuery request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Getting collection by slug: {Slug}", request.Slug);

        var query = context.Set<CourseCollection>()
            .Where(cc => cc.DeletedAt == null && cc.Slug == request.Slug);

        if (request.TenantId.HasValue)
        {
            query = query.Where(cc => cc.TenantId == request.TenantId || cc.TenantId == null);
        }

        return await query.FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<CourseCollection?> Handle(GetCollectionByIdQuery request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Getting collection by ID: {Id}", request.Id);

        return await context.Set<CourseCollection>()
            .Where(cc => cc.DeletedAt == null)
            .FirstOrDefaultAsync(cc => cc.Id == request.Id, cancellationToken).ConfigureAwait(false);
    }

    public async Task<IEnumerable<CourseCollection>> Handle(GetFeaturedCollectionsQuery request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Getting featured collections");

        var query = context.Set<CourseCollection>()
            .Where(cc => cc.DeletedAt == null && cc.IsPublished && cc.IsFeatured);

        if (request.TenantId.HasValue)
        {
            query = query.Where(cc => cc.TenantId == request.TenantId || cc.TenantId == null);
        }

        return await query
            .OrderByDescending(cc => cc.CreatedAt)
            .Take(request.Take)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<IEnumerable<CourseCollection>> Handle(GetCollectionsByCuratorQuery request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Getting collections by curator: {CuratorId}", request.CuratorId);

        var query = context.Set<CourseCollection>()
            .Where(cc => cc.DeletedAt == null && cc.CuratorId == request.CuratorId);

        if (!request.IncludeUnpublished)
        {
            query = query.Where(cc => cc.IsPublished);
        }

        return await query
            .OrderByDescending(cc => cc.CreatedAt)
            .Skip(request.Skip)
            .Take(request.Take)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<IEnumerable<CourseCollection>> Handle(GetAllCollectionsQuery request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Getting all collections (admin)");

        var query = context.Set<CourseCollection>()
            .Where(cc => cc.DeletedAt == null);

        if (!request.IncludeUnpublished)
        {
            query = query.Where(cc => cc.IsPublished);
        }

        if (request.TenantId.HasValue)
        {
            query = query.Where(cc => cc.TenantId == request.TenantId);
        }

        return await query
            .OrderByDescending(cc => cc.IsFeatured)
            .ThenByDescending(cc => cc.CreatedAt)
            .Skip(request.Skip)
            .Take(request.Take)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    // ===== SEARCH QUERY HANDLERS =====

    public async Task<IEnumerable<SearchHistory>> Handle(GetUserSearchHistoryQuery request, CancellationToken cancellationToken)
    {
        logger.LogDebug("Getting search history for user: {UserId}", request.UserId);

        return await context.Set<SearchHistory>()
            .Where(sh => sh.UserId == request.UserId)
            .OrderByDescending(sh => sh.CreatedAt)
            .Take(request.Take)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<IEnumerable<PopularSearchResult>> Handle(GetPopularSearchesQuery request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Getting popular searches for last {Days} days", request.DaysBack);

        var cutoffDate = DateTime.UtcNow.AddDays(-request.DaysBack);

        var searchGroups = await context.Set<SearchHistory>()
            .Where(sh => sh.CreatedAt >= cutoffDate)
            .GroupBy(sh => sh.Query.ToLower())
            .Select(g => new
            {
                Query = g.Key,
                SearchCount = g.Count(),
                TotalClicks = g.Count(sh => sh.ClickedCourseId.HasValue)
            })
            .OrderByDescending(x => x.SearchCount)
            .Take(request.Take)
            .ToListAsync(cancellationToken);

        return searchGroups.Select(g => new PopularSearchResult(
            Query: g.Query,
            SearchCount: g.SearchCount,
            TotalClicks: g.TotalClicks,
            ClickThroughRate: g.SearchCount > 0 ? (double)g.TotalClicks / g.SearchCount : 0
        ));
    }
}
