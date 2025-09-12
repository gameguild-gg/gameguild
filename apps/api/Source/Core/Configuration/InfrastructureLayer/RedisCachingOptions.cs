namespace GameGuild;

/// <summary>
/// Configuration options for Redis distributed caching
/// </summary>
public class RedisCachingOptions
{
    /// <summary>
    /// Redis connection string
    /// </summary>
    public string ConnectionString { get; set; } = "localhost:6379";

    /// <summary>
    /// Redis instance name for key prefixing
    /// </summary>
    public string InstanceName { get; set; } = "GameGuild";

    /// <summary>
    /// Default cache expiration time in minutes
    /// </summary>
    public int DefaultExpirationMinutes { get; set; } = 60;

    /// <summary>
    /// Feature flag cache expiration time in minutes
    /// </summary>
    public int FeatureFlagExpirationMinutes { get; set; } = 15;

    /// <summary>
    /// User session cache expiration time in minutes
    /// </summary>
    public int UserSessionExpirationMinutes { get; set; } = 120;

    /// <summary>
    /// Enable Redis health checks
    /// </summary>
    public bool EnableHealthChecks { get; set; } = true;

    /// <summary>
    /// Connect timeout in milliseconds
    /// </summary>
    public int ConnectTimeoutMs { get; set; } = 5000;

    /// <summary>
    /// Sync timeout in milliseconds
    /// </summary>
    public int SyncTimeoutMs { get; set; } = 5000;

    /// <summary>
    /// Validates the Redis caching options
    /// </summary>
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(ConnectionString))
            throw new ArgumentException("Redis ConnectionString cannot be null or empty.", nameof(ConnectionString));

        if (string.IsNullOrWhiteSpace(InstanceName))
            throw new ArgumentException("Redis InstanceName cannot be null or empty.", nameof(InstanceName));

        if (DefaultExpirationMinutes <= 0)
            throw new ArgumentException("DefaultExpirationMinutes must be positive.", nameof(DefaultExpirationMinutes));

        if (FeatureFlagExpirationMinutes <= 0)
            throw new ArgumentException("FeatureFlagExpirationMinutes must be positive.", nameof(FeatureFlagExpirationMinutes));

        if (UserSessionExpirationMinutes <= 0)
            throw new ArgumentException("UserSessionExpirationMinutes must be positive.", nameof(UserSessionExpirationMinutes));

        if (ConnectTimeoutMs <= 0)
            throw new ArgumentException("ConnectTimeoutMs must be positive.", nameof(ConnectTimeoutMs));

        if (SyncTimeoutMs <= 0)
            throw new ArgumentException("SyncTimeoutMs must be positive.", nameof(SyncTimeoutMs));
    }
}
