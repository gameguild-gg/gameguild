namespace GameGuild.Identity.Authentication;

/// <summary>Security-stage events contain server-owned identifiers and never recovery bearers.</summary>
internal static class MfaRecoveryAudit
{
    internal const string Requested = "Authentication.MfaRecoveryRequested";
    internal const string Continued = "Authentication.MfaRecoveryContinued";
    internal const string Confirmed = "Authentication.MfaRecoveryConfirmed";
    internal const string Cancelled = "Authentication.MfaRecoveryCancelled";

    internal static Task RecordAsync(IAuthenticationAuditEventSink sink, MfaRecoveryRequest request,
        string action, string reason, CancellationToken cancellationToken)
    {
        var tenantId = Guid.TryParse(request.TenantId, out var parsed) && parsed != Guid.Empty ? parsed : (Guid?)null;
        return sink.RecordAsync(new AuthenticationAuditEvent(action, Guid.Parse(request.UserId), true, "MfaRecovery",
            TenantId: tenantId, Metadata: new { RecoveryRequestId = request.Id, Reason = reason },
            AssessedRiskLevel: action == Confirmed ? RiskLevel.High : RiskLevel.Medium), cancellationToken);
    }
}
