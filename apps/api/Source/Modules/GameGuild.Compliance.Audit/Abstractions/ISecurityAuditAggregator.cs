namespace GameGuild.Compliance.Audit;

/// <summary>
///     Service that aggregates audit logs from multiple sources
///     (AuthenticationAttempt, PermissionAuditLog, AuditLog) into a unified view.
/// </summary>
public interface ISecurityAuditAggregator
{
    /// <summary>
    ///     Get unified audit logs from all sources.
    /// </summary>
    Task<UnifiedSecurityAuditResponse> GetUnifiedAuditLogsAsync(
        UnifiedSecurityAuditRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Get authentication-specific audit logs.
    /// </summary>
    Task<AuthenticationAuditResponse> GetAuthenticationLogsAsync(
        AuthenticationAuditRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Get permission-specific audit logs.
    /// </summary>
    Task<PermissionAuditResponse> GetPermissionLogsAsync(
        PermissionAuditRequest request,
        CancellationToken cancellationToken = default);

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
