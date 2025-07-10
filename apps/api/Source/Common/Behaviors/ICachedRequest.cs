namespace GameGuild.Common;

/// <summary>
/// Interface for requests that support caching
/// </summary>
public interface ICachedRequest {
  /// <summary>
  /// Unique cache key for this request
  /// </summary>
  string CacheKey { get; }

  /// <summary>
  /// Absolute cache expiration time
  /// </summary>
  TimeSpan CacheExpiration { get; }

  /// <summary>
  /// Sliding expiration (optional)
  /// </summary>
  TimeSpan? SlidingExpiration { get; }
}
