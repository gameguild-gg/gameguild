namespace GameGuild.SharedKernel.Configuration;

/// <summary>
///     Configuration options for caching services
/// </summary>
public class CachingOptions : BaseOptions
{
    /// <summary>
    ///     Enables in-memory caching
    /// </summary>
    public bool EnableMemoryCache { get; set; } = true;

    /// <summary>
    ///     Enables distributed caching (Redis)
    /// </summary>
    public bool EnableDistributedCache { get; set; } = false;

    /// <summary>
    ///     Redis connection string for distributed caching
    /// </summary>
    public string? RedisConnectionString { get; set; }

    /// <summary>
    ///     Default cache expiration time in minutes
    /// </summary>
    public int DefaultExpirationMinutes { get; set; } = 30;

    /// <summary>
    ///     Cache key prefix to avoid collisions
    /// </summary>
    public string KeyPrefix { get; set; } = "gameguild";

    public override void Validate()
    {
        base.Validate();

        if (EnableDistributedCache && string.IsNullOrWhiteSpace(RedisConnectionString)) throw new InvalidOperationException("Redis connection string is required when distributed cache is enabled.");

        if (DefaultExpirationMinutes <= 0) throw new ArgumentException("Default expiration minutes must be greater than 0.");

        if (string.IsNullOrWhiteSpace(KeyPrefix)) throw new ArgumentException("Cache key prefix cannot be empty.");
    }

    /// <summary>
    ///     Creates default caching options.
    /// </summary>
    public static CachingOptions CreateDefault() { return new CachingOptions(); }
}
