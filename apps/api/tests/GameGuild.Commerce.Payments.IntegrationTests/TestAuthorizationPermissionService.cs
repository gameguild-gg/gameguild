using GameGuild.Identity.Authorization;
using Microsoft.AspNetCore.Http;

namespace GameGuild.Commerce.Payments.IntegrationTests;

internal sealed class TestAuthorizationPermissionService(IHttpContextAccessor httpContextAccessor)
    : IAuthorizationPermissionService
{
    public Task<bool> HasPermissionAsync(
        Guid userId,
        Guid tenantId,
        string permission,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(GetPermissions().Contains(permission, StringComparer.OrdinalIgnoreCase));
    }

    public Task<IReadOnlyList<string>> GetPermissionsAsync(
        Guid userId,
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyList<string>>(GetPermissions());
    }

    public Task<PermissionCheckResult> HasAllPermissionsAsync(
        Guid userId,
        Guid tenantId,
        IEnumerable<string> permissions,
        CancellationToken cancellationToken = default)
    {
        var requested = permissions.ToList();
        var granted = GetPermissions();
        var present = requested.Where(permission => granted.Contains(permission, StringComparer.OrdinalIgnoreCase)).ToList();
        var missing = requested.Except(present, StringComparer.OrdinalIgnoreCase).ToList();
        return Task.FromResult(missing.Count == 0
            ? PermissionCheckResult.AllPresent(present)
            : PermissionCheckResult.Partial(present, missing));
    }

    public Task<PermissionCheckResult> HasAnyPermissionAsync(
        Guid userId,
        Guid tenantId,
        IEnumerable<string> permissions,
        CancellationToken cancellationToken = default)
    {
        var requested = permissions.ToList();
        var granted = GetPermissions();
        var present = requested.Where(permission => granted.Contains(permission, StringComparer.OrdinalIgnoreCase)).ToList();
        return Task.FromResult(present.Count > 0
            ? PermissionCheckResult.Partial(present, requested.Except(present, StringComparer.OrdinalIgnoreCase))
            : PermissionCheckResult.NonePresent(requested));
    }

    private IReadOnlyList<string> GetPermissions()
    {
        var headers = httpContextAccessor.HttpContext?.Request.Headers;
        if (headers is null || !headers.TryGetValue("X-Test-Permissions", out var values)) return [];

        return values
            .SelectMany(value => value?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries) ?? [])
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}
