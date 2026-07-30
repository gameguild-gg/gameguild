namespace GameGuild.Features;

/// <summary>
///     Configuration options for feature flag system
/// </summary>
public class FeatureFlagOptions
{
    /// <summary>
    ///     Configuration section name in appsettings.json
    /// </summary>
    public const string SectionName = "FeatureFlags";

    /// <summary>
    ///     Cache time-to-live in minutes
    /// </summary>
    public int CacheTtlMinutes { get; set; } = 5;

    /// <summary>
    ///     SDK refresh interval in seconds
    /// </summary>
    public int SdkRefreshIntervalSeconds { get; set; } = 300;

    /// <summary>
    ///     Default environment for feature evaluation when not specified
    /// </summary>
    public string DefaultEnvironment { get; set; } = "production";

    /// <summary>
    ///     Enable analytics recording for feature flag evaluations
    /// </summary>
    public bool EnableAnalytics { get; set; } = true;

    /// <summary>
    ///     Enable caching for feature flag evaluations
    /// </summary>
    public bool EnableCaching { get; set; } = true;

    /// <summary>
    ///     Maximum number of features to evaluate in a single bulk request
    /// </summary>
    public int MaxBulkEvaluationSize { get; set; } = 50;

    /// <summary>
    ///     Cache time-to-live for SDK configuration in seconds
    /// </summary>
    public int SdkCacheTtlSeconds { get; set; } = 600;

    /// <summary>
    ///     Enable detailed logging for feature flag operations
    /// </summary>
    public bool EnableDetailedLogging { get; set; } = false;

    /// <summary>
    ///     Base64-encoded encryption key for sensitive feature flag values (256 bits / 32 bytes)
    /// </summary>
    public string? EncryptionKey { get; set; }
}
