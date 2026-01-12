using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace GameGuild.Identity.Context;

/// <summary>
///     Default implementation of IIdentityContext.
///     Provides access to the current user's identity information from the HTTP context.
/// </summary>
public class IdentityContext : IIdentityContext
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public IdentityContext(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    private ClaimsPrincipal? User => _httpContextAccessor.HttpContext?.User;

    /// <inheritdoc />
    public Guid? CurrentUserId
    {
        get
        {
            var userIdClaim = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                              ?? User?.FindFirst("sub")?.Value;

            return Guid.TryParse(userIdClaim, out var userId) ? userId : null;
        }
    }

    /// <inheritdoc />
    public Guid? CurrentTenantId
    {
        get
        {
            var tenantIdClaim = User?.FindFirst("tenant_id")?.Value
                                ?? _httpContextAccessor.HttpContext?.Request.Headers["X-Tenant-Id"].FirstOrDefault();

            return Guid.TryParse(tenantIdClaim, out var tenantId) ? tenantId : null;
        }
    }

    /// <inheritdoc />
    public bool IsAuthenticated => User?.Identity?.IsAuthenticated ?? false;

    /// <inheritdoc />
    public string? CurrentUserEmail => User?.FindFirst(ClaimTypes.Email)?.Value
                                       ?? User?.FindFirst("email")?.Value;

    /// <inheritdoc />
    public string? CurrentUserName => User?.FindFirst(ClaimTypes.Name)?.Value
                                      ?? User?.FindFirst("name")?.Value
                                      ?? User?.Identity?.Name;

    /// <inheritdoc />
    public IReadOnlyList<string> CurrentUserRoles =>
        User?.Claims
            .Where(c => c.Type == ClaimTypes.Role || c.Type == "role")
            .Select(c => c.Value)
            .ToList()
            .AsReadOnly()
        ?? Array.Empty<string>().ToList().AsReadOnly();

    /// <inheritdoc />
    public Task<bool> HasPermissionAsync(string permission)
    {
        // Check if the user has a permission claim
        var hasPermission = User?.Claims
            .Any(c => (c.Type == "permission" || c.Type == "permissions") && c.Value == permission) ?? false;

        return Task.FromResult(hasPermission);
    }

    /// <inheritdoc />
    public Task<bool> HasAnyPermissionAsync(params string[] permissions)
    {
        if (permissions.Length == 0)
            return Task.FromResult(false);

        var userPermissions = User?.Claims
            .Where(c => c.Type == "permission" || c.Type == "permissions")
            .Select(c => c.Value)
            .ToHashSet() ?? new HashSet<string>();

        var hasAny = permissions.Any(p => userPermissions.Contains(p));
        return Task.FromResult(hasAny);
    }

    /// <inheritdoc />
    public Task<bool> HasAllPermissionsAsync(params string[] permissions)
    {
        if (permissions.Length == 0)
            return Task.FromResult(true);

        var userPermissions = User?.Claims
            .Where(c => c.Type == "permission" || c.Type == "permissions")
            .Select(c => c.Value)
            .ToHashSet() ?? new HashSet<string>();

        var hasAll = permissions.All(p => userPermissions.Contains(p));
        return Task.FromResult(hasAll);
    }

    /// <inheritdoc />
    public bool IsInRole(string role)
    {
        return User?.IsInRole(role) ?? false;
    }
}
