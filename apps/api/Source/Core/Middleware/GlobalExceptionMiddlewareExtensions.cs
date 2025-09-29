namespace GameGuild.Core.Middleware;

/// <summary>
/// Extension methods for registering the global exception middleware.
/// </summary>
public static class GlobalExceptionMiddlewareExtensions {
    /// <summary>
    /// Adds the global exception handling middleware to the application pipeline.
    /// This should be added early in the pipeline to catch all unhandled exceptions.
    /// </summary>
    /// <param name="app">The application builder</param>
    /// <returns>The application builder</returns>
    public static IApplicationBuilder UseGlobalExceptionHandling(this IApplicationBuilder app) {
        return app.UseMiddleware<GlobalExceptionMiddleware>();
    }
}