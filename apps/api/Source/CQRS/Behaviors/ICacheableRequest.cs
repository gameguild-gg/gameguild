namespace GameGuild.CQRS;

/// <summary>
/// Marker interface for cacheable requests
/// </summary>
public interface ICacheableRequest
{
    /// <summary>
    /// Gets the cache key for this request
    /// </summary>
    string CacheKey { get; }

    /// <summary>
    /// Gets the cache expiration time
    /// </summary>
    TimeSpan CacheExpiration { get; }
}
