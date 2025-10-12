namespace GameGuild.Core.GraphQL;

/// <summary>
/// Extension methods for adding GraphQL security middleware
/// </summary>
public static class GraphQlSecurityMiddlewareExtensions
{
    public static IApplicationBuilder UseGraphQlSecurity(this IApplicationBuilder builder) { return builder.UseMiddleware<GraphQlSecurityMiddleware>(); }
}
