using GameGuild.Identity.Authentication;
using Microsoft.Extensions.Logging;

namespace GameGuild.Compliance.Audit;

/// <summary>
/// Bridges identity authentication events to the central compliance audit log.
/// </summary>
public sealed class CentralAuthenticationAuditEventSink(
    IAuditService auditService,
    ILogger<CentralAuthenticationAuditEventSink> logger) : IAuthenticationAuditEventSink
{
    public async Task RecordAsync(AuthenticationAuditEvent auditEvent, CancellationToken cancellationToken)
    {
        try
        {
            await auditService.LogAsync(new CreateAuditLogRequest
            {
                ActionType = auditEvent.ActionType,
                ResourceType = "User",
                ResourceId = auditEvent.UserId?.ToString(),
                UserId = auditEvent.UserId,
                TenantId = auditEvent.TenantId,
                IpAddress = auditEvent.IpAddress,
                UserAgent = auditEvent.UserAgent,
                SessionId = auditEvent.SessionId,
                Description = $"Authentication event using {auditEvent.Method}",
                Metadata = new { AuthenticationMethod = auditEvent.Method, Details = auditEvent.Metadata },
                Success = auditEvent.Success,
                ErrorMessage = auditEvent.ErrorMessage,
                RiskLevel = auditEvent.AssessedRiskLevel switch
                {
                    RiskLevel.Low => AuditRiskLevel.Low,
                    RiskLevel.Medium => AuditRiskLevel.Medium,
                    RiskLevel.High => AuditRiskLevel.High,
                    RiskLevel.Critical => AuditRiskLevel.Critical,
                    _ => auditEvent.Success ? AuditRiskLevel.Low : AuditRiskLevel.High
                },
                Category = AuditCategory.Authentication
            }).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            // Audit transport must not make a login, MFA, or session operation fail.
            logger.LogError(exception, "Could not forward authentication event {ActionType} to the audit log", auditEvent.ActionType);
        }
    }
}
