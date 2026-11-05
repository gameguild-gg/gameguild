namespace GameGuild.Identity.Tenants;

/// <summary>
///     Ownership/role gate for tenant lifecycle mutations (issue #1287 T1).
/// </summary>
/// <remarks>
///     <para>
///         The four tenant controllers that carried class-level <c>[Authorize]</c> only
///         (CRUD, lifecycle, bulk operations, metadata) let ANY authenticated principal
///         update, deactivate, archive, delete or purge an arbitrary tenant — the
///         mutation handlers performed no ownership or role checks. This port is the
///         authoritative enforcement point inside the module, mirroring the cohort
///         enrollment guard pattern from PR #1275: the gate lives where the mutation
///         happens, so every entry point (module controllers, the legacy
///         <c>/tenants</c> minimal-API endpoint and the Commerce.Subscriptions clients
///         surface that reuses these commands) inherits it.
///     </para>
///     <para>
///         <b>Tenancy model</b> (<see cref="TenantRole"/>): the Owner membership role
///         carries full control <i>including deletion</i>; the Admin membership role
///         carries full management <i>except tenant deletion</i>; platform
///         administrators (<c>SystemAdmin</c>) administer any tenant. Accordingly:
///         <list type="bullet">
///             <item><see cref="EnsureCanMutateAsync"/> — reversible management
///             (update, activate/deactivate, archive/undelete, metadata writes):
///             active Owner or Admin membership, or SystemAdmin.</item>
///             <item><see cref="EnsureCanPurgeAsync"/> — destructive deletion
///             (hard delete/purge): active Owner membership or SystemAdmin.</item>
///         </list>
///     </para>
///     <para>
///         Fail-closed: anonymous or unresolvable actors, and memberships that are
///         missing, inactive or soft-deleted, are denied. Denials surface as
///         <see cref="GameGuild.AccessDeniedException"/> which the shared exception
///         middleware maps to HTTP 403.
///     </para>
/// </remarks>
public interface ITenantAdministrationGuard
{
    /// <summary>
    ///     Ensures the current actor may perform a reversible management mutation on the tenant.
    /// </summary>
    /// <param name="tenantId">The tenant being mutated.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <exception cref="GameGuild.AccessDeniedException">Thrown when the actor is not an active Owner/Admin member of the tenant (or a system administrator).</exception>
    Task EnsureCanMutateAsync(Guid tenantId, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Ensures the current actor may perform a destructive deletion (hard delete/purge) on the tenant.
    /// </summary>
    /// <param name="tenantId">The tenant being deleted.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <exception cref="GameGuild.AccessDeniedException">Thrown when the actor is not the active Owner of the tenant (or a system administrator).</exception>
    Task EnsureCanPurgeAsync(Guid tenantId, CancellationToken cancellationToken = default);
}
