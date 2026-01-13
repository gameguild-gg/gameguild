using System.Text;
using GameGuild.Abstractions;
using GameGuild.Identity.Authentication;
using GameGuild.Identity.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GameGuild.Compliance.Audit;

/// <summary>
///     Implementation of ISecurityAuditAggregator that queries authentication attempts,
///     permission audit logs, and general audit logs to provide unified security monitoring.
/// </summary>
public class SecurityAuditAggregator : ISecurityAuditAggregator
{
    private readonly IApplicationDbContext _context;
    private readonly IPermissionAuditLogRepository _permissionAuditRepository;
    private readonly ILogger<SecurityAuditAggregator> _logger;

    public SecurityAuditAggregator(
        IApplicationDbContext context,
        IPermissionAuditLogRepository permissionAuditRepository,
        ILogger<SecurityAuditAggregator> logger)
    {
        _context = context;
        _permissionAuditRepository = permissionAuditRepository;
        _logger = logger;
    }

    public async Task<UnifiedSecurityAuditResponse> GetUnifiedAuditLogsAsync(
        UnifiedSecurityAuditRequest request,
        CancellationToken cancellationToken = default)
    {
        var entries = new List<UnifiedSecurityAuditEntry>();

        // Determine date range
        var startDate = request.StartDate ?? DateTime.UtcNow.AddDays(-30);
        var endDate = request.EndDate ?? DateTime.UtcNow;

        // Fetch from different sources based on SourceType filter
        if (request.SourceType is null or SecurityAuditSourceType.All or SecurityAuditSourceType.Authentication)
        {
            var authLogs = await GetAuthenticationEntriesAsync(startDate, endDate, request.UserId, request.Success, request.IpAddress, cancellationToken);
            entries.AddRange(authLogs);
        }

        if (request.SourceType is null or SecurityAuditSourceType.All or SecurityAuditSourceType.Permission)
        {
            var permLogs = await GetPermissionEntriesAsync(startDate, endDate, request.TenantId, request.UserId, request.Success, cancellationToken);
            entries.AddRange(permLogs);
        }

        if (request.SourceType is null or SecurityAuditSourceType.All or SecurityAuditSourceType.General)
        {
            var generalLogs = await GetGeneralEntriesAsync(startDate, endDate, request.TenantId, request.UserId, request.Success, request.ActionType, request.RiskLevel, cancellationToken);
            entries.AddRange(generalLogs);
        }

        // Apply search filter
        if (!string.IsNullOrWhiteSpace(request.SearchText))
        {
            entries = entries.Where(e =>
                (e.Description?.Contains(request.SearchText, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (e.ActionType.Contains(request.SearchText, StringComparison.OrdinalIgnoreCase)) ||
                (e.ResourceType?.Contains(request.SearchText, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (e.IpAddress?.Contains(request.SearchText, StringComparison.OrdinalIgnoreCase) ?? false)
            ).ToList();
        }

        // Sort
        entries = request.SortDirection.Equals("asc", StringComparison.OrdinalIgnoreCase)
            ? entries.OrderBy(e => e.Timestamp).ToList()
            : entries.OrderByDescending(e => e.Timestamp).ToList();

        // Calculate source breakdown before pagination
        var sourceBreakdown = entries
            .GroupBy(e => e.SourceType)
            .ToDictionary(g => g.Key, g => g.Count());

        // Pagination
        var totalCount = entries.Count;
        var pagedEntries = entries
            .Skip(request.Skip)
            .Take(request.Take)
            .ToList();

        return new UnifiedSecurityAuditResponse
        {
            Entries = pagedEntries,
            TotalCount = totalCount,
            Skip = request.Skip,
            Take = pagedEntries.Count,
            SourceBreakdown = sourceBreakdown
        };
    }

    public async Task<AuthenticationAuditResponse> GetAuthenticationLogsAsync(
        AuthenticationAuditRequest request,
        CancellationToken cancellationToken = default)
    {
        var startDate = request.StartDate ?? DateTime.UtcNow.AddDays(-30);
        var endDate = request.EndDate ?? DateTime.UtcNow;

        var query = _context.Set<AuthenticationAttempt>().AsNoTracking()
            .Where(a => a.AttemptedAt >= startDate && a.AttemptedAt <= endDate);

        if (request.UserId.HasValue)
            query = query.Where(a => a.UserId == request.UserId.Value);

        if (!string.IsNullOrEmpty(request.Email))
            query = query.Where(a => a.Email == request.Email);

        if (!string.IsNullOrEmpty(request.IpAddress))
            query = query.Where(a => a.IpAddress == request.IpAddress);

        if (request.Success.HasValue)
            query = query.Where(a => a.IsSuccessful == request.Success.Value);

        if (!string.IsNullOrEmpty(request.FailureReason))
            query = query.Where(a => a.FailureReason != null && a.FailureReason.Contains(request.FailureReason));

        var totalCount = await query.CountAsync(cancellationToken);
        var successfulLogins = await query.CountAsync(a => a.IsSuccessful, cancellationToken);
        var uniqueIps = await query.Select(a => a.IpAddress).Distinct().CountAsync(cancellationToken);

        var attempts = await query
            .OrderByDescending(a => a.AttemptedAt)
            .Skip(request.Skip)
            .Take(request.Take)
            .ToListAsync(cancellationToken);

        return new AuthenticationAuditResponse
        {
            Entries = attempts.Select(a => new AuthenticationAuditEntry
            {
                Id = a.Id,
                Email = a.Email ?? string.Empty,
                UserId = a.UserId,
                IpAddress = a.IpAddress ?? string.Empty,
                UserAgent = a.UserAgent,
                IsSuccessful = a.IsSuccessful,
                FailureReason = a.FailureReason,
                AttemptedAt = a.AttemptedAt,
                ProcessingTime = a.ProcessingTime,
                GeoLocation = null,
                IsSuspicious = false
            }).ToList(),
            TotalCount = totalCount,
            Skip = request.Skip,
            Take = attempts.Count,
            SuccessfulLogins = successfulLogins,
            FailedLogins = totalCount - successfulLogins,
            UniqueIpAddresses = uniqueIps
        };
    }

    public async Task<PermissionAuditResponse> GetPermissionLogsAsync(
        PermissionAuditRequest request,
        CancellationToken cancellationToken = default)
    {
        var startDate = request.StartDate ?? DateTime.UtcNow.AddDays(-30);
        var endDate = request.EndDate ?? DateTime.UtcNow;

        var logs = await _permissionAuditRepository.GetByDateRangeAsync(
            startDate, endDate, request.TenantId, cancellationToken);

        // Apply additional filters
        if (request.UserId.HasValue)
            logs = logs.Where(l => l.UserId == request.UserId.Value).ToList();

        if (!string.IsNullOrEmpty(request.OperationType) && Enum.TryParse<PermissionOperationType>(request.OperationType, true, out var opType))
            logs = logs.Where(l => l.OperationType == opType).ToList();

        if (!string.IsNullOrEmpty(request.PermissionType))
            logs = logs.Where(l => l.PermissionType == request.PermissionType).ToList();

        if (!string.IsNullOrEmpty(request.ResourceType))
            logs = logs.Where(l => l.ResourceType == request.ResourceType).ToList();

        if (request.Success.HasValue)
            logs = logs.Where(l => l.Success == request.Success.Value).ToList();

        var totalCount = logs.Count;
        var grantOps = logs.Count(l => l.OperationType == PermissionOperationType.Grant);
        var revokeOps = logs.Count(l => l.OperationType == PermissionOperationType.Revoke);
        var denyOps = logs.Count(l => !l.Success);

        var pagedLogs = logs
            .Skip(request.Skip)
            .Take(request.Take)
            .ToList();

        return new PermissionAuditResponse
        {
            Entries = pagedLogs.Select(l => new PermissionAuditEntry
            {
                Id = l.Id,
                TenantId = l.TenantId?.Value,
                OperationType = l.OperationType.ToString(),
                UserId = l.UserId,
                ResourceId = l.ResourceId,
                ResourceType = l.ResourceType,
                PermissionType = l.PermissionType,
                OldValue = l.OldValue,
                NewValue = l.NewValue,
                PerformedBy = l.PerformedBy,
                IpAddress = null,
                Reason = null,
                Success = l.Success,
                ErrorMessage = null,
                Timestamp = l.Timestamp
            }).ToList(),
            TotalCount = totalCount,
            Skip = request.Skip,
            Take = pagedLogs.Count,
            GrantOperations = grantOps,
            RevokeOperations = revokeOps,
            DenyOperations = denyOps
        };
    }

    public async Task<SecurityAuditDashboard> GetSecurityDashboardAsync(
        DateTime startDate,
        DateTime endDate,
        Guid? tenantId = null,
        CancellationToken cancellationToken = default)
    {
        // Authentication statistics
        var authQuery = _context.Set<AuthenticationAttempt>().AsNoTracking()
            .Where(a => a.AttemptedAt >= startDate && a.AttemptedAt <= endDate);

        var totalLoginAttempts = await authQuery.CountAsync(cancellationToken);
        var successfulLogins = await authQuery.CountAsync(a => a.IsSuccessful, cancellationToken);
        var failedLogins = totalLoginAttempts - successfulLogins;
        var loginSuccessRate = totalLoginAttempts > 0 ? (successfulLogins * 100.0 / totalLoginAttempts) : 0;
        var uniqueUsersAuth = await authQuery.Where(a => a.UserId.HasValue).Select(a => a.UserId).Distinct().CountAsync(cancellationToken);

        // Permission statistics
        var permLogs = await _permissionAuditRepository.GetByDateRangeAsync(startDate, endDate, tenantId, cancellationToken);
        var totalPermissionChanges = permLogs.Count;
        var permissionGrants = permLogs.Count(l => l.OperationType == PermissionOperationType.Grant);
        var permissionRevokes = permLogs.Count(l => l.OperationType == PermissionOperationType.Revoke);
        var permissionDenials = permLogs.Count(l => !l.Success);

        // General audit statistics
        var auditQuery = _context.Set<AuditLog>().AsNoTracking()
            .Where(a => a.CreatedAt >= startDate && a.CreatedAt <= endDate);

        if (tenantId.HasValue)
            auditQuery = auditQuery.Where(a => a.TenantId == tenantId.Value);

        var securityViolations = await auditQuery
            .CountAsync(a => a.RiskLevel >= AuditRiskLevel.High || !a.Success, cancellationToken);
        var highRiskEvents = await auditQuery
            .CountAsync(a => a.RiskLevel >= AuditRiskLevel.High, cancellationToken);

        // Top users by activity
        var topUsers = await authQuery
            .Where(a => a.UserId.HasValue)
            .GroupBy(a => a.UserId!.Value)
            .Select(g => new TopUserActivity
            {
                UserId = g.Key,
                Email = g.Select(a => a.Email).FirstOrDefault(),
                EventCount = g.Count(),
                FailedAttempts = g.Count(a => !a.IsSuccessful)
            })
            .OrderByDescending(u => u.EventCount)
            .Take(10)
            .ToListAsync(cancellationToken);

        // Top IPs by activity
        var topIps = await authQuery
            .Where(a => !string.IsNullOrEmpty(a.IpAddress))
            .GroupBy(a => a.IpAddress!)
            .Select(g => new TopIpActivity
            {
                IpAddress = g.Key,
                EventCount = g.Count(),
                FailedAttempts = g.Count(a => !a.IsSuccessful),
                UniqueUsers = g.Where(a => a.UserId.HasValue).Select(a => a.UserId).Distinct().Count()
            })
            .OrderByDescending(ip => ip.EventCount)
            .Take(10)
            .ToListAsync(cancellationToken);

        // Failure reasons breakdown
        var failureReasons = await authQuery
            .Where(a => !a.IsSuccessful && !string.IsNullOrEmpty(a.FailureReason))
            .GroupBy(a => a.FailureReason!)
            .Select(g => new FailureReasonCount
            {
                Reason = g.Key,
                Count = g.Count()
            })
            .OrderByDescending(f => f.Count)
            .Take(10)
            .ToListAsync(cancellationToken);

        // Daily trends
        var dailyTrends = await authQuery
            .GroupBy(a => a.AttemptedAt.Date)
            .Select(g => new DailyActivityTrend
            {
                Date = g.Key,
                AuthenticationEvents = g.Count(),
                TotalEvents = g.Count(),
                PermissionEvents = 0,
                SecurityViolations = g.Count(a => !a.IsSuccessful)
            })
            .OrderBy(d => d.Date)
            .ToListAsync(cancellationToken);

        return new SecurityAuditDashboard
        {
            StartDate = startDate,
            EndDate = endDate,
            TenantId = tenantId,

            // Authentication
            TotalAuthenticationAttempts = totalLoginAttempts,
            SuccessfulLogins = successfulLogins,
            FailedLogins = failedLogins,
            LoginSuccessRate = Math.Round(loginSuccessRate, 2),
            UniqueUsersAuthenticated = uniqueUsersAuth,
            SuspiciousLoginAttempts = 0,

            // Permissions
            TotalPermissionChanges = totalPermissionChanges,
            PermissionsGranted = permissionGrants,
            PermissionsRevoked = permissionRevokes,
            PermissionDenials = permissionDenials,

            // General
            TotalSecurityViolations = securityViolations,
            HighRiskEvents = highRiskEvents,
            CrossTenantAttempts = 0,

            // Breakdowns
            TopActiveUsers = topUsers,
            TopIpAddresses = topIps,
            TopFailureReasons = failureReasons,
            DailyTrends = dailyTrends
        };
    }

    public async Task<byte[]> ExportAuditLogsAsync(
        UnifiedSecurityAuditRequest request,
        CancellationToken cancellationToken = default)
    {
        // Override pagination to get all records for export
        request.Skip = 0;
        request.Take = 10000; // Max export limit

        var response = await GetUnifiedAuditLogsAsync(request, cancellationToken);

        var sb = new StringBuilder();
        sb.AppendLine("Timestamp,SourceType,ActionType,ResourceType,ResourceId,UserId,IpAddress,Success,Description");

        foreach (var entry in response.Entries)
        {
            sb.AppendLine($"\"{entry.Timestamp:yyyy-MM-dd HH:mm:ss}\"," +
                         $"\"{entry.SourceType}\"," +
                         $"\"{EscapeCsv(entry.ActionType)}\"," +
                         $"\"{EscapeCsv(entry.ResourceType)}\"," +
                         $"\"{EscapeCsv(entry.ResourceId)}\"," +
                         $"\"{entry.UserId}\"," +
                         $"\"{EscapeCsv(entry.IpAddress)}\"," +
                         $"\"{entry.Success}\"," +
                         $"\"{EscapeCsv(entry.Description)}\"");
        }

        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    #region Private Helper Methods

    private async Task<List<UnifiedSecurityAuditEntry>> GetAuthenticationEntriesAsync(
        DateTime startDate, DateTime endDate, Guid? userId, bool? successOnly, string? ipAddress,
        CancellationToken cancellationToken)
    {
        var query = _context.Set<AuthenticationAttempt>().AsNoTracking()
            .Where(a => a.AttemptedAt >= startDate && a.AttemptedAt <= endDate);

        if (userId.HasValue)
            query = query.Where(a => a.UserId == userId.Value);

        if (successOnly.HasValue)
            query = query.Where(a => a.IsSuccessful == successOnly.Value);

        if (!string.IsNullOrEmpty(ipAddress))
            query = query.Where(a => a.IpAddress == ipAddress);

        var attempts = await query.ToListAsync(cancellationToken);

        return attempts.Select(a => new UnifiedSecurityAuditEntry
        {
            Id = a.Id,
            Timestamp = a.AttemptedAt,
            SourceType = SecurityAuditSourceType.Authentication,
            SourceEntity = "AuthenticationAttempt",
            ActionType = a.IsSuccessful ? "Login" : "LoginFailed",
            ResourceType = "User",
            ResourceId = a.UserId?.ToString(),
            UserId = a.UserId,
            UserEmail = a.Email,
            TenantId = null,
            IpAddress = a.IpAddress,
            UserAgent = a.UserAgent,
            Success = a.IsSuccessful,
            ErrorMessage = a.FailureReason,
            Description = a.IsSuccessful
                ? $"Successful login for {a.Email}"
                : $"Failed login for {a.Email}: {a.FailureReason}",
            RiskLevel = a.IsSuccessful ? AuditRiskLevel.Low : AuditRiskLevel.Medium,
            Metadata = null
        }).ToList();
    }

    private async Task<List<UnifiedSecurityAuditEntry>> GetPermissionEntriesAsync(
        DateTime startDate, DateTime endDate, Guid? tenantId, Guid? userId, bool? successOnly,
        CancellationToken cancellationToken)
    {
        var logs = await _permissionAuditRepository.GetByDateRangeAsync(startDate, endDate, tenantId, cancellationToken);

        if (userId.HasValue)
            logs = logs.Where(l => l.UserId == userId.Value).ToList();

        if (successOnly.HasValue)
            logs = logs.Where(l => l.Success == successOnly.Value).ToList();

        return logs.Select(l => new UnifiedSecurityAuditEntry
        {
            Id = l.Id,
            Timestamp = l.Timestamp,
            SourceType = SecurityAuditSourceType.Permission,
            SourceEntity = "PermissionAuditLog",
            ActionType = l.OperationType.ToString(),
            ResourceType = l.ResourceType ?? "Permission",
            ResourceId = l.ResourceId?.ToString(),
            UserId = l.UserId,
            UserEmail = null,
            TenantId = l.TenantId?.Value,
            IpAddress = null,
            UserAgent = null,
            Success = l.Success,
            ErrorMessage = null,
            Description = $"{l.OperationType} permission '{l.PermissionType}' for user {l.UserId}",
            RiskLevel = l.Success ? AuditRiskLevel.Low : AuditRiskLevel.Medium,
            Metadata = null
        }).ToList();
    }

    private async Task<List<UnifiedSecurityAuditEntry>> GetGeneralEntriesAsync(
        DateTime startDate, DateTime endDate, Guid? tenantId, Guid? userId, bool? successOnly,
        string? actionType, AuditRiskLevel? riskLevel,
        CancellationToken cancellationToken)
    {
        var query = _context.Set<AuditLog>().AsNoTracking()
            .Where(a => a.CreatedAt >= startDate && a.CreatedAt <= endDate);

        if (tenantId.HasValue)
            query = query.Where(a => a.TenantId == tenantId.Value);

        if (userId.HasValue)
            query = query.Where(a => a.UserId == userId.Value);

        if (successOnly.HasValue)
            query = query.Where(a => a.Success == successOnly.Value);

        if (!string.IsNullOrEmpty(actionType))
            query = query.Where(a => a.ActionType == actionType);

        if (riskLevel.HasValue)
            query = query.Where(a => a.RiskLevel == riskLevel.Value);

        var logs = await query.ToListAsync(cancellationToken);

        return logs.Select(l => new UnifiedSecurityAuditEntry
        {
            Id = l.Id,
            Timestamp = l.CreatedAt,
            SourceType = SecurityAuditSourceType.General,
            SourceEntity = "AuditLog",
            ActionType = l.ActionType,
            ResourceType = l.ResourceType,
            ResourceId = l.ResourceId,
            UserId = l.UserId,
            UserEmail = null,
            TenantId = l.TenantId,
            IpAddress = l.IpAddress,
            UserAgent = l.UserAgent,
            Success = l.Success,
            ErrorMessage = l.ErrorMessage,
            Description = l.Description,
            RiskLevel = l.RiskLevel,
            Metadata = l.Metadata
        }).ToList();
    }

    private static string? EscapeCsv(string? value)
    {
        if (string.IsNullOrEmpty(value)) return value;
        return value.Replace("\"", "\"\"");
    }

    #endregion
}
