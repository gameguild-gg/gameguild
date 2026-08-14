using GameGuild.CQRS;
using GameGuild.Identity.Authorization;
using GameGuild.Identity.Tenants.Utilities;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace GameGuild.Identity.Tenants;

/// <summary>
///     Middleware that resolves and validates the current tenant for multi-tenant requests.
///     Resolution priority: X-Tenant-Id header > Host domain > Query string > Route > JWT claim > anonymous default tenant.
///     Stores the resolved tenant in HttpContext.Items for downstream access.
///     Uses CQRS queries via IMediator for tenant resolution.
///     
///     <para>
///         <b>Security:</b> Enforces tenant membership validation - authenticated users can only
///         access tenants they are members of. Returns 403 Forbidden for unauthorized tenant access.
///     </para>
///     <para>
///         See also: <c>docs/security/TENANT_MEMBERSHIP_VALIDATION.md</c>
///     </para>
/// </summary>
public class TenantMiddleware(
    RequestDelegate next,
    ILogger<TenantMiddleware> logger)
{
    /// <summary>
    ///     The header name for the tenant ID.
    /// </summary>
    public const string TenantIdHeader = "X-Tenant-Id";

    /// <summary>
    ///     The query string key for the tenant ID.
    /// </summary>
    public const string TenantIdQueryKey = "tenantId";

    /// <summary>
    ///     The HttpContext.Items key for the resolved tenant.
    /// </summary>
    /// <remarks>⚠️ Prefer using <see cref="HttpContextKeys.CurrentTenant"/> directly.</remarks>
    [Obsolete("Use HttpContextKeys.CurrentTenant instead for consistency across modules.")]
    public const string TenantItemKey = HttpContextKeys.CurrentTenant;

    /// <summary>
    ///     The HttpContext.Items key for the tenant ID.
    /// </summary>
    /// <remarks>⚠️ Prefer using <see cref="HttpContextKeys.AuthorizationTenantId"/> directly.</remarks>
    [Obsolete("Use HttpContextKeys.AuthorizationTenantId instead for consistency across modules.")]
    public const string TenantIdItemKey = HttpContextKeys.AuthorizationTenantId;

    /// <summary>
    ///     Paths that should bypass tenant resolution (e.g., health checks, swagger).
    /// </summary>
    private static readonly string[] BypassPaths =
    [
        "/health",
        "/ready",
        "/live",
        "/swagger",
        "/documentation",
        "/openapi",
        "/.well-known"
    ];
    
    /// <summary>
    ///     Exact paths that should bypass tenant resolution.
    /// </summary>
    private static readonly string[] ExactBypassPaths =
    [
        "/"
    ];

    public async Task InvokeAsync(
        HttpContext context,
        IMediator mediator,
        ITenantDomainsRepository tenantDomainsRepository,
        ITenantMemberRepository tenantMemberRepository)
    {
        var path = context.Request.Path.Value?.ToLowerInvariant() ?? string.Empty;

        // Skip tenant resolution for system endpoints
        if (ShouldBypassTenantResolution(context, path))
        {
            await next(context).ConfigureAwait(false);
            return;
        }

        var authenticatedTenantId = Authorization.Utilities.ClaimsExtractor.GetTenantIdAsGuid(context.User);
        var explicitTenantId = GetExplicitTenantId(context);
        if (authenticatedTenantId.HasValue &&
            explicitTenantId.HasValue &&
            authenticatedTenantId.Value != explicitTenantId.Value)
        {
            await RejectTenantClaimMismatchAsync(
                context,
                authenticatedTenantId.Value,
                explicitTenantId.Value).ConfigureAwait(false);
            return;
        }

        var (tenant, resolutionSource) = await ResolveTenantAsync(
            context,
            mediator,
            tenantDomainsRepository,
            context.RequestAborted).ConfigureAwait(false);

        if (tenant is not null)
        {
            if (authenticatedTenantId.HasValue && tenant.Id != authenticatedTenantId.Value)
            {
                await RejectTenantClaimMismatchAsync(
                    context,
                    authenticatedTenantId.Value,
                    tenant.Id).ConfigureAwait(false);
                return;
            }

            // SECURITY: Validate tenant membership for authenticated users
            var userId = GetAuthenticatedUserId(context);
            if (userId.HasValue)
            {
                var isSystemAdmin = Authorization.Utilities.ClaimsExtractor
                    .GetRoles(context.User)
                    .Any(role => role.Equals("SystemAdmin", StringComparison.OrdinalIgnoreCase));
                var membership = isSystemAdmin
                    ? null
                    : await GetActiveTenantMembershipAsync(
                        userId.Value,
                        tenant.Id,
                        tenantMemberRepository,
                        context.RequestAborted).ConfigureAwait(false);

                if (!isSystemAdmin && membership is null)
                {
                    logger.LogWarning(
                        "User {UserId} attempted to access tenant {TenantId} ({TenantName}) without membership",
                        userId.Value,
                        tenant.Id,
                        tenant.Name);

                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    await context.Response.WriteAsJsonAsync(new
                    {
                        error = "Forbidden",
                        message = "You are not a member of the requested tenant"
                    }, context.RequestAborted).ConfigureAwait(false);
                    return;
                }

                if (membership is not null)
                {
                    context.Items[HttpContextKeys.AuthorizationTenantRole] = membership.Role;
                    logger.LogDebug(
                        "Tenant membership validated: User {UserId} is member of tenant {TenantId} with role {TenantRole}",
                        userId.Value,
                        tenant.Id,
                        membership.Role);
                }
            }

            // Store tenant in HttpContext.Items for access throughout the request
            context.Items[HttpContextKeys.CurrentTenant] = tenant;
            context.Items[HttpContextKeys.AuthorizationTenantId] = tenant.Id;

            // Add to logging scope for structured logging (redact TenantId to prevent PII leakage)
            using (logger.BeginScope(new Dictionary<string, object>
            {
                ["TenantId"] = LogRedaction.RedactId(tenant.Id, "tid"),
                ["TenantSlug"] = tenant.Slug,
                ["TenantResolutionSource"] = resolutionSource
            }))
            {
                logger.LogDebug(
                    "Tenant resolved: {TenantName} ({TenantId}) via {ResolutionSource}",
                    tenant.Name,
                    LogRedaction.RedactId(tenant.Id, "tid"),
                    resolutionSource);

                await next(context).ConfigureAwait(false);
            }
        }
        else
        {
            // No tenant resolved - continue without tenant context
            // Individual endpoints can require tenant via [RequireTenant] attribute or check ITenantContext
            logger.LogDebug("No tenant resolved for request to {Path}", path);
            await next(context).ConfigureAwait(false);
        }
    }

    private static bool ShouldBypassTenantResolution(HttpContext context, string path)
    {
        if (IsReadOnlyMembershipIntrospection(context, path))
        {
            return true;
        }

        // Check for exact match paths (like root "/")
        if (ExactBypassPaths.Any(bypassPath =>
            path.Equals(bypassPath, StringComparison.OrdinalIgnoreCase)))
        {
            return true;
        }
        
        // Check for prefix match paths
        return BypassPaths.Any(bypassPath =>
            path.StartsWith(bypassPath, StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsReadOnlyMembershipIntrospection(HttpContext context, string path)
    {
        if (!HttpMethods.IsGet(context.Request.Method) && !HttpMethods.IsHead(context.Request.Method))
        {
            return false;
        }

        var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
        return segments.Length == 4 &&
               segments[0].Length > 1 &&
               segments[0][0] == 'v' &&
               int.TryParse(segments[0].AsSpan(1), out _) &&
               segments[1].Equals("users", StringComparison.OrdinalIgnoreCase) &&
               Guid.TryParse(segments[2], out _) &&
               (segments[3].Equals("memberships", StringComparison.OrdinalIgnoreCase) ||
                segments[3].Equals("memberships:count", StringComparison.OrdinalIgnoreCase));
    }

    private static Guid? GetExplicitTenantId(HttpContext context)
    {
        return TenantIdExtractor.FromHeader(context, TenantIdHeader) ??
               TenantIdExtractor.FromQuery(context, TenantIdQueryKey) ??
               TenantIdExtractor.FromRoute(context, TenantIdQueryKey);
    }

    private async Task RejectTenantClaimMismatchAsync(
        HttpContext context,
        Guid authenticatedTenantId,
        Guid requestedTenantId)
    {
        logger.LogWarning(
            "Authenticated tenant {AuthenticatedTenantId} does not match requested tenant {RequestedTenantId}",
            authenticatedTenantId,
            requestedTenantId);

        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        await context.Response.WriteAsJsonAsync(new
        {
            error = "TenantMismatch",
            message = "Switch workspace before accessing the requested tenant"
        }, context.RequestAborted).ConfigureAwait(false);
    }

    private async Task<(Tenant? Tenant, string ResolutionSource)> ResolveTenantAsync(
        HttpContext context,
        IMediator mediator,
        ITenantDomainsRepository tenantDomainsRepository,
        CancellationToken cancellationToken)
    {
        // 1. Try X-Tenant-Id header (explicit tenant selection)
        var tenantIdFromHeader = TenantIdExtractor.FromHeader(context, TenantIdHeader);
        if (tenantIdFromHeader.HasValue)
        {
            var tenant = await mediator.Send(new GetTenantByIdQuery(tenantIdFromHeader.Value), cancellationToken).ConfigureAwait(false);
            if (tenant is not null && tenant.IsActive)
            {
                return (tenant, "Header");
            }

            logger.LogWarning(
                "Tenant ID {TenantId} from header not found or inactive",
                tenantIdFromHeader);
        }

        // 2. Try host domain resolution
        var host = TenantIdExtractor.GetHost(context);
        if (!string.IsNullOrWhiteSpace(host) && !TenantIdExtractor.IsLocalhost(host))
        {
            var tenantDomain = await tenantDomainsRepository.GetByDomainAsync(host, cancellationToken).ConfigureAwait(false);
            if (tenantDomain?.Tenant is not null && tenantDomain.Tenant.IsActive)
            {
                return (tenantDomain.Tenant, "Domain");
            }
        }

        // 3. Try query string
        var tenantIdFromQuery = TenantIdExtractor.FromQuery(context, TenantIdQueryKey);
        if (tenantIdFromQuery.HasValue)
        {
            var tenant = await mediator.Send(new GetTenantByIdQuery(tenantIdFromQuery.Value), cancellationToken).ConfigureAwait(false);
            if (tenant is not null && tenant.IsActive)
            {
                return (tenant, "QueryString");
            }
        }

        // 4. Try route value
        var tenantIdFromRoute = TenantIdExtractor.FromRoute(context, TenantIdQueryKey);
        if (tenantIdFromRoute.HasValue)
        {
            var tenant = await mediator.Send(new GetTenantByIdQuery(tenantIdFromRoute.Value), cancellationToken).ConfigureAwait(false);
            if (tenant is not null && tenant.IsActive)
            {
                return (tenant, "RouteValue");
            }
        }

        // 5. Use the authenticated tenant claim when no explicit route selection was provided.
        var tenantIdFromClaim = Authorization.Utilities.ClaimsExtractor.GetTenantIdAsGuid(context.User);
        if (tenantIdFromClaim.HasValue)
        {
            var tenant = await mediator.Send(new GetTenantByIdQuery(tenantIdFromClaim.Value), cancellationToken).ConfigureAwait(false);
            if (tenant is not null && tenant.IsActive)
            {
                return (tenant, "Claim");
            }

            logger.LogWarning(
                "Tenant ID {TenantId} from authenticated claim not found or inactive",
                tenantIdFromClaim);
        }

        // 6. Only anonymous traffic can fall back to the platform default tenant.
        // Authenticated users without a tenant claim are onboarding or selecting a workspace;
        // silently binding them to the default tenant would create a cross-tenant authorization risk.
        if (Authorization.Utilities.ClaimsExtractor.IsAuthenticated(context.User))
        {
            return (null, "None");
        }

        var defaultTenant = await mediator.Send(new GetDefaultTenantQuery(), cancellationToken).ConfigureAwait(false);
        if (defaultTenant is not null && defaultTenant.IsActive)
        {
            return (defaultTenant, "Default");
        }

        return (null, "None");
    }

    /// <summary>
    ///     Extracts the authenticated user ID from the HttpContext claims.
    /// </summary>
    /// <param name="context">The HTTP context</param>
    /// <returns>The user ID if authenticated, null otherwise</returns>
    private static Guid? GetAuthenticatedUserId(HttpContext context)
    {
        if (!Authorization.Utilities.ClaimsExtractor.IsAuthenticated(context.User))
        {
            return null;
        }

        return Authorization.Utilities.ClaimsExtractor.GetUserIdAsGuid(context.User);
    }

    /// <summary>
    ///     Validates that the user is a member of the specified tenant.
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <param name="tenantId">The tenant ID</param>
    /// <param name="memberRepository">The tenant member repository</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if the user is an active member of the tenant, false otherwise</returns>
    private async Task<TenantMember?> GetActiveTenantMembershipAsync(
        Guid userId,
        Guid tenantId,
        ITenantMemberRepository memberRepository,
        CancellationToken cancellationToken)
    {
        try
        {
            var membership = await memberRepository.GetByUserAndTenantAsync(
                userId,
                tenantId,
                cancellationToken).ConfigureAwait(false);

            return membership is { IsActive: true } ? membership : null;
        }
        catch (Exception ex)
        {
            // FAIL-CLOSED: If membership check fails, deny access
            logger.LogError(
                ex,
                "Failed to validate tenant membership for user {UserId} in tenant {TenantId}",
                userId,
                tenantId);
            return null;
        }
    }
}

/// <summary>
///     Extension methods for adding TenantMiddleware to the application pipeline.
/// </summary>
public static class TenantMiddlewareExtensions
{
    /// <summary>
    ///     Adds the tenant resolution middleware to the application pipeline.
    ///     Should be placed after routing and authentication so membership validation sees the authenticated user.
    /// </summary>
    /// <param name="app">The application builder</param>
    /// <returns>The application builder for chaining</returns>
    public static IApplicationBuilder UseTenantResolution(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);
        return app.UseMiddleware<TenantMiddleware>();
    }
}
