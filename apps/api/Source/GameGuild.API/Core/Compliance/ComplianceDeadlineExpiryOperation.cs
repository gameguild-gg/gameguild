namespace GameGuild.API.Core.Compliance;

/// <summary>
///     Outcome of one statutory-deadline sweep: how many data subject requests
///     (GDPR 30-day) and FERPA inspection requests (45-day) transitioned to Expired.
/// </summary>
public sealed record ComplianceDeadlineExpiryResult(int DataSubjectRequestsExpired, int FerpaInspectionRequestsExpired)
{
    public int TotalExpired => DataSubjectRequestsExpired + FerpaInspectionRequestsExpired;
}

/// <summary>
///     Runs one bounded expiry pass over both compliance deadline machines
///     (issue #1219 N3). Exposed as a scoped service so integration tests can drive
///     the exact transition the background worker performs periodically.
/// </summary>
public interface IComplianceDeadlineExpiryOperation
{
    Task<ComplianceDeadlineExpiryResult> RunAsync(CancellationToken cancellationToken = default);
}

public sealed class ComplianceDeadlineExpiryOperation(
    GameGuild.Compliance.Consent.IConsentService consentService,
    GameGuild.Compliance.FERPA.IFerpaService ferpaService) : IComplianceDeadlineExpiryOperation
{
    public async Task<ComplianceDeadlineExpiryResult> RunAsync(CancellationToken cancellationToken = default)
    {
        var utcNow = DateTime.UtcNow;
        var dsrExpired = await consentService.ExpireOverdueDataSubjectRequestsAsync(utcNow, cancellationToken).ConfigureAwait(false);
        var ferpaExpired = await ferpaService.ExpireOverdueInspectionRequestsAsync(utcNow, cancellationToken).ConfigureAwait(false);
        return new ComplianceDeadlineExpiryResult(dsrExpired, ferpaExpired);
    }
}
