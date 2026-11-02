using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace GameGuild.Identity.Authorization;

/// <summary>
///     Middleware for permission caching optimization.
/// </summary>
/// <remarks>
///     <para>
///         DEPRECATED (issue #1203): this class was a header-writing stub — it appended
///         <c>X-Permission-Cache: enabled</c> and did nothing else. Permission caching is
///         performed by the permission resolution services (memory-cache backed, with
///         explicit invalidation via ClearPermissionCacheCommand), so no per-request
///         middleware responsibility remains to implement. It has been unregistered from
///         the pipeline; see docs/authorization/gaps.md §C2.
///     </para>
/// </remarks>
[Obsolete("Header-writing stub with no behavior; unregistered from the pipeline (issue #1203, docs/authorization/gaps.md §C2). Permission caching is owned by the permission resolution services.")]
public sealed class PermissionCachingMiddleware(
    RequestDelegate next,
    ILogger<PermissionCachingMiddleware> logger
)
{
    /// <summary>
    ///     Invokes the permission caching middleware.
    /// </summary>
    public async Task InvokeAsync(HttpContext context)
    {
        logger.LogTrace("Permission caching middleware invoked for {Path}", LogRedaction.Sanitize(context.Request.Path));
        
        // Add permission caching headers for diagnostics
        context.Response.Headers.Append("X-Permission-Cache", "enabled");

        await next(context).ConfigureAwait(false);
    }
}
