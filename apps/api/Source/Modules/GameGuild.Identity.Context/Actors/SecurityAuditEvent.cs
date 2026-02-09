namespace GameGuild.Identity.Context.Actors;

/// <summary>
///     Security event types for audit logging.
/// </summary>
/// <remarks>
///     <para>
///         These events are emitted at security decision points, not on every ActorContext access.
///         Logging every access would be too verbose and create performance issues.
///     </para>
///     <para>
///         For permission changes (grant/revoke), use <c>IPermissionAuditService</c> in the Authorization module.
///     </para>
/// </remarks>
public enum SecurityEventType
{
    /// <summary>
    ///     Actor context was built for a request.
    ///     Useful for tracking authentication patterns.
    /// </summary>
    ActorContextCreated,

    /// <summary>
    ///     Actor attempted to access a resource without permission.
    ///     Critical for detecting unauthorized access attempts.
    /// </summary>
    UnauthorizedAccessAttempt,

    /// <summary>
    ///     Actor's role or permissions changed during the session.
    ///     Important for detecting privilege escalation.
    /// </summary>
    PrivilegeEscalationAttempt,

    /// <summary>
    ///     Actor accessed a sensitive resource (high-value target).
    ///     Helps track access to critical business data.
    /// </summary>
    SensitiveResourceAccess,

    /// <summary>
    ///     Actor's context was elevated (e.g., JIT elevation).
    ///     Tracks temporary privilege grants.
    /// </summary>
    ContextElevated,

    /// <summary>
    ///     Actor's elevated context was revoked/expired.
    ///     Tracks when temporary privileges end.
    /// </summary>
    ContextElevationExpired,

    /// <summary>
    ///     Actor impersonated another user.
    ///     Critical for admin audit trails.
    /// </summary>
    ImpersonationStarted,

    /// <summary>
    ///     Actor stopped impersonating another user.
    ///     Tracks impersonation session end.
    /// </summary>
    ImpersonationEnded,

    /// <summary>
    ///     Actor's session was terminated (logout, timeout, revocation).
    /// </summary>
    SessionTerminated,

    /// <summary>
    ///     Cross-tenant access was attempted or granted.
    ///     Important for multi-tenant isolation auditing.
    /// </summary>
    CrossTenantAccess
}

/// <summary>
///     Represents a security audit event for the actor context.
/// </summary>
/// <remarks>
///     <para>
///         Security events are designed to be immutable records that capture
///         the security-relevant details at the moment an event occurs.
///     </para>
///     <para>
///         <b>When to emit events:</b>
///         <list type="bullet">
///             <item>Authorization failures (UnauthorizedAccessAttempt)</item>
///             <item>Sensitive resource access patterns (SensitiveResourceAccess)</item>
///             <item>Privilege changes (PrivilegeEscalationAttempt, ContextElevated)</item>
///             <item>Session events (ImpersonationStarted, SessionTerminated)</item>
///         </list>
///     </para>
///     <para>
///         <b>NOT emitted for:</b>
///         <list type="bullet">
///             <item>Every ActorContext.HasPermission() call (too verbose)</item>
///             <item>Read-only context property access (no security relevance)</item>
///             <item>Successful normal operations (use application-level logging)</item>
///         </list>
///     </para>
/// </remarks>
public sealed record SecurityAuditEvent
{
    /// <summary>
    ///     Unique identifier for this event.
    /// </summary>
    public required Guid EventId { get; init; }

    /// <summary>
    ///     Type of security event.
    /// </summary>
    public required SecurityEventType EventType { get; init; }

    /// <summary>
    ///     When the event occurred (UTC).
    /// </summary>
    public required DateTime Timestamp { get; init; }

    /// <summary>
    ///     The actor's subject ID (user/service ID).
    /// </summary>
    public string? SubjectId { get; init; }

    /// <summary>
    ///     The tenant context when the event occurred.
    /// </summary>
    public Guid? TenantId { get; init; }

    /// <summary>
    ///     Type of actor (User, Service, System, etc.).
    /// </summary>
    public ActorKind ActorKind { get; init; }

    /// <summary>
    ///     Resource type being accessed (if applicable).
    /// </summary>
    public string? ResourceType { get; init; }

    /// <summary>
    ///     Resource identifier (if applicable).
    /// </summary>
    public string? ResourceId { get; init; }

    /// <summary>
    ///     Permission that was checked/required (if applicable).
    /// </summary>
    public string? Permission { get; init; }

    /// <summary>
    ///     Whether the operation succeeded.
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    ///     Human-readable reason or message.
    /// </summary>
    public string? Reason { get; init; }

    /// <summary>
    ///     IP address of the request (if available).
    /// </summary>
    public string? IpAddress { get; init; }

    /// <summary>
    ///     User agent string (if available).
    /// </summary>
    public string? UserAgent { get; init; }

    /// <summary>
    ///     Correlation ID for request tracing.
    /// </summary>
    public string? CorrelationId { get; init; }

    /// <summary>
    ///     Additional contextual data.
    /// </summary>
    public IReadOnlyDictionary<string, object>? AdditionalData { get; init; }

    /// <summary>
    ///     Creates a new security audit event with a generated ID and timestamp.
    /// </summary>
    public static SecurityAuditEvent Create(
        SecurityEventType eventType,
        ActorContext? actorContext = null,
        string? resourceType = null,
        string? resourceId = null,
        string? permission = null,
        bool success = true,
        string? reason = null)
    {
        return new SecurityAuditEvent
        {
            EventId = Guid.NewGuid(),
            EventType = eventType,
            Timestamp = DateTime.UtcNow,
            SubjectId = actorContext?.SubjectId,
            TenantId = actorContext?.TenantId,
            ActorKind = actorContext?.ActorKind ?? ActorKind.Anonymous,
            ResourceType = resourceType,
            ResourceId = resourceId,
            Permission = permission,
            Success = success,
            Reason = reason
        };
    }
}
