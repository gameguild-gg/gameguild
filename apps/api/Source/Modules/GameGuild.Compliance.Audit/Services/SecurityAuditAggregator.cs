namespace GameGuild.Compliance.Audit;

/// <summary>
///     Thin facade that delegates to <see cref="IAuditLogQueryService"/> and
///     <see cref="IAuditReportService"/> for backward compatibility.
/// </summary>
public class SecurityAuditAggregator(
    IAuditLogQueryService auditLogQueryService,
    IAuditReportService auditReportService) : ISecurityAuditAggregator
{
    public Task<UnifiedSecurityAuditResponse> GetUnifiedAuditLogsAsync(
        UnifiedSecurityAuditRequest request,
        CancellationToken cancellationToken = default)
        => auditLogQueryService.GetUnifiedAuditLogsAsync(request, cancellationToken);

    public Task<AuthenticationAuditResponse> GetAuthenticationLogsAsync(
        AuthenticationAuditRequest request,
        CancellationToken cancellationToken = default)
        => auditLogQueryService.GetAuthenticationLogsAsync(request, cancellationToken);

    public Task<PermissionAuditResponse> GetPermissionLogsAsync(
        PermissionAuditRequest request,
        CancellationToken cancellationToken = default)
        => auditLogQueryService.GetPermissionLogsAsync(request, cancellationToken);

    public Task<SecurityAuditDashboard> GetSecurityDashboardAsync(
        DateTime startDate,
        DateTime endDate,
        Guid? tenantId = null,
        CancellationToken cancellationToken = default)
        => auditReportService.GetSecurityDashboardAsync(startDate, endDate, tenantId, cancellationToken);

    public Task<byte[]> ExportAuditLogsAsync(
        UnifiedSecurityAuditRequest request,
        CancellationToken cancellationToken = default)
        => auditReportService.ExportAuditLogsAsync(request, cancellationToken);
}
