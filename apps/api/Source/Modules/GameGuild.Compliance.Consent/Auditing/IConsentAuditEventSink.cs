namespace GameGuild.Compliance.Consent;

/// <summary>
/// Action types for consent and data subject request audit events. The literal values are part of the
/// audit contract: the central audit module classifies these same values through its taxonomy.
/// </summary>
public static class ConsentAuditActionTypes
{
    public const string Granted = "Consent.Granted";
    public const string Revoked = "Consent.Revoked";
    public const string RequestSubmitted = "DataProtection.DataSubjectRequestRecorded";
    public const string RequestCompleted = "DataProtection.DataSubjectRequestCompleted";
    public const string RequestRejected = "DataProtection.DataSubjectRequestRejected";
}

/// <summary>
/// Publishes consent and data-subject-request lifecycle events without coupling the consent module
/// to a particular audit storage implementation.
/// </summary>
public interface IConsentAuditEventSink
{
    Task RecordAsync(ConsentAuditEvent auditEvent, CancellationToken cancellationToken);
}

/// <summary>
/// A privacy-relevant consent or data subject request event. Personal data values must never be
/// included; descriptions carry the event semantics only.
/// </summary>
public sealed record ConsentAuditEvent(
    string ActionType,
    Guid? SubjectUserId,
    Guid? TenantId,
    bool Success,
    string Description,
    object? Metadata = null,
    string? ErrorMessage = null);
