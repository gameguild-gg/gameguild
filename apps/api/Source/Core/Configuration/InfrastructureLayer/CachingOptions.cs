namespace GameGuild;

/// <summary> Configuration options for caching services. </summary>
public class CachingOptions {
  /// <summary> Enables in-memory caching. </summary>
  public bool EnableMemoryCache { get; set; } = true;

  /// <summary> Enables distributed caching (Redis). </summary>
  public bool EnableDistributedCache { get; set; } = false;

  /// <summary> Redis connection string for distributed caching. </summary>
  public string? RedisConnectionString { get; set; }

  /// <summary> Default cache expiration time in minutes. </summary>
  public int DefaultExpirationMinutes { get; set; } = 30;

  /// <summary> Validates the caching options. </summary>
  public void Validate() {
    if (EnableDistributedCache && string.IsNullOrWhiteSpace(RedisConnectionString)) { throw new InvalidOperationException("Redis connection string is required when distributed cache is enabled."); }

    if (DefaultExpirationMinutes <= 0) { throw new ArgumentException("Default expiration minutes must be greater than 0."); }
  }
}
