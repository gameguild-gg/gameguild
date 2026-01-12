using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace GameGuild.Identity.Authorization;

/// <summary>
///     Resolves the tenant from the current request.
/// </summary>
public interface IAuthorizationTenantResolver
{
    /// <summary>
    ///     Resolves the tenant ID from the HTTP context.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <returns>The resolved tenant ID, or null if not resolvable.</returns>
    string? ResolveFromRequest(HttpContext context);

    /// <summary>
    ///     Resolves the tenant ID from user claims.
    /// </summary>
    /// <param name="principal">The claims principal.</param>
    /// <returns>The resolved tenant ID, or null if not found.</returns>
    string? ResolveFromClaims(ClaimsPrincipal principal);

    /// <summary>
    ///     Gets the user's default tenant from claims.
    /// </summary>
    /// <param name="principal">The claims principal.</param>
    /// <returns>The user's default tenant ID, or null if not found.</returns>
    string? GetUserDefaultTenant(ClaimsPrincipal principal);

    /// <summary>
    ///     Asynchronously resolves the tenant ID from the HTTP context.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The resolved tenant ID, or null if not resolvable.</returns>
    Task<string?> ResolveTenantIdAsync(HttpContext context, CancellationToken cancellationToken = default)
    {
        // Default implementation delegates to sync method
        return Task.FromResult(ResolveFromRequest(context));
    }
}
