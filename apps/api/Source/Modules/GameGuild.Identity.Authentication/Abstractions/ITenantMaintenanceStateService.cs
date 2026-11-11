namespace GameGuild.Identity.Authentication;

/// <summary>
///     A tenant's maintenance window (issue #1134). While a window is active, non-admin
///     sign-ins into the tenant are refused with a 503 and a retry hint; tenant
///     administrators (Owner/Admin) and platform SystemAdmins still authenticate so they can
///     verify the tenant before reopening it.
/// </summary>
public sealed record TenantMaintenanceState
{
    /// <summary>The tenant the window applies to.</summary>
    public required Guid TenantId { get; init; }

    /// <summary>Free-form operator note recorded when the window was opened.</summary>
    public string? Reason { get; init; }

    /// <summary>Identity of the operator who opened the window.</summary>
    public string? OpenedBy { get; init; }

    /// <summary>When the window was opened (UTC).</summary>
    public DateTimeOffset OpenedAt { get; init; }

    /// <summary>
    ///     When the operator expects the tenant to become available again (UTC); null when
    ///     open-ended. Sign-in responses surface this as the Retry-After hint.
    /// </summary>
    public DateTimeOffset? ResumesAt { get; init; }
}

/// <summary>
///     Tenant maintenance windows, stored in the distributed cache (Redis in production, so
///     every API instance observes the same window). Reads fail OPEN: an unavailable cache
///     reports "no maintenance" — the gate protects in-flight tenants from new sessions, it
///     must never become a total sign-in outage on its own. Windows are managed out-of-band
///     (operator tooling or a host bridge calling <see cref="SetMaintenanceAsync" />);
///     there is deliberately no public endpoint in this module.
/// </summary>
public interface ITenantMaintenanceStateService
{
    /// <summary>
    ///     Returns the tenant's active maintenance window, or null when the tenant is not in
    ///     maintenance (including when the stored window has no scheduled resume that has
    ///     passed — an open-ended window only ends when cleared).
    /// </summary>
    Task<TenantMaintenanceState?> FindAsync(Guid tenantId, CancellationToken cancellationToken = default);

    /// <summary>Opens (or replaces) the maintenance window for a tenant; null clears it.</summary>
    Task SetMaintenanceAsync(Guid tenantId, TenantMaintenanceState? state, CancellationToken cancellationToken = default);
}
