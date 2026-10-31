namespace GameGuild.Compliance.Audit;

/// <summary>
/// Common audit action types
/// </summary>
public static class AuditActionTypes
{
    // Authentication
    public const string Login = "Login";

    public const string Logout = "Logout";

    public const string LoginFailed = "LoginFailed";

    public const string MfaEnabled = "MfaEnabled";

    public const string MfaDisabled = "MfaDisabled";

    public const string MfaVerified = "MfaVerified";

    public const string MfaFailed = "MfaFailed";

    public const string PasswordChanged = "PasswordChanged";

    public const string PasswordResetRequested = "PasswordResetRequested";

    public const string PasswordReset = "PasswordReset";

    // Authorization & Permissions
    public const string PermissionGranted = "PermissionGranted";

    public const string PermissionDenied = "PermissionDenied";

    public const string PermissionRevoked = "PermissionRevoked";

    public const string PermissionChanged = "PermissionChanged";

    public const string RoleAssigned = "RoleAssigned";

    public const string RoleRevoked = "RoleRevoked";

    public const string AccessDenied = "AccessDenied";

    // User Management
    public const string UserCreated = "UserCreated";

    public const string UserUpdated = "UserUpdated";

    public const string UserDeleted = "UserDeleted";

    public const string UserSuspended = "UserSuspended";

    public const string UserReactivated = "UserReactivated";

    public const string UserProfileUpdated = "UserProfileUpdated";

    // Admin Actions
    public const string AdminAction = "AdminAction";

    public const string SystemConfigChanged = "SystemConfigChanged";

    public const string DataExported = "DataExported";

    public const string DataImported = "DataImported";

    public const string BulkOperation = "BulkOperation";

    // PII Redaction Pipeline (issue #192)
    public const string PiiRedactionRuleCreated = "PiiRedactionRuleCreated";

    public const string PiiRedactionRuleUpdated = "PiiRedactionRuleUpdated";

    public const string PiiRedactionRuleDeleted = "PiiRedactionRuleDeleted";

    public const string PiiTokenResolved = "PiiTokenResolved";

    // Session Management
    public const string SessionCreated = "SessionCreated";

    public const string SessionTerminated = "SessionTerminated";

    public const string DeviceTrusted = "DeviceTrusted";

    public const string DeviceRevoked = "DeviceRevoked";

    // Security Events
    public const string SecurityViolation = "SecurityViolation";

    public const string SecurityAlertResolved = "SecurityAlertResolved";

    public const string RateLimitExceeded = "RateLimitExceeded";

    public const string SuspiciousActivity = "SuspiciousActivity";

    public const string PolicyViolation = "PolicyViolation";

    // Webhook Security Events (provider callback verification pipeline)
    public const string WebhookSignatureFailed = "WebhookSignatureFailed";

    public const string WebhookSourceIpRejected = "WebhookSourceIpRejected";

    public const string WebhookReplayDetected = "WebhookReplayDetected";

    public const string WebhookRateLimitExceeded = "WebhookRateLimitExceeded";

    public const string WebhookSourceBlocked = "WebhookSourceBlocked";

    // Billing webhook delivery dead-lettering after exhausted retries (issue #408)
    public const string WebhookDeadLettered = "WebhookDeadLettered";

    // Billing webhook lifecycle entries (issue #408: an audit-module entry for every
    // webhook event). Informational operational trail complementing the security events
    // above: every accepted delivery is recorded, and every terminal outcome (processed
    // or failed attempt) is journaled through the same audit pipeline entry point.
    public const string WebhookEventReceived = "WebhookEventReceived";

    public const string WebhookEventProcessed = "WebhookEventProcessed";

    public const string WebhookEventFailed = "WebhookEventFailed";

    // External Billing Provider Management (issue #397 provider-management API)
    public const string BillingProviderEnabled = "BillingProviderEnabled";

    public const string BillingProviderDisabled = "BillingProviderDisabled";

    public const string BillingProviderMigrationDryRun = "BillingProviderMigrationDryRun";

    // Billing webhook signing-secret rotation (issue #408)
    public const string BillingWebhookSecretRotated = "BillingWebhookSecretRotated";

    // Promo engine unusual-usage anomaly (issue #418 anti-farming alerting)
    public const string PromoUsageAnomalyDetected = "PromoUsageAnomalyDetected";

    // Tenant Operations
    public const string TenantCreated = "TenantCreated";

    public const string TenantUpdated = "TenantUpdated";

    public const string TenantDeleted = "TenantDeleted";

    public const string TenantIsolationBypassed = "TenantIsolationBypassed";

    public const string TenantUserAdded = "TenantUserAdded";

    public const string TenantUserRemoved = "TenantUserRemoved";

    // Privacy Operations
    public const string PrivacySettingsUpdated = "PrivacySettingsUpdated";

    public const string PrivacyTemplateApplied = "PrivacyTemplateApplied";

    public const string PrivacyFieldViewed = "PrivacyFieldViewed";

    public const string PrivacyViolationAttempt = "PrivacyViolationAttempt";

    public const string PrivacyBulkOperationApplied = "PrivacyBulkOperationApplied";

    // Username Operations
    public const string UsernameNormalized = "UsernameNormalized";

    public const string UsernameCollisionResolved = "UsernameCollisionResolved";

    // Federated identity / IdP integration auditing (issue #180)
    public const string ExternalLoginSucceeded = "Authentication.ExternalLoginSucceeded";

    public const string ExternalLoginFailed = "Authentication.ExternalLoginFailed";

    public const string ExternalAccountLinked = "Authentication.ExternalAccountLinked";

    public const string ExternalAccountProvisioned = "Authentication.ExternalAccountProvisioned";

    public const string FederatedSignInMfaDenied = "Authentication.FederatedSignInMfaDenied";

    // Learning lifecycle auditing (issue #180)
    public const string CertificateVerified = "Learning.Certificates.CertificateVerified";

    public const string CertificateRenewed = "Learning.Certificates.CertificateRenewed";

    public const string EnrollmentCompleted = "Learning.Enrollments.EnrollmentCompleted";

    // Field-level data access auditing (issue #186)
    public const string FieldAccessRecorded = "DataProtection.FieldAccessRecorded";

    public const string ConsentGranted = "Consent.Granted";

    public const string ConsentRevoked = "Consent.Revoked";

    public const string SensitiveFieldClassified = "DataProtection.SensitiveFieldClassified";

    public const string FieldAccessAnomalyDetected = "DataProtection.FieldAccessAnomalyDetected";

    public const string BulkExportThresholdExceeded = "DataProtection.BulkExportThresholdExceeded";

    public const string GdprProcessingActivityChanged = "DataProtection.GdprProcessingActivityChanged";

    public const string DataSubjectRequestRecorded = "DataProtection.DataSubjectRequestRecorded";

    public const string DataSubjectRequestCompleted = "DataProtection.DataSubjectRequestCompleted";

    public const string DataSubjectRequestRejected = "DataProtection.DataSubjectRequestRejected";

    public const string CrossBorderTransferRecorded = "DataProtection.CrossBorderTransferRecorded";

    public const string SensitiveDataRetentionEnforced = "DataProtection.SensitiveDataRetentionEnforced";
}
