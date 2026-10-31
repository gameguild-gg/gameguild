using System.Text.Json.Serialization;

namespace GameGuild.Compliance.Audit;

/// <summary>
///     Kinds of security-relevant events recognized by the security event pipeline.
///     Every kind maps to a default severity, an escalation policy for failed outcomes,
///     and a stable description used by the taxonomy endpoint.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<SecurityEventKind>))]
public enum SecurityEventKind
{
    Authentication = 1,
    Authorization = 2,
    SessionManagement = 3,
    AccountLifecycle = 4,
    ThreatDetection = 5,
    DataProtection = 6,
    TenantIsolation = 7,
    ConfigurationChange = 8,
    DataMovement = 9,
    Other = 10
}

/// <summary>
///     Complete classification of security events recorded through the central audit log.
///     The taxonomy is the single source of truth for the security event pipeline: it decides
///     which audit events are security events, their default severity, whether a failed outcome
///     escalates severity, and which security event kind drives alerting and retention.
/// </summary>
public static class SecurityEventTaxonomy
{
    /// <summary>
    ///     Describes one security event classification: the kind, the default severity, whether a
    ///     failed outcome escalates the severity by one level, and the stable descriptor text.
    /// </summary>
    public sealed record SecurityEventDescriptor(
        SecurityEventKind Kind,
        AuditRiskLevel DefaultSeverity,
        bool EscalateOnFailure,
        string Description);

