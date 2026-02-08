namespace GameGuild.Compliance.Audit;

/// <summary>
///     Service responsible for querying and collecting audit log entries
///     from multiple sources (authentication, permission, general).
/// </summary>
public interface IAuditLogQueryService
{
    /// <summary>
    ///     Get unified audit logs from all sources with filtering, search, and pagination.
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
}
