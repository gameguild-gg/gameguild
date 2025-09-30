namespace GameGuild.Core.Middleware;

/// <summary>
/// Extension methods for registering the correlation ID middleware.
/// </summary>
public static class CorrelationIdMiddlewareExtensions
{
    /// <summary>
    /// Adds the correlation ID middleware to the application pipeline.
    /// This should be added early in the pipeline to ensure correlation IDs are available for all requests.
    /// </summary>
    /// <param name="app">The application builder</param>
    /// <returns>The application builder</returns>
    public static IApplicationBuilder UseCorrelationId(this IApplicationBuilder app) { return app.UseMiddleware<CorrelationIdMiddleware>(); }
}
