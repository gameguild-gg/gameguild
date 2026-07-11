using System.Text.Json;
using GameGuild.Identity.Authorization;

namespace GameGuild.Identity.Authentication;

/// <summary>
/// Projects active custom-role assignments into the authorization permission pipeline.
/// </summary>
public sealed class RolePermissionProvider(IRoleRepository roleRepository) : IAuthorizationRolePermissionProvider
{
    public async Task<IReadOnlyCollection<string>> GetPermissionsAsync(
        Guid userId,
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        var roles = await roleRepository.GetUserRolesAsync(userId, includeExpired: false, cancellationToken).ConfigureAwait(false);

        return roles
            .Where(role => role.IsActive && (!role.TenantId.HasValue || role.TenantId == tenantId))
            .SelectMany(role => DeserializePermissions(role.Permissions))
            .Where(permission => !string.IsNullOrWhiteSpace(permission))
            .Where(permission => !string.Equals(permission, "admin:*", StringComparison.OrdinalIgnoreCase))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static IReadOnlyCollection<string> DeserializePermissions(string permissions)
    {
        try
        {
            return JsonSerializer.Deserialize<string[]>(permissions) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }
}
