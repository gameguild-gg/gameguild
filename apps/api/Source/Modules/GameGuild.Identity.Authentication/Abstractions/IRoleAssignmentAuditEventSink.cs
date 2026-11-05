namespace GameGuild.Identity.Authentication;

/// <summary>
///     Audit action types emitted by the role-assignment lifecycle. The values match
///     the central security-event taxonomy keys (<c>AuditActionTypes.RoleAssigned</c> /
///     <c>AuditActionTypes.RoleRevoked</c>) verbatim; they are redeclared here because
///     the identity module cannot reference the audit module (the audit module bridges
///     this sink the other way, mirroring <see cref="IAuthenticationAuditEventSink"/>).
/// </summary>
public static class RoleAssignmentAuditActionTypes
{
    /// <summary>A role was assigned to (or reactivated for) a user.</summary>
    public const string RoleAssigned = "RoleAssigned";

    /// <summary>A role was removed from a user.</summary>
    public const string RoleRevoked = "RoleRevoked";
}

/// <summary>
///     Publishes role-assignment lifecycle events (assign / revoke) without coupling
///     the identity module to a particular audit implementation. The roles path wrote
///     no audit events at all — the <c>RoleAssigned</c>/<c>RoleRevoked</c> action types
///     were listed by the compliance coverage checks with no producer (issue #1204);
///     this sink closes that gap the same way <see cref="IAuthenticationAuditEventSink"/>
///     bridges authentication events.
/// </summary>
public interface IRoleAssignmentAuditEventSink
{
    /// <summary>
    ///     Records one role-assignment lifecycle event. Implementations are expected to
    ///     journal durably in the owning command's transaction (fail closed).
    /// </summary>
    Task RecordAsync(RoleAssignmentAuditEvent auditEvent, CancellationToken cancellationToken);
}

/// <summary>
///     One role-assignment lifecycle event. Secrets and credential values are never included.
/// </summary>
/// <param name="ActionType">
///     <see cref="RoleAssignmentAuditActionTypes.RoleAssigned"/> or
///     <see cref="RoleAssignmentAuditActionTypes.RoleRevoked"/>.
/// </param>
/// <param name="SubjectUserId">The user whose role set changed.</param>
/// <param name="RoleId">The affected role.</param>
/// <param name="RoleName">The affected role's name (audit display).</param>
/// <param name="TenantId">The owning tenant, when known.</param>
/// <param name="PerformedBy">The acting administrator, when known.</param>
/// <param name="ExpiresAt">The assignment's expiry, when time-bound.</param>
/// <param name="Success">Whether the mutation succeeded.</param>
/// <param name="ErrorMessage">Failure detail when <paramref name="Success"/> is false.</param>
/// <param name="Metadata">Additional structured detail (e.g. bulk-assignment status).</param>
public sealed record RoleAssignmentAuditEvent(
    string ActionType,
    Guid SubjectUserId,
    Guid RoleId,
    string RoleName,
    Guid? TenantId = null,
    Guid? PerformedBy = null,
    DateTime? ExpiresAt = null,
    bool Success = true,
    string? ErrorMessage = null,
    object? Metadata = null);
