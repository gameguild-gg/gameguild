
namespace GameGuild.Compliance.Audit;

/// <summary>
/// Field-level data access audit for tracking read/write operations on sensitive fields with PII masking.
/// Enables granular auditing of which fields were accessed, when, and by whom.
/// </summary>
public sealed class FieldAccessAudit : EntityBase {
    public Guid UserId { get; private set; }
    public string EntityType { get; private set; } = string.Empty;
    public Guid EntityId { get; private set; }
    public string FieldName { get; private set; } = string.Empty;
    public FieldAccessType AccessType { get; private set; }
    public DateTime AccessedAt { get; private set; }

    // Sensitive data handling
    public bool IsSensitiveField { get; private set; }
    public string? OldValue { get; private set; }
    public string? NewValue { get; private set; }
    public string? MaskedOldValue { get; private set; }
    public string? MaskedNewValue { get; private set; }
    public SensitivityLevel SensitivityLevel { get; private set; }

    // Access context
    public string IpAddress { get; private set; } = string.Empty;
    public string UserAgent { get; private set; } = string.Empty;
    public string? RequestId { get; private set; }
    public string? SessionId { get; private set; }
    public string? ApiEndpoint { get; private set; }

    // Compliance tracking
    public string? LegalBasis { get; private set; }
    public string? ConsentId { get; private set; }
    public bool RequiresNotification { get; private set; }
    public bool NotificationSent { get; private set; }
    public DateTime? NotificationSentAt { get; private set; }

    private FieldAccessAudit() { }

    public static FieldAccessAudit Create(
        Guid tenantId,
        Guid userId,
        string entityType,
        Guid entityId,
        string fieldName,
        FieldAccessType accessType,
        bool isSensitive,
        SensitivityLevel sensitivityLevel,
        string ipAddress,
        string userAgent) {
        return new FieldAccessAudit {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            UserId = userId,
            EntityType = entityType,
            EntityId = entityId,
            FieldName = fieldName,
            AccessType = accessType,
            IsSensitiveField = isSensitive,
            SensitivityLevel = sensitivityLevel,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            AccessedAt = SystemClock.UtcNow
        };
    }

    public void SetValues(string? oldValue, string? newValue, string? maskedOldValue, string? maskedNewValue) {
        OldValue = oldValue;
        NewValue = newValue;
        MaskedOldValue = maskedOldValue;
        MaskedNewValue = maskedNewValue;
    }

    public void SetAccessContext(string? requestId, string? sessionId, string? apiEndpoint) {
        RequestId = requestId;
        SessionId = sessionId;
        ApiEndpoint = apiEndpoint;
    }

    public void SetComplianceInfo(string? legalBasis, string? consentId, bool requiresNotification) {
        LegalBasis = legalBasis;
        ConsentId = consentId;
        RequiresNotification = requiresNotification;
    }

    public void MarkNotificationSent() {
        NotificationSent = true;
        NotificationSentAt = SystemClock.UtcNow;
    }
}

public enum FieldAccessType {
    Read,
    Write,
    Delete,
    Export,
    Anonymize,
    Encrypt,
    Decrypt
}
