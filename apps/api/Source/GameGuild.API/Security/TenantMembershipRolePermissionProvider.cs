using GameGuild.Identity.Authorization;
using GameGuild.Identity.Tenants;

namespace GameGuild.API.Security;

/// <summary>
/// Projects this product's built-in tenant membership roles (owner, admin,
/// moderator, member, contributor, viewer, guest) into the dynamic permission
/// pipeline. Members always keep self-service user permissions; administrative
/// roles additionally receive tenant administration and user management
/// permissions.
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
        {
            return [];
        }

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

            // Administrative-tier capabilities (issue #294): tenant administrators keep
            // member management, tenant settings and the self-service audit-read tier as
            // first-class permission keys. Role administration (roles:manage) and audit
            // exports (audit:export) are deliberately NOT granted implicitly — they are
            // delegation-only capabilities so tenant owners cannot edit global roles or
            // export compliance data without an explicit grant.
            permissions.Add(ManageMembersPermission.Keys.Manage);
            permissions.Add(ManageSettingsPermission.Keys.Read);
            permissions.Add(ManageSettingsPermission.Keys.Manage);
            permissions.Add(AuditPermission.Keys.Read);

            // Query-operations tier (issue #375): administrative roles carry the
            // list/search/query operation keys so the audit-query, security-event and
            // permission-query surfaces keep working for tenant administrators. Regular
            // members receive nothing implicitly (fail-closed, explicit grants only).
            permissions.Add(QueryPermission.Keys.List);
            permissions.Add(QueryPermission.Keys.Search);
            permissions.Add(QueryPermission.Keys.Query);

            // Achievements admin tier (issue #1319 G2): the achievements:create/update/delete/award
            // keys were never seeded, so the achievements admin surface 403'd for everyone. Tenant
            // administrators receive the full management tier; the keys are also delegable
            // individually through the permission-grant surface.
            permissions.Add(global::GameGuild.Gamification.Achievements.AchievementsPermission.Keys.Create);
            permissions.Add(global::GameGuild.Gamification.Achievements.AchievementsPermission.Keys.Update);
            permissions.Add(global::GameGuild.Gamification.Achievements.AchievementsPermission.Keys.Delete);
            permissions.Add(global::GameGuild.Gamification.Achievements.AchievementsPermission.Keys.Award);
        }

        return permissions.ToArray();
    }
}
