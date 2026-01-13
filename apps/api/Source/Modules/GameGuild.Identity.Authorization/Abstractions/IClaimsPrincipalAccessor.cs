using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace GameGuild.Identity.Authorization;

/// <summary>
///     Provides access to the current claims principal.
///     Abstracts away the dependency on HttpContext for DIP compliance.
/// </summary>
/// <remarks>
///     <para>
///         This abstraction allows authorization services to access the current user's claims
///         without directly depending on HttpContext. This improves testability and follows
///         the Dependency Inversion Principle (DIP).
///     </para>
///     <para>
///         For HTTP contexts, use <see cref="HttpContextClaimsPrincipalAccessor"/>.
///         For testing, create a mock implementation or use <see cref="StaticClaimsPrincipalAccessor"/>.
///     </para>
/// </remarks>
public interface IClaimsPrincipalAccessor
{
    /// <summary>
    ///     Gets the current claims principal.
    /// </summary>
    /// <returns>The current claims principal, or null if not available.</returns>
    ClaimsPrincipal? ClaimsPrincipal { get; }

    /// <summary>
    ///     Gets the current user ID from claims.
    /// </summary>
    /// <returns>The user ID if available, otherwise null.</returns>
    Guid? GetUserId();

    /// <summary>
    ///     Gets the current tenant ID from claims.
    /// </summary>
    /// <returns>The tenant ID if available, otherwise null.</returns>
    Guid? GetTenantId();

    /// <summary>
    ///     Checks if the current user is authenticated.
    /// </summary>
    bool IsAuthenticated { get; }
}

/// <summary>
///     HTTP context-based implementation of <see cref="IClaimsPrincipalAccessor"/>.
/// </summary>
public sealed class HttpContextClaimsPrincipalAccessor : IClaimsPrincipalAccessor
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    /// <summary>
    ///     Initializes a new instance of <see cref="HttpContextClaimsPrincipalAccessor"/>.
    /// </summary>
    /// <param name="httpContextAccessor">The HTTP context accessor.</param>
    public HttpContextClaimsPrincipalAccessor(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
    }

    /// <inheritdoc />
    public ClaimsPrincipal? ClaimsPrincipal => _httpContextAccessor.HttpContext?.User;

    /// <inheritdoc />
    public bool IsAuthenticated => ClaimsPrincipal?.Identity?.IsAuthenticated ?? false;

    /// <inheritdoc />
    public Guid? GetUserId()
    {
        var userIdClaim = ClaimsPrincipal?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                          ?? ClaimsPrincipal?.FindFirst(AuthorizationClaims.Sub)?.Value;

        return Guid.TryParse(userIdClaim, out var userId) ? userId : null;
    }

    /// <inheritdoc />
    public Guid? GetTenantId()
    {
        var tenantIdClaim = ClaimsPrincipal?.FindFirst(AuthorizationClaims.TenantId)?.Value;
        return Guid.TryParse(tenantIdClaim, out var tenantId) ? tenantId : null;
    }
}

/// <summary>
///     Static claims principal accessor for testing or non-HTTP contexts.
/// </summary>
public sealed class StaticClaimsPrincipalAccessor : IClaimsPrincipalAccessor
{
    /// <summary>
    ///     Initializes a new instance of <see cref="StaticClaimsPrincipalAccessor"/>.
    /// </summary>
    /// <param name="claimsPrincipal">The claims principal to use.</param>
    public StaticClaimsPrincipalAccessor(ClaimsPrincipal? claimsPrincipal = null)
    {
        ClaimsPrincipal = claimsPrincipal;
    }

    /// <inheritdoc />
    public ClaimsPrincipal? ClaimsPrincipal { get; set; }

    /// <inheritdoc />
    public bool IsAuthenticated => ClaimsPrincipal?.Identity?.IsAuthenticated ?? false;

    /// <inheritdoc />
    public Guid? GetUserId()
    {
        var userIdClaim = ClaimsPrincipal?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                          ?? ClaimsPrincipal?.FindFirst(AuthorizationClaims.Sub)?.Value;

        return Guid.TryParse(userIdClaim, out var userId) ? userId : null;
    }

    /// <inheritdoc />
    public Guid? GetTenantId()
    {
        var tenantIdClaim = ClaimsPrincipal?.FindFirst(AuthorizationClaims.TenantId)?.Value;
        return Guid.TryParse(tenantIdClaim, out var tenantId) ? tenantId : null;
    }
}
