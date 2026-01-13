using Microsoft.AspNetCore.Http;

namespace GameGuild.Identity.Tenants;

/// <summary>
///     Result of tenant resolution containing the tenant and resolution source.
/// </summary>
/// <param name="Tenant">The resolved tenant, or null if none found.</param>
/// <param name="Source">The source of the resolution (Header, Domain, Query, Route, Default, None).</param>
public sealed record TenantResolutionResult(Tenant? Tenant, TenantResolutionSource Source)
{
    /// <summary>
    ///     Creates a result indicating no tenant was resolved.
    /// </summary>
    public static TenantResolutionResult None => new(null, TenantResolutionSource.None);

    /// <summary>
    ///     Whether a tenant was successfully resolved.
    /// </summary>
    public bool HasTenant => Tenant is not null;
}

/// <summary>
///     The source from which the tenant was resolved.
/// </summary>
public enum TenantResolutionSource
{
    /// <summary>No tenant was resolved.</summary>
    None,
    
    /// <summary>Resolved from X-Tenant-Id header.</summary>
    Header,
    
    /// <summary>Resolved from request host domain.</summary>
    Domain,
    
    /// <summary>Resolved from query string parameter.</summary>
    QueryString,
    
    /// <summary>Resolved from route value.</summary>
    RouteValue,
    
    /// <summary>Resolved from JWT claims.</summary>
    Claims,
    
    /// <summary>Resolved as the system default tenant.</summary>
    Default
}

/// <summary>
///     Shared interface for resolving the current tenant from various sources.
///     Eliminates duplicate tenant resolution logic across middleware components.
/// </summary>
/// <remarks>
///     <para>
///         <b>Resolution Priority:</b>
///         <list type="number">
///             <item>X-Tenant-Id header (explicit tenant selection)</item>
///             <item>Host domain (domain-based multi-tenancy)</item>
///             <item>Query string parameter</item>
///             <item>Route value</item>
///             <item>JWT claims (user's default tenant)</item>
///             <item>System default tenant (fallback)</item>
///         </list>
///     </para>
///     <para>
///         This interface is used by:
///         <list type="bullet">
///             <item><c>TenantMiddleware</c> - Request pipeline tenant resolution</item>
///             <item><c>ActorContextMiddleware</c> - Actor context building</item>
///             <item><c>AuthorizationTenantResolver</c> - Authorization handler tenant access</item>
///         </list>
///     </para>
/// </remarks>
public interface ITenantResolver
{
    /// <summary>
    ///     Resolves the tenant from the current HTTP request.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The resolution result containing the tenant and source.</returns>
    Task<TenantResolutionResult> ResolveAsync(
        HttpContext context,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Resolves the tenant ID from a string value (Guid string or slug).
    /// </summary>
    /// <param name="tenantIdentifier">The tenant ID (Guid) or slug.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The resolved tenant, or null if not found or inactive.</returns>
    Task<Tenant?> ResolveByIdentifierAsync(
        string tenantIdentifier,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Gets the tenant ID from the current HTTP context if already resolved.
    ///     Does not perform resolution - use ResolveAsync for that.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <returns>The tenant ID if already resolved, null otherwise.</returns>
    Guid? GetResolvedTenantId(HttpContext context);
}
