namespace GameGuild.Modules.Features.Models;

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
}

/// <summary>
///     SDK endpoint configuration
/// </summary>
public class SdkEndpoints
{
    /// <summary>
    ///     Endpoint for fetching feature flags
    /// </summary>
    public string Features { get; set; } = "/api/features";

    /// <summary>
    ///     Endpoint for evaluating feature flags
    /// </summary>
    public string Evaluate { get; set; } = "/api/features/evaluate";

    /// <summary>
    ///     Endpoint for submitting analytics
    /// </summary>
    public string Analytics { get; set; } = "/api/features/analytics";

    /// <summary>
    ///     Endpoint for health checks
    /// </summary>
    public string Health { get; set; } = "/api/health";

    /// <summary>
    ///     Endpoint for SDK configuration
    /// </summary>
    public string Config { get; set; } = "/api/sdk/config";
}