    private static readonly Dictionary<string, SecurityEventDescriptor> Descriptors = new(StringComparer.Ordinal)
    {
        // Authentication
        [AuditActionTypes.Login] = new(SecurityEventKind.Authentication, AuditRiskLevel.Low, true, "Interactive sign-in attempt."),
        [AuditActionTypes.Logout] = new(SecurityEventKind.Authentication, AuditRiskLevel.Low, false, "Session sign-out."),
        [AuditActionTypes.LoginFailed] = new(SecurityEventKind.Authentication, AuditRiskLevel.High, false, "Failed sign-in attempt (credentials or verification challenge)."),
        [AuditActionTypes.MfaEnabled] = new(SecurityEventKind.Authentication, AuditRiskLevel.Medium, false, "Multi-factor authentication enrolled."),
        [AuditActionTypes.MfaDisabled] = new(SecurityEventKind.Authentication, AuditRiskLevel.High, false, "Multi-factor authentication disabled."),
        [AuditActionTypes.MfaVerified] = new(SecurityEventKind.Authentication, AuditRiskLevel.Low, true, "Multi-factor challenge verified."),
        [AuditActionTypes.MfaFailed] = new(SecurityEventKind.Authentication, AuditRiskLevel.High, false, "Multi-factor challenge failed."),
        [AuditActionTypes.PasswordChanged] = new(SecurityEventKind.Authentication, AuditRiskLevel.Medium, true, "Password changed."),
        [AuditActionTypes.PasswordResetRequested] = new(SecurityEventKind.Authentication, AuditRiskLevel.Medium, false, "Password reset requested."),
        [AuditActionTypes.PasswordReset] = new(SecurityEventKind.Authentication, AuditRiskLevel.High, false, "Password reset completed."),

        // Federated identity provider / SSO integration (issue #180)
        [AuditActionTypes.ExternalLoginSucceeded] = new(SecurityEventKind.Authentication, AuditRiskLevel.Low, true, "Federated or external-provider sign-in succeeded."),
        [AuditActionTypes.ExternalLoginFailed] = new(SecurityEventKind.Authentication, AuditRiskLevel.High, false, "Federated or external-provider sign-in failed."),
        [AuditActionTypes.ExternalAccountLinked] = new(SecurityEventKind.Authentication, AuditRiskLevel.Medium, false, "External identity linked to an existing account."),
        [AuditActionTypes.ExternalAccountProvisioned] = new(SecurityEventKind.AccountLifecycle, AuditRiskLevel.Medium, false, "Account provisioned just-in-time from a federated identity."),
        [AuditActionTypes.FederatedSignInMfaDenied] = new(SecurityEventKind.Authentication, AuditRiskLevel.High, false, "Federated sign-in refused because the provider did not attest multi-factor authentication."),

        // Authorization
        [AuditActionTypes.PermissionGranted] = new(SecurityEventKind.Authorization, AuditRiskLevel.Medium, false, "Permission granted to a subject."),
        [AuditActionTypes.PermissionDenied] = new(SecurityEventKind.Authorization, AuditRiskLevel.High, false, "Permission denied to a subject."),
        [AuditActionTypes.PermissionRevoked] = new(SecurityEventKind.Authorization, AuditRiskLevel.Medium, false, "Permission revoked from a subject."),
        [AuditActionTypes.PermissionChanged] = new(SecurityEventKind.Authorization, AuditRiskLevel.Medium, false, "Permission definition changed."),
        [AuditActionTypes.RoleAssigned] = new(SecurityEventKind.Authorization, AuditRiskLevel.Medium, false, "Role assigned to a subject."),
        [AuditActionTypes.RoleRevoked] = new(SecurityEventKind.Authorization, AuditRiskLevel.Medium, false, "Role revoked from a subject."),
        [AuditActionTypes.AccessDenied] = new(SecurityEventKind.Authorization, AuditRiskLevel.High, false, "Access to a resource denied."),

        // Session management
        [AuditActionTypes.SessionCreated] = new(SecurityEventKind.SessionManagement, AuditRiskLevel.Low, false, "Session established."),
        [AuditActionTypes.SessionTerminated] = new(SecurityEventKind.SessionManagement, AuditRiskLevel.Medium, false, "Session terminated."),
        [AuditActionTypes.DeviceTrusted] = new(SecurityEventKind.SessionManagement, AuditRiskLevel.Medium, false, "Device marked as trusted."),
        [AuditActionTypes.DeviceRevoked] = new(SecurityEventKind.SessionManagement, AuditRiskLevel.Medium, false, "Trusted device revoked."),

        // Account lifecycle
        [AuditActionTypes.UserCreated] = new(SecurityEventKind.AccountLifecycle, AuditRiskLevel.Medium, false, "User account created."),
        [AuditActionTypes.UserUpdated] = new(SecurityEventKind.AccountLifecycle, AuditRiskLevel.Low, true, "User account updated."),
        [AuditActionTypes.UserDeleted] = new(SecurityEventKind.AccountLifecycle, AuditRiskLevel.High, false, "User account deleted."),
        [AuditActionTypes.UserSuspended] = new(SecurityEventKind.AccountLifecycle, AuditRiskLevel.High, false, "User account suspended."),
        [AuditActionTypes.UserReactivated] = new(SecurityEventKind.AccountLifecycle, AuditRiskLevel.Medium, false, "User account reactivated."),
        [AuditActionTypes.UserProfileUpdated] = new(SecurityEventKind.AccountLifecycle, AuditRiskLevel.Low, true, "User profile updated."),
        [AuditActionTypes.UsernameNormalized] = new(SecurityEventKind.AccountLifecycle, AuditRiskLevel.Low, false, "Username normalized."),
        [AuditActionTypes.UsernameCollisionResolved] = new(SecurityEventKind.AccountLifecycle, AuditRiskLevel.Medium, false, "Username collision resolved."),

        // Threat detection
        [AuditActionTypes.SecurityViolation] = new(SecurityEventKind.ThreatDetection, AuditRiskLevel.Critical, false, "Security violation detected."),
        [AuditActionTypes.RateLimitExceeded] = new(SecurityEventKind.ThreatDetection, AuditRiskLevel.Medium, false, "Rate limit exceeded."),
        [AuditActionTypes.SuspiciousActivity] = new(SecurityEventKind.ThreatDetection, AuditRiskLevel.High, false, "Suspicious activity detected."),
        [AuditActionTypes.PolicyViolation] = new(SecurityEventKind.ThreatDetection, AuditRiskLevel.High, false, "Security policy violated."),
        [AuditActionTypes.SecurityAlertResolved] = new(SecurityEventKind.ThreatDetection, AuditRiskLevel.Low, false, "Raised security alert resolved."),

        // Webhook security (provider callback verification pipeline)
        [AuditActionTypes.WebhookSignatureFailed] = new(SecurityEventKind.ThreatDetection, AuditRiskLevel.High, false, "Billing webhook failed provider signature or timestamp verification."),
        [AuditActionTypes.WebhookSourceIpRejected] = new(SecurityEventKind.ThreatDetection, AuditRiskLevel.High, false, "Billing webhook rejected by the source IP allowlist."),
        [AuditActionTypes.WebhookReplayDetected] = new(SecurityEventKind.ThreatDetection, AuditRiskLevel.Low, false, "Duplicate billing webhook delivery detected by idempotency checks."),
        [AuditActionTypes.WebhookRateLimitExceeded] = new(SecurityEventKind.ThreatDetection, AuditRiskLevel.Medium, false, "Billing webhook endpoint rate limit exceeded."),
        [AuditActionTypes.WebhookSourceBlocked] = new(SecurityEventKind.ThreatDetection, AuditRiskLevel.High, false, "Billing webhook source temporarily blocked for suspicious activity."),
        [AuditActionTypes.WebhookDeadLettered] = new(SecurityEventKind.ThreatDetection, AuditRiskLevel.Critical, false, "Billing webhook dead-lettered after exhausting its retry budget; repeated delivery failures require operator attention."),

        // Billing webhook lifecycle (issue #408: an audit-module entry for every webhook
        // event). Operational trail, not a threat: classified as Other/Low so the events
        // journal through the standard audit path of the same pipeline entry point.
        [AuditActionTypes.WebhookEventReceived] = new(SecurityEventKind.Other, AuditRiskLevel.Low, false, "Billing webhook delivery accepted into the durable inbox."),
        [AuditActionTypes.WebhookEventProcessed] = new(SecurityEventKind.Other, AuditRiskLevel.Low, false, "Billing webhook event completed processing from the durable inbox."),
        [AuditActionTypes.WebhookEventFailed] = new(SecurityEventKind.Other, AuditRiskLevel.Medium, false, "Billing webhook event recorded a failed processing attempt; the durable inbox schedules its retry."),

        // Promo engine abuse detection (issue #418)
        [AuditActionTypes.PromoUsageAnomalyDetected] = new(SecurityEventKind.ThreatDetection, AuditRiskLevel.High, false, "Unusual promo code usage pattern detected (validation or redemption velocity anomaly)."),

        // Tenant isolation
        [AuditActionTypes.TenantIsolationBypassed] = new(SecurityEventKind.TenantIsolation, AuditRiskLevel.Critical, false, "Tenant isolation boundary bypassed."),

        // Data protection
        [AuditActionTypes.PrivacySettingsUpdated] = new(SecurityEventKind.DataProtection, AuditRiskLevel.Low, true, "Privacy settings updated."),
        [AuditActionTypes.PrivacyTemplateApplied] = new(SecurityEventKind.DataProtection, AuditRiskLevel.Medium, false, "Privacy template applied."),
        [AuditActionTypes.PrivacyFieldViewed] = new(SecurityEventKind.DataProtection, AuditRiskLevel.Medium, false, "Sensitive data field viewed."),
        [AuditActionTypes.PrivacyViolationAttempt] = new(SecurityEventKind.DataProtection, AuditRiskLevel.High, false, "Privacy violation attempt blocked."),
        [AuditActionTypes.PrivacyBulkOperationApplied] = new(SecurityEventKind.DataProtection, AuditRiskLevel.Medium, true, "Bulk privacy operation applied."),

        // Tenant configuration
        [AuditActionTypes.TenantCreated] = new(SecurityEventKind.ConfigurationChange, AuditRiskLevel.Medium, false, "Tenant created."),
        [AuditActionTypes.TenantUpdated] = new(SecurityEventKind.ConfigurationChange, AuditRiskLevel.Medium, true, "Tenant updated."),
        [AuditActionTypes.TenantDeleted] = new(SecurityEventKind.ConfigurationChange, AuditRiskLevel.High, false, "Tenant deleted."),
        [AuditActionTypes.TenantUserAdded] = new(SecurityEventKind.Authorization, AuditRiskLevel.Medium, false, "User added to tenant."),
        [AuditActionTypes.TenantUserRemoved] = new(SecurityEventKind.Authorization, AuditRiskLevel.Medium, false, "User removed from tenant."),

        // Data movement
        [AuditActionTypes.DataExported] = new(SecurityEventKind.DataMovement, AuditRiskLevel.Medium, true, "Data exported out of the platform."),
        [AuditActionTypes.DataImported] = new(SecurityEventKind.DataMovement, AuditRiskLevel.Medium, true, "Data imported into the platform."),
        [AuditActionTypes.BulkExportThresholdExceeded] = new(SecurityEventKind.DataMovement, AuditRiskLevel.High, false, "Bulk export exceeded a configured record threshold."),
        [AuditActionTypes.CrossBorderTransferRecorded] = new(SecurityEventKind.DataMovement, AuditRiskLevel.Medium, true, "Cross-border data transfer validated and logged."),

        // Financial transaction auditing (issue #180)
        [AuditActionTypes.BillingProviderEnabled] = new(SecurityEventKind.ConfigurationChange, AuditRiskLevel.High, true, "External billing provider enabled."),
        [AuditActionTypes.BillingProviderDisabled] = new(SecurityEventKind.ConfigurationChange, AuditRiskLevel.High, true, "External billing provider disabled."),
        [AuditActionTypes.BillingProviderMigrationDryRun] = new(SecurityEventKind.ConfigurationChange, AuditRiskLevel.Medium, false, "Billing provider migration dry-run executed."),
        [AuditActionTypes.BillingWebhookSecretRotated] = new(SecurityEventKind.ConfigurationChange, AuditRiskLevel.High, true, "Billing webhook signing secret rotated."),

        // Field-level data protection (issue #186)
        [AuditActionTypes.FieldAccessRecorded] = new(SecurityEventKind.DataProtection, AuditRiskLevel.Low, false, "Field-level data access recorded."),
        [AuditActionTypes.SensitiveFieldClassified] = new(SecurityEventKind.DataProtection, AuditRiskLevel.Medium, false, "Field automatically classified as sensitive."),
        [AuditActionTypes.FieldAccessAnomalyDetected] = new(SecurityEventKind.ThreatDetection, AuditRiskLevel.High, false, "Unusual data access pattern detected."),
        [AuditActionTypes.DataSubjectRequestRecorded] = new(SecurityEventKind.DataProtection, AuditRiskLevel.Medium, false, "Data subject rights request received."),
        [AuditActionTypes.DataSubjectRequestCompleted] = new(SecurityEventKind.DataProtection, AuditRiskLevel.Medium, true, "Data subject rights request fulfilled."),
        [AuditActionTypes.DataSubjectRequestRejected] = new(SecurityEventKind.DataProtection, AuditRiskLevel.High, true, "Data subject rights request rejected."),
        [AuditActionTypes.SensitiveDataRetentionEnforced] = new(SecurityEventKind.DataProtection, AuditRiskLevel.Medium, true, "Sensitive data retention policy enforced."),
        [AuditActionTypes.ConsentGranted] = new(SecurityEventKind.DataProtection, AuditRiskLevel.Low, false, "Consent granted for a processing purpose."),
        [AuditActionTypes.ConsentRevoked] = new(SecurityEventKind.DataProtection, AuditRiskLevel.Medium, true, "Consent revoked for a processing purpose."),
        [AuditActionTypes.GdprProcessingActivityChanged] = new(SecurityEventKind.DataProtection, AuditRiskLevel.Medium, true, "GDPR Article 30 processing activity recorded or changed."),

        // Configuration and administration
        [AuditActionTypes.AdminAction] = new(SecurityEventKind.ConfigurationChange, AuditRiskLevel.High, true, "Administrative action executed."),
        [AuditActionTypes.SystemConfigChanged] = new(SecurityEventKind.ConfigurationChange, AuditRiskLevel.High, true, "System configuration changed."),
        [AuditActionTypes.BulkOperation] = new(SecurityEventKind.ConfigurationChange, AuditRiskLevel.High, true, "Bulk operation executed.")
    };

