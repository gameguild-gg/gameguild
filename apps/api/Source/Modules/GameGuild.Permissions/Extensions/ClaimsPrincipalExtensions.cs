using System.Security.Claims;

namespace GameGuild.Permissions.Infrastructure.Extensions;

/// <summary>
///     Extension methods for ClaimsPrincipal to simplify claims extraction
/// </summary>
public static class ClaimsPrincipalExtensions
{
    /// <summary>
    ///     Gets the user ID from claims
    /// </summary>
    public static Guid? GetUserId(this ClaimsPrincipal principal)
    {
        var userIdClaim = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? principal.FindFirst("sub")?.Value ?? principal.FindFirst("user_id")?.Value;

        return Guid.TryParse(userIdClaim, out var userId) ? userId : null;
    }

    /// <summary>
    ///     Gets the tenant ID from claims
    /// </summary>
    public static Guid? GetTenantId(this ClaimsPrincipal principal)
    {
        var tenantIdClaim = principal.FindFirst("tenant_id")?.Value;

        return Guid.TryParse(tenantIdClaim, out var tenantId) ? tenantId : null;
    }

    /// <summary>
    ///     Gets the user email from claims
    /// </summary>
    public static string? GetEmail(this ClaimsPrincipal principal) { return principal.FindFirst(ClaimTypes.Email)?.Value ?? principal.FindFirst("email")?.Value; }

    /// <summary>
    ///     Gets the user's full name from claims
    /// </summary>
    public static string? GetFullName(this ClaimsPrincipal principal) { return principal.FindFirst(ClaimTypes.Name)?.Value ?? principal.FindFirst("name")?.Value ?? principal.Identity?.Name; }

    /// <summary>
    ///     Gets all roles from claims
    /// </summary>
    public static IEnumerable<string> GetRoles(this ClaimsPrincipal principal) { return principal.Claims.Where(c => c.Type == ClaimTypes.Role || c.Type == "role").Select(c => c.Value).Distinct(); }

    /// <summary>
    ///     Checks if user has a specific role
    /// </summary>
    public static bool HasRole(this ClaimsPrincipal principal, string role) { return principal.GetRoles().Any(r => r.Equals(role, StringComparison.OrdinalIgnoreCase)); }

    /// <summary>
    ///     Checks if user has any of the specified roles
    /// </summary>
    public static bool HasAnyRole(this ClaimsPrincipal principal, params string[ ] roles)
    {
        var userRoles = principal.GetRoles().ToHashSet(StringComparer.OrdinalIgnoreCase);

        return roles.Any(r => userRoles.Contains(r));
    }

    /// <summary>
    ///     Checks if user has all of the specified roles
    /// </summary>
    public static bool HasAllRoles(this ClaimsPrincipal principal, params string[ ] roles)
    {
        var userRoles = principal.GetRoles().ToHashSet(StringComparer.OrdinalIgnoreCase);

        return roles.All(r => userRoles.Contains(r));
    }

    /// <summary>
    ///     Checks if user is a system administrator
    /// </summary>
    public static bool IsSystemAdmin(this ClaimsPrincipal principal) { return principal.HasAnyRole("SystemAdmin", "Admin", "SuperAdmin"); }

    /// <summary>
    ///     Checks if user is a tenant administrator
    /// </summary>
    public static bool IsTenantAdmin(this ClaimsPrincipal principal) { return principal.HasAnyRole("TenantAdmin", "SystemAdmin", "Admin"); }

    /// <summary>
    ///     Gets a custom claim value by type
    /// </summary>
    public static string? GetClaimValue(this ClaimsPrincipal principal, string claimType) { return principal.FindFirst(claimType)?.Value; }

    /// <summary>
    ///     Gets all claim values for a specific type
    /// </summary>
    public static IEnumerable<string> GetClaimValues(this ClaimsPrincipal principal, string claimType) { return principal.Claims.Where(c => c.Type == claimType).Select(c => c.Value); }

    /// <summary>
    ///     Gets tenant name from claims
    /// </summary>
    public static string? GetTenantName(this ClaimsPrincipal principal) { return principal.FindFirst("tenant_name")?.Value; }

    /// <summary>
    ///     Gets subscription plan from claims
    /// </summary>
    public static string? GetSubscriptionPlan(this ClaimsPrincipal principal) { return principal.FindFirst("subscription_plan")?.Value; }

    /// <summary>
    ///     Gets culture code from claims
    /// </summary>
    public static string? GetCultureCode(this ClaimsPrincipal principal) { return principal.FindFirst("culture")?.Value; }

    /// <summary>
    ///     Gets timezone from claims
    /// </summary>
    public static string? GetTimeZone(this ClaimsPrincipal principal) { return principal.FindFirst("timezone")?.Value; }
}
