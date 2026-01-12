using Microsoft.AspNetCore.Http;

namespace GameGuild.Identity.Authorization;

/// <summary>
///     Extracts tenant context from HttpContext (claims, headers, or query string)
///     Priority: Claims > Header > Query String > Route Value
/// </summary>
public class TenantContext(IHttpContextAccessor httpContextAccessor) : ITenantContext
{
    private const string TenantIdClaimType = "tenant_id";
    private const string TenantNameClaimType = "tenant_name";
    private const string TenantHeaderKey = "X-Tenant-Id";
    private const string TenantQueryKey = "tenantId";

    public Guid? TenantId
    {
        get
        {
            var httpContext = httpContextAccessor.HttpContext;

            if (httpContext == null) return null;

            // Try claims first
            var tenantIdClaim = httpContext.User.FindFirst(TenantIdClaimType)?.Value;

            if (Guid.TryParse(tenantIdClaim, out var tenantIdFromClaim)) 
            { 
                return tenantIdFromClaim; 
            }

            // Try header
            if (httpContext.Request.Headers.TryGetValue(TenantHeaderKey, out var tenantIdHeader) && 
                Guid.TryParse(tenantIdHeader, out var tenantIdFromHeader)) 
            { 
                return tenantIdFromHeader; 
            }

            // Try query string
            if (httpContext.Request.Query.TryGetValue(TenantQueryKey, out var tenantIdQuery) && 
                Guid.TryParse(tenantIdQuery, out var tenantIdFromQuery)) 
            { 
                return tenantIdFromQuery; 
            }

            // Try route value
            if (httpContext.Request.RouteValues.TryGetValue(TenantQueryKey, out var tenantIdRoute) && 
                Guid.TryParse(tenantIdRoute?.ToString(), out var tenantIdFromRoute)) 
            { 
                return tenantIdFromRoute; 
            }

            return null;
        }
    }

    public string? TenantName => httpContextAccessor.HttpContext?.User.FindFirst(TenantNameClaimType)?.Value;

    public bool IsActive
    {
        get
        {
            var isActiveClaim = httpContextAccessor.HttpContext?.User.FindFirst("tenant_active")?.Value;
            return bool.TryParse(isActiveClaim, out var isActive) && isActive;
        }
    }

    public string? SubscriptionPlan => httpContextAccessor.HttpContext?.User.FindFirst("subscription_plan")?.Value;

    public IDictionary<string, object> Settings
    {
        get
        {
            var settings = new Dictionary<string, object>();
            var user = httpContextAccessor.HttpContext?.User;

            if (user != null)
            {
                // Collect all tenant-specific settings from claims
                foreach (var claim in user.Claims.Where(c => c.Type.StartsWith("tenant_setting:", StringComparison.Ordinal)))
                {
                    var key = claim.Type.Replace("tenant_setting:", "", StringComparison.Ordinal);
                    settings[key] = claim.Value;
                }
            }

            return settings;
        }
    }
}
