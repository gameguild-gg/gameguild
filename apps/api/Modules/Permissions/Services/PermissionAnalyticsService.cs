using System.Text.Json;
using GameGuild.Database;
using GameGuild.Modules.Permissions.Abstractions;
using GameGuild.Modules.Permissions.Models;

namespace GameGuild.Modules.Permissions.Services;

/// <summary>
/// Service for analyzing permission usage patterns and generating insights
/// </summary>
public class PermissionAnalyticsService : IPermissionAnalyticsService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<PermissionAnalyticsService> _logger;

    public PermissionAnalyticsService(
        ApplicationDbContext context,
        ILogger<PermissionAnalyticsService> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<PermissionUsageReport> GetUsageReportAsync(
        Guid tenantId,
        DateTimeRange period,
        bool includeSecurityAnalysis = true)
    {
        _logger.LogInformation("Generating permission usage report for Tenant:{TenantId}, Period:{Period}",
            tenantId, $"{period.StartDate:yyyy-MM-dd} to {period.EndDate:yyyy-MM-dd}");

        var report = new PermissionUsageReport
        {
            Period = period,
            GeneratedAt = DateTime.UtcNow
        };

        // Get most used permissions
        report.MostUsedPermissions = await GetMostUsedPermissionsAsync(tenantId, period);

        // Get denied attempts
        report.DeniedAttempts = await GetDeniedAttemptsAsync(tenantId, period);

        // Get permission changes
        report.PermissionChanges = await GetPermissionChangesAsync(tenantId, period);

        // Get unused permissions
        report.UnusedPermissions = await GetUnusedPermissionsAsync(tenantId, period);

        // Get most active users
        report.MostActiveUsers = await GetMostActiveUsersAsync(tenantId, period);

        // Get usage patterns
        report.UsagePatterns = await GetUsagePatternsAsync(tenantId, period);

        // Get security report if requested
        if (includeSecurityAnalysis)
        {
            report.SecurityReport = await GetSecurityAnalysisAsync(tenantId, period);
        }

        _logger.LogInformation("Completed permission usage report generation for Tenant:{TenantId}", tenantId);

        return report;
    }

    public async Task<IEnumerable<PermissionUsageStatistic>> GetPermissionTrendsAsync(
        Guid tenantId,
        PermissionType? permission = null,
        DateTimeRange? period = null)
    {
        var query = _context.PermissionAuditLogs
            .Where(log => log.TenantId == tenantId && log.Operation == "Check");

        if (permission.HasValue)
            query = query.Where(log => log.Permissions.Contains(permission.Value));

        if (period != null)
            query = query.Where(log => log.PerformedAt >= period.StartDate && log.PerformedAt <= period.EndDate);

        var trends = await query
            .GroupBy(log => log.Permissions.FirstOrDefault())
            .Select(g => new PermissionUsageStatistic
            {
                Permission = g.Key,
                UsageCount = g.Count(),
                UniqueUsers = g.Select(log => log.UserId).Distinct().Count(),
                FirstUsed = g.Min(log => log.PerformedAt),
                LastUsed = g.Max(log => log.PerformedAt),
                AverageUsagePerUser = g.Count() / (double)g.Select(log => log.UserId).Distinct().Count(),
                PermissionLayer = g.First().PermissionLayer ?? "Unknown"
            })
            .OrderByDescending(stat => stat.UsageCount)
            .ToListAsync();

        return trends;
    }

    public async Task<IEnumerable<UserPermissionActivity>> GetAnomalousUsersAsync(
        Guid tenantId,
        DateTimeRange period,
        double suspiciousThreshold = 0.3)
    {
        var userActivities = await GetMostActiveUsersAsync(tenantId, period);

        // Identify users with unusually high denial rates
        var anomalouUsers = userActivities
            .Where(activity => activity.SuccessRate < suspiciousThreshold || activity.DeniedChecks > 100)
            .OrderBy(activity => activity.SuccessRate)
            .ThenByDescending(activity => activity.DeniedChecks);

        return anomalouUsers;
    }

    public async Task<IEnumerable<PermissionDenialStatistic>> GetMostDeniedPermissionsAsync(
        Guid tenantId,
        DateTimeRange period,
        int limit = 10)
    {
        return await GetDeniedAttemptsAsync(tenantId, period, limit);
    }

    public async Task<IEnumerable<PermissionChangeStatistic>> GetPermissionChangeAnalysisAsync(
        Guid tenantId,
        DateTimeRange period)
    {
        return await GetPermissionChangesAsync(tenantId, period);
    }

    public async Task<IEnumerable<PermissionRecommendation>> GetPermissionRecommendationsAsync(
        Guid tenantId,
        DateTimeRange? analysisWindow = null)
    {
        var recommendations = new List<PermissionRecommendation>();

        // Default to last 30 days if no window specified
        var window = analysisWindow ?? new DateTimeRange
        {
            StartDate = DateTime.UtcNow.AddDays(-30),
            EndDate = DateTime.UtcNow
        };

        // Recommendation 1: Users with high denial rates
        var anomalousUsers = await GetAnomalousUsersAsync(tenantId, window);
        if (anomalousUsers.Any())
        {
            recommendations.Add(new PermissionRecommendation
            {
                Type = "Security",
                Title = "Users with High Permission Denial Rates",
                Description = $"Found {anomalousUsers.Count()} users with unusual permission denial patterns",
                Impact = "High",
                ActionRequired = "Review user permissions and activities",
                AffectedUsers = anomalousUsers.Select(u => u.UserId)
            });
        }

        // Recommendation 2: Unused permissions
        var unusedPermissions = await GetUnusedPermissionsAsync(tenantId, window);
        if (unusedPermissions.Any())
        {
            recommendations.Add(new PermissionRecommendation
            {
                Type = "Cleanup",
                Title = "Unused Permissions",
                Description = $"Found {unusedPermissions.Count()} permissions that are never used",
                Impact = "Medium",
                ActionRequired = "Consider removing unused permissions",
                AffectedPermissions = unusedPermissions
            });
        }

        // Recommendation 3: Over-privileged users
        // This would require more complex analysis based on your business logic

        return recommendations;
    }

    public async Task<PermissionSecurityReport> GetSecurityAnalysisAsync(
        Guid tenantId,
        DateTimeRange period)
    {
        var securityIncidents = await _context.PermissionAuditLogs
            .Where(log => log.TenantId == tenantId
                && log.PerformedAt >= period.StartDate
                && log.PerformedAt <= period.EndDate
                && (!log.IsSuccess || log.Operation == "Denied"))
            .GroupBy(log => log.UserId)
            .Select(g => new SecurityIncident
            {
                UserId = g.Key,
                IncidentType = "High Denial Rate",
                Description = $"User had {g.Count()} permission denials",
                Timestamp = g.Max(log => log.PerformedAt),
                Severity = g.Count() > 50 ? "High" : g.Count() > 20 ? "Medium" : "Low",
                IsResolved = false
            })
            .ToListAsync();

        var usersWithHighDenialRates = await _context.PermissionAuditLogs
            .Where(log => log.TenantId == tenantId
                && log.PerformedAt >= period.StartDate
                && log.PerformedAt <= period.EndDate
                && !log.IsSuccess)
            .GroupBy(log => log.UserId)
            .Where(g => g.Count() > 20) // More than 20 denials
            .Select(g => g.Key!.Value)
            .ToListAsync();

        return new PermissionSecurityReport
        {
            TotalSecurityIncidents = securityIncidents.Count,
            SuspiciousActivityAttempts = securityIncidents.Count(i => i.Severity == "High"),
            RecentIncidents = securityIncidents.OrderByDescending(i => i.Timestamp).Take(10),
            UsersWithHighDenialRates = usersWithHighDenialRates,
            EscalatedPermissions = 0, // Would need additional logic
            ExpiredPermissions = await _context.TenantPermissions
                .Where(tp => tp.TenantId == tenantId && tp.ExpiresAt <= DateTime.UtcNow)
                .CountAsync()
        };
    }

    public async Task<PermissionMetrics> GetRealTimeMetricsAsync(Guid tenantId)
    {
        var last24Hours = DateTime.UtcNow.AddDays(-1);

        var recentLogs = await _context.PermissionAuditLogs
            .Where(log => log.TenantId == tenantId && log.PerformedAt >= last24Hours)
            .ToListAsync();

        var totalChecks = recentLogs.Count(log => log.Operation == "Check");
        var successfulChecks = recentLogs.Count(log => log.Operation == "Check" && log.IsSuccess);

        return new PermissionMetrics
        {
            ActiveUsers = recentLogs.Select(log => log.UserId).Distinct().Count(),
            TotalPermissionChecks = totalChecks,
            SuccessfulChecks = successfulChecks,
            DeniedChecks = totalChecks - successfulChecks,
            SuccessRate = totalChecks > 0 ? (double)successfulChecks / totalChecks : 0,
            AverageResponseTime = 50, // Placeholder - would need actual timing data
            LastUpdated = DateTime.UtcNow,
            PermissionUsageCount = recentLogs
                .Where(log => log.Operation == "Check")
                .SelectMany(log => log.Permissions)
                .GroupBy(p => p)
                .ToDictionary(g => g.Key, g => g.Count())
        };
    }

    public async Task<byte[]> ExportAnalyticsDataAsync(
        Guid tenantId,
        DateTimeRange period,
        AnalyticsExportFormat format = AnalyticsExportFormat.Json)
    {
        var report = await GetUsageReportAsync(tenantId, period);

        return format switch
        {
            AnalyticsExportFormat.Json => JsonSerializer.SerializeToUtf8Bytes(report, new JsonSerializerOptions { WriteIndented = true }),
            AnalyticsExportFormat.Csv => ExportToCsv(report),
            AnalyticsExportFormat.Excel => ExportToExcel(report),
            AnalyticsExportFormat.Pdf => ExportToPdf(report),
            _ => throw new ArgumentException($"Unsupported export format: {format}")
        };
    }

    #region Private Helper Methods

    private async Task<IEnumerable<PermissionUsageStatistic>> GetMostUsedPermissionsAsync(Guid tenantId, DateTimeRange period)
    {
        return await _context.PermissionAuditLogs
            .Where(log => log.TenantId == tenantId
                && log.PerformedAt >= period.StartDate
                && log.PerformedAt <= period.EndDate
                && log.Operation == "Check"
                && log.IsSuccess)
            .SelectMany(log => log.Permissions.Select(p => new { Permission = p, Log = log }))
            .GroupBy(x => x.Permission)
            .Select(g => new PermissionUsageStatistic
            {
                Permission = g.Key,
                UsageCount = g.Count(),
                UniqueUsers = g.Select(x => x.Log.UserId).Distinct().Count(),
                FirstUsed = g.Min(x => x.Log.PerformedAt),
                LastUsed = g.Max(x => x.Log.PerformedAt),
                AverageUsagePerUser = g.Count() / (double)g.Select(x => x.Log.UserId).Distinct().Count(),
                PermissionLayer = g.First().Log.PermissionLayer ?? "Unknown"
            })
            .OrderByDescending(stat => stat.UsageCount)
            .Take(20)
            .ToListAsync();
    }

    private async Task<IEnumerable<PermissionDenialStatistic>> GetDeniedAttemptsAsync(Guid tenantId, DateTimeRange period, int limit = 10)
    {
        return await _context.PermissionAuditLogs
            .Where(log => log.TenantId == tenantId
                && log.PerformedAt >= period.StartDate
                && log.PerformedAt <= period.EndDate
                && (log.Operation == "Denied" || !log.IsSuccess))
            .SelectMany(log => log.Permissions.Select(p => new { Permission = p, Log = log }))
            .GroupBy(x => x.Permission)
            .Select(g => new PermissionDenialStatistic
            {
                Permission = g.Key,
                DenialCount = g.Count(),
                UniqueUsers = g.Select(x => x.Log.UserId).Distinct().Count(),
                FirstDenied = g.Min(x => x.Log.PerformedAt),
                LastDenied = g.Max(x => x.Log.PerformedAt),
                MostCommonReason = g.GroupBy(x => x.Log.Reason ?? "Unknown")
                    .OrderByDescending(r => r.Count())
                    .First().Key,
                TopDeniedResources = g.Where(x => x.Log.ResourceId.HasValue)
                    .GroupBy(x => x.Log.ResourceId!.Value.ToString())
                    .OrderByDescending(r => r.Count())
                    .Take(5)
                    .Select(r => r.Key)
            })
            .OrderByDescending(stat => stat.DenialCount)
            .Take(limit)
            .ToListAsync();
    }

    private async Task<IEnumerable<PermissionChangeStatistic>> GetPermissionChangesAsync(Guid tenantId, DateTimeRange period)
    {
        return await _context.PermissionAuditLogs
            .Where(log => log.TenantId == tenantId
                && log.PerformedAt >= period.StartDate
                && log.PerformedAt <= period.EndDate
                && (log.Operation == "Grant" || log.Operation == "Revoke"))
            .SelectMany(log => log.Permissions.Select(p => new { Permission = p, Log = log }))
            .GroupBy(x => new { x.Permission, x.Log.Operation })
            .Select(g => new PermissionChangeStatistic
            {
                Operation = g.Key.Operation,
                Permission = g.Key.Permission,
                ChangeCount = g.Count(),
                LastChange = g.Max(x => x.Log.PerformedAt),
                LastChangedBy = g.OrderByDescending(x => x.Log.PerformedAt).First().Log.PerformedBy,
                Trend = "Stable" // Would need time-series analysis for actual trends
            })
            .OrderByDescending(stat => stat.ChangeCount)
            .ToListAsync();
    }

    private async Task<IEnumerable<PermissionType>> GetUnusedPermissionsAsync(Guid tenantId, DateTimeRange period)
    {
        var usedPermissions = await _context.PermissionAuditLogs
            .Where(log => log.TenantId == tenantId
                && log.PerformedAt >= period.StartDate
                && log.PerformedAt <= period.EndDate
                && log.Operation == "Check")
            .SelectMany(log => log.Permissions)
            .Distinct()
            .ToListAsync();

        var allPermissions = Enum.GetValues<PermissionType>();

        return allPermissions.Except(usedPermissions);
    }

    private async Task<IEnumerable<UserPermissionActivity>> GetMostActiveUsersAsync(Guid tenantId, DateTimeRange period)
    {
        return await _context.PermissionAuditLogs
            .Where(log => log.TenantId == tenantId
                && log.PerformedAt >= period.StartDate
                && log.PerformedAt <= period.EndDate
                && log.Operation == "Check"
                && log.UserId.HasValue)
            .GroupBy(log => log.UserId!.Value)
            .Select(g => new UserPermissionActivity
            {
                UserId = g.Key,
                TotalPermissionChecks = g.Count(),
                SuccessfulChecks = g.Count(log => log.IsSuccess),
                DeniedChecks = g.Count(log => !log.IsSuccess),
                SuccessRate = g.Count() > 0 ? (double)g.Count(log => log.IsSuccess) / g.Count() : 0,
                MostUsedPermissions = g.SelectMany(log => log.Permissions)
                    .GroupBy(p => p)
                    .OrderByDescending(pg => pg.Count())
                    .Take(5)
                    .Select(pg => pg.Key),
                LastActivity = g.Max(log => log.PerformedAt)
            })
            .OrderByDescending(activity => activity.TotalPermissionChecks)
            .Take(20)
            .ToListAsync();
    }

    private async Task<PermissionUsagePattern> GetUsagePatternsAsync(Guid tenantId, DateTimeRange period)
    {
        var logs = await _context.PermissionAuditLogs
            .Where(log => log.TenantId == tenantId
                && log.PerformedAt >= period.StartDate
                && log.PerformedAt <= period.EndDate
                && log.Operation == "Check")
            .Select(log => log.PerformedAt)
            .ToListAsync();

        var hourlyUsage = logs
            .GroupBy(dt => dt.Hour)
            .ToDictionary(g => g.Key, g => g.Count());

        var dailyUsage = logs
            .GroupBy(dt => dt.DayOfWeek)
            .ToDictionary(g => g.Key, g => g.Count());

        var monthlyUsage = logs
            .GroupBy(dt => dt.ToString("yyyy-MM"))
            .ToDictionary(g => g.Key, g => g.Count());

        return new PermissionUsagePattern
        {
            HourlyUsage = hourlyUsage,
            DailyUsage = dailyUsage,
            MonthlyUsage = monthlyUsage,
            PeakHour = hourlyUsage.OrderByDescending(kv => kv.Value).FirstOrDefault().Key,
            PeakDay = dailyUsage.OrderByDescending(kv => kv.Value).FirstOrDefault().Key,
            PeakMonth = monthlyUsage.OrderByDescending(kv => kv.Value).FirstOrDefault().Key ?? string.Empty
        };
    }

    private byte[] ExportToCsv(PermissionUsageReport report)
    {
        // Implement CSV export logic
        return System.Text.Encoding.UTF8.GetBytes("CSV export not implemented");
    }

    private byte[] ExportToExcel(PermissionUsageReport report)
    {
        // Implement Excel export logic
        return System.Text.Encoding.UTF8.GetBytes("Excel export not implemented");
    }

    private byte[] ExportToPdf(PermissionUsageReport report)
    {
        // Implement PDF export logic
        return System.Text.Encoding.UTF8.GetBytes("PDF export not implemented");
    }

    #endregion
}