    /// <summary>
    ///     Classifies an audit event. Known action types use their explicit descriptor; unknown
    ///     action types fall back to a category-derived descriptor so every security event has a kind.
    /// </summary>
    /// <param name="actionType">Action type recorded on the audit event.</param>
    /// <param name="category">Persisted audit category, used for the fallback classification.</param>
    /// <param name="success">Outcome of the audited operation.</param>
    /// <param name="declaredRiskLevel">Risk level declared by the caller, when any.</param>
    /// <returns>The matching descriptor with the effective severity for this occurrence.</returns>
    public static ClassifiedSecurityEvent Classify(
        string actionType,
        AuditCategory category,
        bool success,
        AuditRiskLevel? declaredRiskLevel = null)
    {
        if (string.IsNullOrWhiteSpace(actionType))
        {
            return new ClassifiedSecurityEvent(
                SecurityEventKind.Other,
                EffectiveSeverity(AuditRiskLevel.Low, declaredRiskLevel),
                IsSecurityRelevant(SecurityEventKind.Other, category, EffectiveSeverity(AuditRiskLevel.Low, declaredRiskLevel)),
                "Unclassified audit event.",
                actionType ?? string.Empty);
        }

        if (Descriptors.TryGetValue(actionType, out var descriptor))
        {
            var severity = success || !descriptor.EscalateOnFailure
                ? descriptor.DefaultSeverity
                : Escalate(descriptor.DefaultSeverity);
            severity = EffectiveSeverity(severity, declaredRiskLevel);
            return new ClassifiedSecurityEvent(
                descriptor.Kind,
                severity,
                IsSecurityRelevant(descriptor.Kind, category, severity),
                descriptor.Description,
                actionType);
        }

        var fallback = ClassifyUnknown(actionType, category);
        var fallbackSeverity = EffectiveSeverity(fallback.Severity, declaredRiskLevel);
        return new ClassifiedSecurityEvent(
            fallback.Kind,
            fallbackSeverity,
            IsSecurityRelevant(fallback.Kind, category, fallbackSeverity),
            fallback.Description,
            actionType);
    }

