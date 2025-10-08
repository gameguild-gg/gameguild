namespace GameGuild.Authorization.Middleware;

/// <summary> Extension methods for adding context middleware </summary>
public static class ContextMiddlewareExtensions
{
    /// <summary> Adds context middleware to the application pipeline </summary>
    public static IApplicationBuilder UseContextMiddleware(this IApplicationBuilder builder)
    {
        // Context middleware removed as part of simplification
        return builder;
    }
}
