using GameGuild.Identity.Context.Actors;

namespace GameGuild.Identity.Tenants;

/// <summary>
///     Default <see cref="ITenantAdministrationGuard"/>: membership-role enforcement
///     scoped to the target tenant (issue #1287 T1).
/// </summary>
/// <remarks>
///     <para>
///         The actor's administrative standing is resolved from the tenant membership
///         row for the tenant actually being mutated — not from the request's tenant
///         context, which is resolved from headers before the route and therefore
///         cannot be trusted to name the resource being mutated. This is the same
///         membership source <see cref="TenantMembershipRolePermissionProvider"/>
///         projects into the permission pipeline, evaluated here directly against the
///         mutation target.
///     </para>
///     <para>
///         Fail-closed by construction: no actor context, an unresolvable subject, or
///         a missing/inactive/soft-deleted membership all deny.
///     </para>
/// </remarks>
public sealed class TenantAdministrationGuard(
    IActorContextAccessor actorContextAccessor,
    ITenantMemberRepository memberRepository) : ITenantAdministrationGuard
{
    public async Task EnsureCanMutateAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        var actorId = ResolveActor();

        if (IsSystemAdmin())
        {
            return;
        }

        var membership = await LoadActiveMembershipAsync(actorId, tenantId, cancellationToken).ConfigureAwait(false);

        if (membership is null || !IsAdministrativeRole(membership.Role))
        {
            throw AccessDeniedException.ForTenantMembership(actorId, tenantId);
        }
    }

    public async Task EnsureCanPurgeAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        var actorId = ResolveActor();

        if (IsSystemAdmin())
        {
            return;
        }

        var membership = await LoadActiveMembershipAsync(actorId, tenantId, cancellationToken).ConfigureAwait(false);

        // Tenancy model: the Admin membership role carries full management EXCEPT
        // tenant deletion — only the Owner (or a platform administrator above) deletes.
        if (membership is null || !IsOwnerRole(membership.Role))
        {
            throw new AccessDeniedException(
                $"User {actorId} is not the owner of tenant {tenantId}; tenant deletion requires the Owner membership role or platform administration");
        }
    }

    private Guid ResolveActor()
    {
        var context = actorContextAccessor.ActorContext;

        if (!context.IsAuthenticated)
        {
            throw new AccessDeniedException("Tenant mutations require an authenticated actor");
        }

        var actorId = context.SubjectIdAsGuid;
        if (!actorId.HasValue || actorId.Value == Guid.Empty)
        {
            throw new AccessDeniedException("Tenant mutations require a resolvable actor identity");
        }

        return actorId.Value;
    }

    private bool IsSystemAdmin() => actorContextAccessor.ActorContext.IsSystemAdmin;

    private async Task<TenantMember?> LoadActiveMembershipAsync(Guid userId, Guid tenantId, CancellationToken cancellationToken)
    {
        var membership = await memberRepository
            .GetByUserAndTenantAsync(userId, tenantId, cancellationToken)
            .ConfigureAwait(false);

        if (membership is null ||
            !membership.IsActive ||
            membership.DeletedAt is not null ||
            membership.UserId != userId ||
            membership.TenantId != tenantId)
        {
            return null;
        }

        return membership;
    }

    // Same administrative-role set the module's permission provider projects
    // (Owner/Admin membership roles plus the platform TenantAdmin designation).
    private static bool IsAdministrativeRole(string? role) =>
        string.Equals(role, TenantRole.Owner.Value, StringComparison.OrdinalIgnoreCase) ||
        string.Equals(role, TenantRole.Admin.Value, StringComparison.OrdinalIgnoreCase) ||
        string.Equals(role, "TenantAdmin", StringComparison.OrdinalIgnoreCase);

    private static bool IsOwnerRole(string? role) =>
        string.Equals(role, TenantRole.Owner.Value, StringComparison.OrdinalIgnoreCase);
}
