namespace GameGuild.Identity.Context.Actors;

/// <summary>
///     Interface for logging security audit events related to actor context and authorization.
/// </summary>
/// <remarks>
///     <para>
///         This interface provides security-focused audit logging for the actor context system.
///         It complements <c>IPermissionAuditService</c> (which handles permission grants/revokes)
///         by capturing runtime security events like unauthorized access attempts and privilege changes.
///     </para>
///     <para>
///         <b>Implementation Notes:</b>
///         <list type="bullet">
///             <item>Implementations should be asynchronous and non-blocking to avoid impacting request latency</item>
///             <item>Consider using a buffered/batched approach for high-throughput scenarios</item>
///             <item>Events should be persisted to durable storage for compliance and forensics</item>
///             <item>Sensitive data (passwords, tokens) should NEVER be included in events</item>
///         </list>
///     </para>
///     <para>
///         <b>Relationship to other audit services:</b>
///         <list type="bullet">
///             <item><c>ISecurityAuditLogger</c>: Runtime security events (access attempts, privilege checks)</item>
///             <item><c>IPermissionAuditService</c>: Permission configuration changes (grants, revokes)</item>
///             <item><c>IActivityLogger</c>: Application-level business operations (if exists)</item>
///         </list>
///     </para>
/// </remarks>
public interface ISecurityAuditLogger
{
    /// <summary>
    ///     Logs a security audit event.
    /// </summary>
    /// <param name="auditEvent">The security event to log.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the async operation.</returns>
    /// <remarks>
    ///     This method should not throw exceptions for logging failures.
    ///     Implementations should handle failures gracefully (e.g., log to fallback, buffer for retry).
    /// </remarks>
    Task LogAsync(SecurityAuditEvent auditEvent, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Logs an unauthorized access attempt.
    /// </summary>
    /// <param name="actorContext">The actor who attempted access.</param>
    /// <param name="resourceType">Type of resource accessed.</param>
    /// <param name="resourceId">Identifier of the resource.</param>
    /// <param name="requiredPermission">The permission that was required but not held.</param>
    /// <param name="reason">Optional reason for the denial.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task LogUnauthorizedAccessAsync(
        ActorContext actorContext,
        string resourceType,
        string? resourceId,
        string requiredPermission,
        string? reason = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Logs a sensitive resource access event.
    /// </summary>
    /// <param name="actorContext">The actor who accessed the resource.</param>
    /// <param name="resourceType">Type of sensitive resource.</param>
    /// <param name="resourceId">Identifier of the resource.</param>
    /// <param name="action">Action performed on the resource.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <remarks>
    ///     Use this for high-value resources that require audit trails (e.g., PII, financial data).
    /// </remarks>
    Task LogSensitiveAccessAsync(
        ActorContext actorContext,
        string resourceType,
        string resourceId,
        string action,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Logs a privilege escalation detection event.
    /// </summary>
    /// <param name="actorContext">The actor involved in the escalation.</param>
    /// <param name="previousRoles">Previous roles before escalation attempt.</param>
    /// <param name="attemptedRoles">Roles the actor attempted to gain.</param>
    /// <param name="success">Whether the escalation was successful.</param>
    /// <param name="reason">Reason for the escalation (or denial).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task LogPrivilegeEscalationAsync(
        ActorContext actorContext,
        IEnumerable<string> previousRoles,
        IEnumerable<string> attemptedRoles,
        bool success,
        string? reason = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Logs a cross-tenant access attempt.
    /// </summary>
    /// <param name="actorContext">The actor attempting cross-tenant access.</param>
    /// <param name="sourceTenantId">The actor's original tenant.</param>
    /// <param name="targetTenantId">The tenant the actor is trying to access.</param>
    /// <param name="resourceType">Type of resource being accessed.</param>
    /// <param name="success">Whether the access was granted.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task LogCrossTenantAccessAsync(
        ActorContext actorContext,
        Guid sourceTenantId,
        Guid targetTenantId,
        string resourceType,
        bool success,
        CancellationToken cancellationToken = default);
}