    /// <summary>Returns every explicit descriptor in the taxonomy, ordered by kind then action type.</summary>
    public static IReadOnlyList<SecurityEventTaxonomyEntry> GetTaxonomy() =>
        Descriptors
            .OrderBy(pair => (int)pair.Value.Kind)
            .ThenBy(pair => pair.Key, StringComparer.Ordinal)
            .Select(pair => new SecurityEventTaxonomyEntry(
                pair.Key,
                pair.Value.Kind,
                pair.Value.DefaultSeverity,
                pair.Value.EscalateOnFailure,
                pair.Value.Description))
            .ToArray();

    /// <summary>The kinds considered security events regardless of severity or outcome.</summary>
    public static bool IsSecurityKind(SecurityEventKind kind) =>
        kind is SecurityEventKind.Authentication
            or SecurityEventKind.Authorization
            or SecurityEventKind.SessionManagement
            or SecurityEventKind.ThreatDetection
            or SecurityEventKind.TenantIsolation;

    private static (SecurityEventKind Kind, AuditRiskLevel Severity, string Description) ClassifyUnknown(
        string actionType,
        AuditCategory category) => category switch
    {
        AuditCategory.Authentication => (SecurityEventKind.Authentication, AuditRiskLevel.Medium, "Unclassified authentication event."),
        AuditCategory.Authorization => (SecurityEventKind.Authorization, AuditRiskLevel.Medium, "Unclassified authorization event."),
        AuditCategory.Permission => (SecurityEventKind.Authorization, AuditRiskLevel.Medium, "Unclassified permission event."),
        AuditCategory.Security => (SecurityEventKind.ThreatDetection, AuditRiskLevel.High, "Unclassified security event."),
        AuditCategory.Privacy => (SecurityEventKind.DataProtection, AuditRiskLevel.Medium, "Unclassified privacy event."),
        AuditCategory.Tenant => (SecurityEventKind.ConfigurationChange, AuditRiskLevel.Medium, "Unclassified tenant event."),
        AuditCategory.Admin => (SecurityEventKind.ConfigurationChange, AuditRiskLevel.High, "Unclassified administrative event."),
        _ => (SecurityEventKind.Other, AuditRiskLevel.Low, "Unclassified audit event.")
    };

