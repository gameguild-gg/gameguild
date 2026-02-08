namespace GameGuild.Compliance.Audit;

/// <summary>
///     Service responsible for generating security audit dashboards and exporting audit data.
/// </summary>
public interface IAuditReportService
{
    /// <summary>
    ///     Get security dashboard with aggregated statistics.
    /// </summary>
    Task<SecurityAuditDashboard> GetSecurityDashboardAsync(
        DateTime startDate,
        DateTime endDate,
        Guid? tenantId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Export audit logs to CSV format.
    /// </summary>
    Task<byte[]> ExportAuditLogsAsync(
        UnifiedSecurityAuditRequest request,
        CancellationToken cancellationToken = default);
}
