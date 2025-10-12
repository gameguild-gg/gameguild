namespace GameGuild.Core.Middleware;

/// <summary>
/// Extension methods for configuring the request logging middleware.
/// </summary>
public static class RequestLoggingMiddlewareExtensions
{
    /// <summary>
    /// Adds request logging middleware to the application pipeline.
    /// Should be added after correlation ID middleware but before other business logic middleware.
    /// </summary>
    /// <param name="app">The application builder</param>
    /// <param name="configureOptions">Optional configuration for logging options</param>
    /// <returns>The application builder for chaining</returns>
    public static IApplicationBuilder UseRequestLogging(this IApplicationBuilder app, Action<RequestLoggingOptions>? configureOptions = null)
    {
        var options = new RequestLoggingOptions();
        configureOptions?.Invoke(options);

        return app.UseMiddleware<RequestLoggingMiddleware>(options);
    }
}
