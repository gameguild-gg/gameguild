using GameGuild.Identity.Authorization;

namespace GameGuild.Identity.Tenants;

/// <summary>
/// Projects administrative tenant membership roles into the dynamic permission pipeline.
/// </summary>
/// <remarks>
/// The membership lookup is scoped to the requested tenant and inactive or deleted
/// memberships never contribute permissions.
/// </remarks>
public sealed class TenantMembershipRolePermissionProvider(
    ITenantMemberRepository memberRepository) : IAuthorizationRolePermissionProvider
{
    public async Task<IReadOnlyCollection<string>> GetPermissionsAsync(
        Guid userId,
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        var membership = await memberRepository
            .GetByUserAndTenantAsync(userId, tenantId, cancellationToken)
            .ConfigureAwait(false);

        if (membership is null ||
            !membership.IsActive ||
            membership.DeletedAt is not null ||
            membership.UserId != userId ||
            membership.TenantId != tenantId ||
            !IsAdministrativeRole(membership.Role))
        {
            return [];
        }

        return [AdminPermission.Keys.TenantAdmin];
    }

    private static bool IsAdministrativeRole(string? role) =>
        string.Equals(role, TenantRole.Owner.Value, StringComparison.OrdinalIgnoreCase) ||
        string.Equals(role, TenantRole.Admin.Value, StringComparison.OrdinalIgnoreCase) ||
        string.Equals(role, "TenantAdmin", StringComparison.OrdinalIgnoreCase);
}
