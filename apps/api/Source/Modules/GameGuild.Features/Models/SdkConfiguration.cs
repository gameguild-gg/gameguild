namespace GameGuild.Features;

/// <summary>
///     Configuration for the feature flag SDK
/// </summary>
public class SdkConfiguration
{
    /// <summary>
    ///     The API key for authentication
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    ///     The base URL for the feature flag service
    /// </summary>
    public string BaseUrl { get; set; } = string.Empty;

    /// <summary>
    ///     The environment name (e.g., development, staging, production)
    /// </summary>
    public string Environment { get; set; } = "production";

    /// <summary>
    ///     Timeout for HTTP requests in seconds
    /// </summary>
    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>
    ///     How often to poll for feature flag updates in seconds
    /// </summary>
    public int PollingIntervalSeconds { get; set; } = 60;

    /// <summary>
    ///     Whether to enable local caching
    /// </summary>
    public bool EnableCaching { get; set; } = true;

    /// <summary>
    ///     Cache expiration time in minutes
    /// </summary>
    public int CacheExpirationMinutes { get; set; } = 5;

    /// <summary>
    ///     Whether to enable analytics tracking
    /// </summary>
    public bool EnableAnalytics { get; set; } = true;

    /// <summary>
    ///     Whether to enable debug logging
    /// </summary>
    public bool EnableDebugLogging { get; set; } = false;

    /// <summary>
    ///     Configuration version
    /// </summary>
    public string Version { get; set; } = "1.0.0";

    /// <summary>
    ///     When the configuration was generated
    /// </summary>
    public DateTimeOffset GeneratedAt { get; set; } = DateTimeOffset.UtcNow;
}
