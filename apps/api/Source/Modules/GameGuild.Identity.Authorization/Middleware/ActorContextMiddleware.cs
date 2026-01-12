using System.Security.Claims;
using GameGuild.Identity.Context.Actors;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace GameGuild.Identity.Authorization;

/// <summary>
///     Middleware that builds and populates the <see cref="ActorContext"/> from the current HTTP request.
/// </summary>
/// <remarks>
///     <para>
///         This middleware should run early in the pipeline, after authentication but before
///         authorization decisions are made. It extracts claims, resolves tenant context,
///         and builds an immutable ActorContext for the request.
///     </para>
///     <para>
///         For tenant-scoped permissions, this middleware can optionally fetch effective
///         permissions from the database (via IAuthorizationPermissionService).
///     </para>
///     <para>
///         SECURITY: This middleware implements fail-closed error handling. If permission fetching
///         fails, the ActorContext is set to Anonymous and a 500 error is returned to prevent
///         potential privilege escalation from stale token permissions.
///     </para>
/// </remarks>
public sealed class ActorContextMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ActorContextMiddleware> _logger;

    /// <summary>
    ///     Initializes a new instance of the <see cref="ActorContextMiddleware"/> class.
    /// </summary>
    public ActorContextMiddleware(RequestDelegate next, ILogger<ActorContextMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    /// <summary>
    ///     Invokes the middleware.
    /// </summary>
    public async Task InvokeAsync(
        HttpContext context,
        IActorContextAccessor actorContextAccessor,
        IAuthorizationTenantResolver tenantResolver,
        IAuthorizationPermissionService? permissionService = null)
    {
        try
        {
            var actorContext = await BuildActorContextAsync(
                context, 
                tenantResolver, 
                permissionService,
                context.RequestAborted);

            actorContextAccessor.SetActorContext(actorContext);

            await _next(context);
        }
        catch (PermissionFetchException ex)
        {
            // SECURITY: Fail-closed on permission fetch errors
            // Set context to Anonymous to deny all permissions
            actorContextAccessor.SetActorContext(ActorContext.Anonymous);
            
            _logger.LogError(ex,
                "SECURITY: Permission fetch failed for user {SubjectId} in tenant {TenantId}. " +
                "Request denied with fail-closed policy. RequestId: {RequestId}, Path: {Path}",
                ex.SubjectId,
                ex.TenantId,
                context.TraceIdentifier,
                context.Request.Path);

            // Return 500 to indicate server error (don't leak security details to client)
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            context.Response.ContentType = "application/problem+json";
            
            await context.Response.WriteAsJsonAsync(new
            {
                type = "https://tools.ietf.org/html/rfc7231#section-6.6.1",
                title = "Internal Server Error",
                status = 500,
                detail = "An error occurred while processing the security context. Please try again.",
                traceId = context.TraceIdentifier
            }, cancellationToken: context.RequestAborted);
        }
        finally
        {
            // Clear context to prevent leaking to pooled connections
            actorContextAccessor.ClearActorContext();
        }
    }

    private static async Task<ActorContext> BuildActorContextAsync(
        HttpContext httpContext,
        IAuthorizationTenantResolver tenantResolver,
        IAuthorizationPermissionService? permissionService,
        CancellationToken cancellationToken)
    {
        var user = httpContext.User;
        var isAuthenticated = user.Identity?.IsAuthenticated ?? false;

        if (!isAuthenticated)
        {
            return ActorContext.Anonymous;
        }

        // Determine actor kind from claims
        var actorKind = DetermineActorKind(user);

        // Extract subject ID
        var subjectId = ClaimNames.GetUserId(user);

        // Resolve tenant ID
        var tenantIdStr = await tenantResolver.ResolveTenantIdAsync(httpContext, cancellationToken);
        Guid? tenantId = Guid.TryParse(tenantIdStr, out var tid) ? tid : null;

        // Extract roles from claims
        var roles = ExtractRoles(user);

        // Extract permissions - either from claims or fetch from database
        var permissions = await ExtractOrFetchPermissionsAsync(
            user, 
            subjectId, 
            tenantId, 
            permissionService, 
            cancellationToken);

        // Extract attributes from claims
        var attributes = ExtractAttributes(user, tenantId);

        // Determine auth scheme
        var authScheme = httpContext.User.Identity?.AuthenticationType;

        var builder = ActorContextBuilder.Create()
            .WithActorKind(actorKind)
            .WithSubjectId(subjectId)
            .WithTenantId(tenantId)
            .WithRoles(roles)
            .WithPermissions(permissions)
            .WithAttributes(attributes)
            .WithAuthScheme(authScheme)
            .AsAuthenticated();

        return builder.Build();
    }

    private static ActorKind DetermineActorKind(ClaimsPrincipal user)
    {
        // Check for service/client credentials flow
        var grantType = user.FindFirst("grant_type")?.Value;
        if (grantType == "client_credentials")
        {
            return ActorKind.Service;
        }

        // Check for explicit actor type claim
        var actorTypeClaim = user.FindFirst("actor_type")?.Value;
        if (!string.IsNullOrEmpty(actorTypeClaim))
        {
            return actorTypeClaim.ToLowerInvariant() switch
            {
                "service" => ActorKind.Service,
                "system" => ActorKind.System,
                "webhook" => ActorKind.Webhook,
                "external" => ActorKind.External,
                _ => ActorKind.User
            };
        }

        // Check for system subject
        var subjectId = ClaimNames.GetUserId(user);
        if (subjectId == SystemActor.SystemSubjectId)
        {
            return ActorKind.System;
        }

        return ActorKind.User;
    }

    private static HashSet<string> ExtractRoles(ClaimsPrincipal user)
    {
        var roles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var claim in user.Claims)
        {
            if (claim.Type == ClaimTypes.Role || 
                claim.Type == ClaimNames.Role || 
                claim.Type == "roles")
            {
                roles.Add(claim.Value);
            }
        }

        return roles;
    }

    private static async Task<HashSet<string>> ExtractOrFetchPermissionsAsync(
        ClaimsPrincipal user,
        string? subjectId,
        Guid? tenantId,
        IAuthorizationPermissionService? permissionService,
        CancellationToken cancellationToken)
    {
        var permissions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // First, extract permissions from claims (if present in token)
        foreach (var claim in user.Claims)
        {
            if (claim.Type == "permission" || claim.Type == "permissions")
            {
                permissions.Add(claim.Value);
            }
        }

        // If we have a permission service and a valid subject/tenant, fetch from database
        // This ensures we always have the latest permissions even if not in token
        if (permissionService != null && 
            !string.IsNullOrEmpty(subjectId) && 
            Guid.TryParse(subjectId, out var userId) &&
            tenantId.HasValue)
        {
            try
            {
                var dbPermissions = await permissionService.GetPermissionsAsync(
                    userId, 
                    tenantId.Value, 
                    cancellationToken);

                // SECURITY: Replace claim permissions with database permissions
                // Database is the source of truth for current permissions
                permissions.Clear();
                foreach (var perm in dbPermissions)
                {
                    permissions.Add(perm);
                }
            }
            catch (Exception ex)
            {
                // SECURITY: Fail-closed on permission fetch errors
                // Throw custom exception to trigger middleware error handling
                throw new PermissionFetchException(
                    $"Failed to fetch permissions for user {userId} in tenant {tenantId}",
                    userId,
                    tenantId.Value,
                    ex);
            }
        }

        return permissions;
    }

    private static Dictionary<string, string> ExtractAttributes(ClaimsPrincipal user, Guid? tenantId)
    {
        var attributes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        // Standard claims to extract
        var claimMappings = new[]
        {
            (ClaimTypes.Email, "email"),
            ("email", "email"),
            (ClaimTypes.Name, "name"),
            ("name", "name"),
            ("preferred_username", "preferred_username"),
            ("email_verified", "email_verified"),
            ("mfa_verified", "mfa_verified"),
            (ClaimNames.Amr, "amr"),
            ("tenant_name", "tenant_name"),
            ("tenant_active", "tenant_active"),
            ("subscription_plan", "subscription_plan"),
            ("department", "department"),
            ("locale", "locale")
        };

        foreach (var (claimType, attrKey) in claimMappings)
        {
            var claimValue = user.FindFirst(claimType)?.Value;
            if (!string.IsNullOrEmpty(claimValue) && !attributes.ContainsKey(attrKey))
            {
                attributes[attrKey] = claimValue;
            }
        }

        // Extract tenant settings
        foreach (var claim in user.Claims)
        {
            if (claim.Type.StartsWith("tenant_setting:", StringComparison.Ordinal))
            {
                attributes[claim.Type] = claim.Value;
            }
        }

        // Add tenant ID as attribute for reference
        if (tenantId.HasValue)
        {
            attributes["tenant_id"] = tenantId.Value.ToString();
        }

        return attributes;
    }
}
