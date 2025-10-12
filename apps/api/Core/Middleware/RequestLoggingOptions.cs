namespace GameGuild.Core.Middleware;

/// <summary>
/// Configuration options for request logging middleware.
/// </summary>
public class RequestLoggingOptions
{
    /// <summary>
    /// Whether to log request headers (sensitive headers are automatically filtered).
    /// </summary>
    public bool LogRequestHeaders { get; set; } = false;

    /// <summary>
    /// Whether to log response headers.
    /// </summary>
    public bool LogResponseHeaders { get; set; } = false;

    /// <summary>
    /// Whether to log request bodies for appropriate content types.
    /// </summary>
    public bool LogRequestBody { get; set; } = false;

    /// <summary>
    /// Maximum length of request/response bodies to log before truncating.
    /// </summary>
    public int MaxBodyLength { get; set; } = 4096;

    /// <summary>
    /// Threshold in milliseconds for considering a request as slow.
    /// </summary>
    public double SlowRequestThresholdMs { get; set; } = 2000;

    /// <summary>
    /// Paths to skip logging (e.g., health check endpoints).
    /// </summary>
    public List<string> SkipPaths { get; set; } = ["/health", "/ping", "/favicon.ico", "/_framework", "/swagger"];
}
