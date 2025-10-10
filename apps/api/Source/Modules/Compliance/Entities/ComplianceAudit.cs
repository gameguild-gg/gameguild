using GameGuild.Core.Entities;

namespace GameGuild.Modules.Compliance.Entities;

/// <summary>
/// Represents a compliance audit log entry.
/// </summary>
public sealed class ComplianceAudit : EntityBase
{
    /// <summary>
    /// Gets or sets the tenant ID for multi-tenant support.
    /// </summary>
    public Guid? TenantId { get; set; }

    /// <summary>
    /// Gets or sets the user ID who performed the action.
    /// </summary>
    public Guid? UserId { get; set; }

    /// <summary>
    /// Gets or sets the audit event type.
    /// </summary>
    public AuditEventType EventType { get; set; }

    /// <summary>
    /// Gets or sets the entity type being audited (e.g., "ConsentPolicy", "UserConsent").
    /// </summary>
    public string EntityType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the entity ID being audited.
    /// </summary>
    public Guid? EntityId { get; set; }

    /// <summary>
    /// Gets or sets the action performed (e.g., "Created", "Updated", "Deleted", "Consented", "Withdrawn").
    /// </summary>
    public string Action { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the old values (JSON) before the change.
    /// </summary>
    public string? OldValues { get; set; }

    /// <summary>
    /// Gets or sets the new values (JSON) after the change.
    /// </summary>
    public string? NewValues { get; set; }

    /// <summary>
    /// Gets or sets the IP address from which the action was performed.
    /// </summary>
    public string? IpAddress { get; set; }

    /// <summary>
    /// Gets or sets the user agent (browser/device info).
    /// </summary>
    public string? UserAgent { get; set; }

    /// <summary>
    /// Gets or sets when the event occurred.
    /// </summary>
    public DateTime OccurredAt { get; set; }

    /// <summary>
    /// Gets or sets additional metadata (JSON).
    /// </summary>
    public string? Metadata { get; set; }

    /// <summary>
    /// Gets or sets the compliance regulation this audit relates to (e.g., "GDPR", "CCPA").
    /// </summary>
    public string? Regulation { get; set; }

    /// <summary>
    /// Gets or sets the severity level of the audit event.
    /// </summary>
    public AuditSeverity Severity { get; set; } = AuditSeverity.Info;

    /// <summary>
    /// Creates an audit entry for a consent action.
    /// </summary>
    public static ComplianceAudit ForConsent(Guid userId, Guid policyId, Guid versionId, bool isConsented, string? ipAddress, string? userAgent)
    {
        return new ComplianceAudit
        {
            UserId = userId,
            EventType = AuditEventType.ConsentGiven,
            EntityType = nameof(UserConsent),
            EntityId = policyId,
            Action = isConsented ? "Consented" : "Declined",
            NewValues = System.Text.Json.JsonSerializer.Serialize(new { PolicyId = policyId, VersionId = versionId, IsConsented = isConsented }),
            IpAddress = ipAddress,
            UserAgent = userAgent,
            OccurredAt = DateTime.UtcNow,
            Severity = AuditSeverity.Info
        };
    }

    /// <summary>
    /// Creates an audit entry for a consent withdrawal.
    /// </summary>
    public static ComplianceAudit ForWithdrawal(Guid userId, Guid consentId, string? reason, string? ipAddress)
    {
        return new ComplianceAudit
        {
            UserId = userId,
            EventType = AuditEventType.ConsentWithdrawn,
            EntityType = nameof(UserConsent),
            EntityId = consentId,
            Action = "Withdrawn",
            NewValues = System.Text.Json.JsonSerializer.Serialize(new { Reason = reason }),
            IpAddress = ipAddress,
            OccurredAt = DateTime.UtcNow,
            Severity = AuditSeverity.Warning
        };
    }
}

/// <summary>
/// Audit event types.
/// </summary>
public enum AuditEventType
{
    PolicyCreated = 1,
    PolicyUpdated = 2,
    PolicyPublished = 3,
    PolicyDeactivated = 4,
    VersionCreated = 5,
    ConsentGiven = 6,
    ConsentWithdrawn = 7,
    ConsentExpired = 8,
    DataExported = 9,
    DataDeleted = 10,
    ComplianceViolation = 11
}

/// <summary>
/// Audit severity levels.
/// </summary>
public enum AuditSeverity
{
    Info = 1,
    Warning = 2,
    Error = 3,
    Critical = 4
}
