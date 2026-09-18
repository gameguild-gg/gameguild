using GameGuild.Identity.Authorization;

namespace GameGuild.Identity.Tenants;

/// <summary>
/// Projects the built-in role stored on an active tenant membership into the
/// authorization permission pipeline.
/// </summary>
public sealed class TenantMembershipRolePermissionProvider(ITenantMemberRepository memberRepository)
    : IAuthorizationRolePermissionProvider
{
    public async Task<IReadOnlyCollection<string>> GetPermissionsAsync(
        Guid userId,
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        var membership = await memberRepository
            .GetByUserAndTenantAsync(userId, tenantId, cancellationToken)
            .ConfigureAwait(false);

        if (membership is not { IsActive: true } || string.IsNullOrWhiteSpace(membership.Role))
            return [];

        var role = TenantRole.FromString(membership.Role);
        var permissions = new HashSet<string>(
            StaticRolePermissions.GetStaticPermissions(role.Value),
            StringComparer.OrdinalIgnoreCase);

        permissions.Add(UsersPermission.Keys.ReadSelf);
        permissions.Add(UsersPermission.Keys.EditSelf);
        permissions.Add(UsersPermission.Keys.DeleteSelf);

        var isAdministrativeRole = role.IsAdmin ||
                                   string.Equals(role.Value, "TenantAdmin", StringComparison.OrdinalIgnoreCase);
        if (isAdministrativeRole)
        {
            permissions.Add(AdminPermission.Keys.TenantAdmin);
            permissions.Add(UsersPermission.Keys.Read);
            permissions.Add(UsersPermission.Keys.Create);
            permissions.Add(UsersPermission.Keys.Update);
            permissions.Add(UsersPermission.Keys.Delete);
            permissions.Add(UsersPermission.Keys.Admin);
            permissions.Add(UsersPermission.Keys.Manage);
        }

        return permissions.ToArray();
    }
}
