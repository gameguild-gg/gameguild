using GameGuild.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace GameGuild.Localization;

/// <summary>
/// Interface for batch loading localizations to prevent N+1 queries.
/// Use this service when loading multiple entities with localizations.
/// </summary>
public interface IBatchLocalizationLoader
{
    /// <summary>
    /// Preloads localizations for multiple resources in a single query.
    /// Call this before accessing entity.Localizations to prevent N+1.
    /// </summary>
    /// <param name="resourceIds">The IDs of resources to load localizations for.</param>
    /// <param name="languageId">Optional: filter to specific language.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Dictionary mapping resource ID to its localizations.</returns>
    Task<IReadOnlyDictionary<Guid, IReadOnlyList<ResourceLocalization>>> LoadLocalizationsAsync(
        IEnumerable<Guid> resourceIds,
        Guid? languageId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Preloads localizations for a single field across multiple resources.
    /// Optimized for scenarios like loading translated titles for a list of items.
    /// </summary>
    /// <param name="resourceIds">The IDs of resources to load localizations for.</param>
    /// <param name="fieldName">The field to load (e.g., "Title").</param>
    /// <param name="languageId">Optional: filter to specific language.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<IReadOnlyDictionary<Guid, ResourceLocalization?>> LoadFieldLocalizationsAsync(
        IEnumerable<Guid> resourceIds,
        string fieldName,
        Guid? languageId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets localized content for a field, with fallback to default value.
    /// </summary>
    /// <param name="resourceId">The resource ID.</param>
    /// <param name="fieldName">The field name.</param>
    /// <param name="languageId">The target language.</param>
    /// <param name="defaultValue">Default value if no localization exists.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<string> GetLocalizedFieldAsync(
        Guid resourceId,
        string fieldName,
        Guid languageId,
        string defaultValue,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Batch localization loader that fetches localizations for multiple resources
/// in a single database query, preventing N+1 issues.
/// </summary>
public class BatchLocalizationLoader : IBatchLocalizationLoader
{
    private readonly IApplicationDbContext _context;
    private readonly IMemoryCache _cache;
    private readonly ILogger<BatchLocalizationLoader> _logger;
    private readonly TimeSpan _cacheExpiration = TimeSpan.FromMinutes(15);
    private const string BatchCachePrefix = "loc:batch:";

    public BatchLocalizationLoader(
        IApplicationDbContext context,
        IMemoryCache cache,
        ILogger<BatchLocalizationLoader> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<IReadOnlyDictionary<Guid, IReadOnlyList<ResourceLocalization>>> LoadLocalizationsAsync(
        IEnumerable<Guid> resourceIds,
        Guid? languageId = null,
        CancellationToken cancellationToken = default)
    {
        var resourceIdList = resourceIds.Distinct().ToList();
        if (resourceIdList.Count == 0)
        {
            return new Dictionary<Guid, IReadOnlyList<ResourceLocalization>>();
        }

        _logger.LogDebug("Batch loading localizations for {Count} resources", resourceIdList.Count);

        // Check cache for already-loaded resources
        var result = new Dictionary<Guid, IReadOnlyList<ResourceLocalization>>();
        var missingIds = new List<Guid>();

        foreach (var resourceId in resourceIdList)
        {
            var cacheKey = BuildBatchCacheKey(resourceId, languageId);
            if (_cache.TryGetValue(cacheKey, out IReadOnlyList<ResourceLocalization>? cached))
            {
                result[resourceId] = cached!;
            }
            else
            {
                missingIds.Add(resourceId);
            }
        }

        if (missingIds.Count == 0)
        {
            _logger.LogDebug("All {Count} resources found in cache", resourceIdList.Count);
            return result;
        }

        // Load missing from database in a single query
        var query = _context.Set<ResourceLocalization>()
            .Where(l => missingIds.Contains(l.ResourceId) && !l.IsDeleted);

        if (languageId.HasValue)
        {
            query = query.Where(l => l.LanguageId == languageId.Value);
        }

        var dbLocalizations = await query
            .Include(l => l.Language)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        // Group by resource and cache
        var grouped = dbLocalizations
            .GroupBy(l => l.ResourceId)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<ResourceLocalization>)g.ToList());

        foreach (var resourceId in missingIds)
        {
            var localizations = grouped.TryGetValue(resourceId, out var locs)
                ? locs
                : Array.Empty<ResourceLocalization>();

            result[resourceId] = localizations;

            // Cache individual results
            var cacheKey = BuildBatchCacheKey(resourceId, languageId);
            _cache.Set(cacheKey, localizations, new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = _cacheExpiration,
                Size = localizations.Count + 1
            });
        }

        _logger.LogDebug("Loaded {DbCount} localizations from DB, {CacheCount} from cache",
            dbLocalizations.Count, resourceIdList.Count - missingIds.Count);

        return result;
    }

    public async Task<IReadOnlyDictionary<Guid, ResourceLocalization?>> LoadFieldLocalizationsAsync(
        IEnumerable<Guid> resourceIds,
        string fieldName,
        Guid? languageId = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(fieldName);

        var resourceIdList = resourceIds.Distinct().ToList();
        if (resourceIdList.Count == 0)
        {
            return new Dictionary<Guid, ResourceLocalization?>();
        }

        _logger.LogDebug("Batch loading field '{Field}' for {Count} resources", fieldName, resourceIdList.Count);

        var query = _context.Set<ResourceLocalization>()
            .Where(l => resourceIdList.Contains(l.ResourceId) && 
                       l.FieldName == fieldName && 
                       !l.IsDeleted);

        if (languageId.HasValue)
        {
            query = query.Where(l => l.LanguageId == languageId.Value);
        }

        var localizations = await query
            .Include(l => l.Language)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        // Create result dictionary with all requested IDs
        var result = resourceIdList.ToDictionary(
            id => id,
            id => localizations.FirstOrDefault(l => l.ResourceId == id));

        return result;
    }

    public async Task<string> GetLocalizedFieldAsync(
        Guid resourceId,
        string fieldName,
        Guid languageId,
        string defaultValue,
        CancellationToken cancellationToken = default)
    {
        var cacheKey = $"loc:field:{resourceId}:{fieldName}:{languageId}";

        if (_cache.TryGetValue(cacheKey, out string? cached))
        {
            return cached ?? defaultValue;
        }

        var localization = await _context.Set<ResourceLocalization>()
            .Where(l => l.ResourceId == resourceId &&
                       l.FieldName == fieldName &&
                       l.LanguageId == languageId &&
                       !l.IsDeleted)
            .Select(l => l.Content)
            .FirstOrDefaultAsync(cancellationToken);

        var result = localization ?? defaultValue;

        _cache.Set(cacheKey, result, new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = _cacheExpiration,
            Size = 1
        });

        return result;
    }

    private static string BuildBatchCacheKey(Guid resourceId, Guid? languageId)
    {
        return languageId.HasValue
            ? $"{BatchCachePrefix}{resourceId}:{languageId}"
            : $"{BatchCachePrefix}{resourceId}:all";
    }
}