    private static bool IsSecurityRelevant(SecurityEventKind kind, AuditCategory category, AuditRiskLevel severity) =>
        IsSecurityKind(kind)
        || severity >= AuditRiskLevel.High
        || category is AuditCategory.Security or AuditCategory.Authentication or AuditCategory.Authorization;

    private static AuditRiskLevel EffectiveSeverity(AuditRiskLevel severity, AuditRiskLevel? declaredRiskLevel) =>
        declaredRiskLevel.HasValue && declaredRiskLevel.Value > severity ? declaredRiskLevel.Value : severity;

    private static AuditRiskLevel Escalate(AuditRiskLevel severity) => severity switch
    {
        AuditRiskLevel.Low => AuditRiskLevel.Medium,
        AuditRiskLevel.Medium => AuditRiskLevel.High,
        AuditRiskLevel.High => AuditRiskLevel.Critical,
        _ => AuditRiskLevel.Critical
    };
}

/// <summary>The classification result for one audit event occurrence.</summary>
public sealed record ClassifiedSecurityEvent(
    SecurityEventKind Kind,
    AuditRiskLevel Severity,
    bool IsSecurityRelevant,
    string Description,
    string ActionType);

/// <summary>One entry of the published security event taxonomy.</summary>
public sealed record SecurityEventTaxonomyEntry(
    string ActionType,
    SecurityEventKind Kind,
    AuditRiskLevel DefaultSeverity,
    bool EscalateOnFailure,
    string Description);
