using Microsoft.AspNetCore.Builder;

namespace GameGuild.Identity.Authentication;

/// <summary>
///     Extension methods for adding token revocation middleware to the application pipeline.
/// </summary>
public static class TokenRevocationMiddlewareExtensions
{
    /// <summary>
    ///     Adds the token revocation middleware to validate JWT tokens against the revocation list.
    ///     Should be called AFTER UseAuthentication() and BEFORE UseAuthorization().
    /// </summary>
    /// <param name="app">The application builder</param>
    /// <returns>The application builder for chaining</returns>
    /// <example>
    /// <code>
    /// app.UseAuthentication();
    /// app.UseTokenRevocation(); // Add after authentication
    /// app.UseAuthorization();
    /// </code>
    /// </example>
    public static IApplicationBuilder UseTokenRevocation(this IApplicationBuilder app)
    {
        return app.UseMiddleware<TokenRevocationMiddleware>();
    }
}
