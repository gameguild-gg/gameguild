using GameGuild.Compliance.Consent;
using Microsoft.Extensions.Logging;

namespace GameGuild.Compliance.Audit;

/// <summary>
/// Bridges consent and data-subject-request lifecycle events to the central audit log (issue #186
/// data subject access tracking with consent monitoring). Consent events are privacy events: they are
/// classified by the security event taxonomy and persisted through the durable security event
/// pipeline with spool fallback. A capture failure must never break the consent flow itself, so
/// failures are logged and surfaced through the pipeline status instead of propagating.
/// </summary>
public sealed class CentralConsentAuditEventSink(
    ISecurityEventLogger securityEventLogger,
    ILogger<CentralConsentAuditEventSink> logger) : IConsentAuditEventSink
{
    public async Task RecordAsync(ConsentAuditEvent auditEvent, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(auditEvent);
        try
        {
            var result = await securityEventLogger.RecordAsync(new CreateAuditLogRequest
            {
                ActionType = auditEvent.ActionType,
                ResourceType = "UserConsent",
                ResourceId = auditEvent.SubjectUserId?.ToString(),
                UserId = auditEvent.SubjectUserId,
                TenantId = auditEvent.TenantId,
                Description = auditEvent.Description,
                Metadata = auditEvent.Metadata,
                Success = auditEvent.Success,
                ErrorMessage = auditEvent.ErrorMessage,
                RiskLevel = auditEvent.Success ? AuditRiskLevel.Low : AuditRiskLevel.Medium,
                Category = AuditCategory.Privacy
            }, cancellationToken).ConfigureAwait(false);

            if (result.Outcome is SecurityEventCaptureOutcome.SpooledLocally or SecurityEventCaptureOutcome.SpoolingDisabled)
            {
                logger.LogWarning(
                    "Consent event {ActionType} was not persisted directly (outcome {Outcome}); capture error: {CaptureError}",
                    auditEvent.ActionType, result.Outcome, result.CaptureError);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Could not forward consent event {ActionType} to the security event pipeline", auditEvent.ActionType);
        }
    }
}
