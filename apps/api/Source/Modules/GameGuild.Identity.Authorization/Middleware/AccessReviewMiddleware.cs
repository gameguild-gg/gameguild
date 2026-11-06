using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace GameGuild.Identity.Authorization;

/// <summary>
///     Middleware for access review compliance tracking.
/// </summary>
/// <remarks>
///     <para>
///         DEPRECATED (issue #1203): this class was a header-writing stub — it appended
///         <c>X-Access-Review: enabled</c> and did nothing else. The compliance-tracking
///         and review-triggering responsibilities described below belong to the
///         access-review services and campaign workflows, not to a per-request
///         middleware, so no minimal in-place implementation exists. It has been
///         unregistered from the pipeline; see docs/authorization/gaps.md §C1.
///     </para>
/// </remarks>
[Obsolete("Header-writing stub with no behavior; unregistered from the pipeline (issue #1203, docs/authorization/gaps.md §C1). Access-review compliance tracking is owned by the access-review services.")]
public sealed class AccessReviewMiddleware(
    RequestDelegate next,
    ILogger<AccessReviewMiddleware> logger
)
{
    /// <summary>
    ///     Invokes the access review middleware.
    /// </summary>
    public async Task InvokeAsync(HttpContext context)
    {
        logger.LogTrace("Access review middleware processing request for {Path}", LogRedaction.Sanitize(context.Request.Path));
        
        // Add access review headers for diagnostics
        context.Response.Headers.Append("X-Access-Review", "enabled");

        await next(context).ConfigureAwait(false);
    }
}
