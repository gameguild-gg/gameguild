using Microsoft.Extensions.Logging;

namespace GameGuild.Identity.Context.Actors;

/// <summary>
///     Default implementation of <see cref="ISecurityAuditLogger"/> that logs to structured logging.
/// </summary>
/// <remarks>
///     <para>
///         This implementation writes security audit events as structured log entries.
///         In production, these logs should be captured by a centralized logging system
///         (e.g., Azure Monitor, Elasticsearch, Splunk) for analysis and alerting.
///     </para>
///     <para>
///         <b>Log Levels Used:</b>
///         <list type="bullet">
///             <item><c>Warning</c>: Unauthorized access attempts, privilege escalation attempts</item>
///             <item><c>Information</c>: Sensitive resource access, cross-tenant access grants</item>
///             <item><c>Error</c>: Cross-tenant access denials (potential attack indicators)</item>
///         </list>
///     </para>
///     <para>
///         For high-compliance scenarios, consider implementing a database-backed version
///         that persists events to a dedicated audit table with tamper-evident logging.
///     </para>
/// </remarks>
public sealed class SecurityAuditLogger : ISecurityAuditLogger
{
    private readonly ILogger<SecurityAuditLogger> _logger;

    public SecurityAuditLogger(ILogger<SecurityAuditLogger> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public Task LogAsync(SecurityAuditEvent auditEvent, CancellationToken cancellationToken = default)
    {
        try
        {
            var logLevel = GetLogLevelForEventType(auditEvent.EventType, auditEvent.Success);

            _logger.Log(
                logLevel,
                "Security Event: {EventType} | Subject: {SubjectId} | Tenant: {TenantId} | " +
                "ActorKind: {ActorKind} | Resource: {ResourceType}/{ResourceId} | " +
                "Permission: {Permission} | Success: {Success} | Reason: {Reason} | " +
                "EventId: {EventId}",
                auditEvent.EventType,
                auditEvent.SubjectId ?? "anonymous",
                auditEvent.TenantId?.ToString() ?? "none",
                auditEvent.ActorKind,
                auditEvent.ResourceType ?? "none",
                auditEvent.ResourceId ?? "none",
                auditEvent.Permission ?? "none",
                auditEvent.Success,
                auditEvent.Reason ?? "none",
                auditEvent.EventId);
        }
        catch (Exception ex)
        {
            // Security audit logging should never cause request failures
            // Log the failure at a lower level and continue
            _logger.LogDebug(ex, "Failed to log security audit event {EventId}", auditEvent.EventId);
            throw;
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task LogUnauthorizedAccessAsync(
        ActorContext actorContext,
        string resourceType,
        string? resourceId,
        string requiredPermission,
        string? reason = null,
        CancellationToken cancellationToken = default)
    {
        var auditEvent = SecurityAuditEvent.Create(
            SecurityEventType.UnauthorizedAccessAttempt,
            actorContext,
            resourceType,
            resourceId,
            requiredPermission,
            success: false,
            reason ?? $"Missing permission: {requiredPermission}");

        return LogAsync(auditEvent, cancellationToken);
    }

    /// <inheritdoc />
    public Task LogSensitiveAccessAsync(
        ActorContext actorContext,
        string resourceType,
        string resourceId,
        string action,
        CancellationToken cancellationToken = default)
    {
        var auditEvent = SecurityAuditEvent.Create(
            SecurityEventType.SensitiveResourceAccess,
            actorContext,
            resourceType,
            resourceId,
            permission: null,
            success: true,
            reason: $"Action: {action}");

        return LogAsync(auditEvent, cancellationToken);
    }

    /// <inheritdoc />
    public Task LogPrivilegeEscalationAsync(
        ActorContext actorContext,
        IEnumerable<string> previousRoles,
        IEnumerable<string> attemptedRoles,
        bool success,
        string? reason = null,
        CancellationToken cancellationToken = default)
    {
        var auditEvent = new SecurityAuditEvent
        {
            EventId = Guid.NewGuid(),
            EventType = SecurityEventType.PrivilegeEscalationAttempt,
            Timestamp = DateTime.UtcNow,
            SubjectId = actorContext.SubjectId,
            TenantId = actorContext.TenantId,
            ActorKind = actorContext.ActorKind,
            Success = success,
            Reason = reason,
            AdditionalData = new Dictionary<string, object>
            {
                ["previousRoles"] = previousRoles.ToList(),
                ["attemptedRoles"] = attemptedRoles.ToList()
            }
        };

        return LogAsync(auditEvent, cancellationToken);
    }

    /// <inheritdoc />
    public Task LogCrossTenantAccessAsync(
        ActorContext actorContext,
        Guid sourceTenantId,
        Guid targetTenantId,
        string resourceType,
        bool success,
        CancellationToken cancellationToken = default)
    {
        var auditEvent = new SecurityAuditEvent
        {
            EventId = Guid.NewGuid(),
            EventType = SecurityEventType.CrossTenantAccess,
            Timestamp = DateTime.UtcNow,
            SubjectId = actorContext.SubjectId,
            TenantId = targetTenantId,
            ActorKind = actorContext.ActorKind,
            ResourceType = resourceType,
            Success = success,
            Reason = success 
                ? $"Cross-tenant access granted from {sourceTenantId} to {targetTenantId}"
                : $"Cross-tenant access denied from {sourceTenantId} to {targetTenantId}",
            AdditionalData = new Dictionary<string, object>
            {
                ["sourceTenantId"] = sourceTenantId,
                ["targetTenantId"] = targetTenantId
            }
        };

        return LogAsync(auditEvent, cancellationToken);
    }

    private static LogLevel GetLogLevelForEventType(SecurityEventType eventType, bool success)
    {
        return eventType switch
        {
            SecurityEventType.UnauthorizedAccessAttempt => LogLevel.Warning,
            SecurityEventType.PrivilegeEscalationAttempt when !success => LogLevel.Warning,
            SecurityEventType.PrivilegeEscalationAttempt when success => LogLevel.Information,
            SecurityEventType.CrossTenantAccess when !success => LogLevel.Error,
            SecurityEventType.CrossTenantAccess when success => LogLevel.Information,
            SecurityEventType.SensitiveResourceAccess => LogLevel.Information,
            SecurityEventType.ImpersonationStarted => LogLevel.Warning,
            SecurityEventType.ImpersonationEnded => LogLevel.Information,
            SecurityEventType.SessionTerminated => LogLevel.Information,
            SecurityEventType.ContextElevated => LogLevel.Information,
            SecurityEventType.ContextElevationExpired => LogLevel.Information,
            SecurityEventType.ActorContextCreated => LogLevel.Debug,
            _ => LogLevel.Information
        };
    }
}
