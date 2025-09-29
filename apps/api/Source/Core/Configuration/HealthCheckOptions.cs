namespace GameGuild.Core.Configuration;

/// <summary>
/// Configuration options for health checks
/// </summary>
public class HealthCheckOptions
{
    /// <summary>
    /// Enable database health check
    /// </summary>
    public bool EnableDatabaseCheck { get; set; } = true;

    /// <summary>
    /// Enable Redis cache health check
    /// </summary>
    public bool EnableRedisCheck { get; set; } = true;

    /// <summary>
    /// Redis connection string
    /// </summary>
    public string? RedisConnectionString { get; set; }

    /// <summary>
    /// Enable payment provider health checks
    /// </summary>
    public bool EnablePaymentProviderChecks { get; set; } = true;

    /// <summary>
    /// Enable KYC provider health checks
    /// </summary>
    public bool EnableKycProviderChecks { get; set; } = true;

    /// <summary>
    /// Enable memory health check
    /// </summary>
    public bool EnableMemoryCheck { get; set; } = true;

    /// <summary>
    /// Memory threshold in MB
    /// </summary>
    public long MemoryThresholdMb { get; set; } = 1024;

    /// <summary>
    /// Enable disk space health check
    /// </summary>
    public bool EnableDiskSpaceCheck { get; set; } = true;

    /// <summary>
    /// Disk space threshold in GB
    /// </summary>
    public long DiskSpaceThresholdGb { get; set; } = 10;

    /// <summary>
    /// Validates the options
    /// </summary>
    public void Validate()
    {
        if (EnableRedisCheck && string.IsNullOrEmpty(RedisConnectionString)) { throw new InvalidOperationException("RedisConnectionString must be specified when EnableRedisCheck is true"); }

        if (MemoryThresholdMb <= 0) { throw new ArgumentException("MemoryThresholdMB must be positive", nameof(MemoryThresholdMb)); }

        if (DiskSpaceThresholdGb <= 0) { throw new ArgumentException("DiskSpaceThresholdGB must be positive", nameof(DiskSpaceThresholdGb)); }
    }
}