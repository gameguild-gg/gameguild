using GameGuild.Modules.Permissions.Models;

namespace GameGuild.Modules.Permissions.Abstractions;

/// <summary>
/// Service for analyzing permission usage patterns and generating insights
/// </summary>
public interface IPermissionAnalyticsService
{
    /// <summary>
    /// Generate comprehensive usage report for a tenant
    /// </summary>
    Task<PermissionUsageReport> GetUsageReportAsync(
        Guid tenantId,
        DateTimeRange period,
        bool includeSecurityAnalysis = true);

    /// <summary>
    /// Get permission usage trends over time
    /// </summary>
    Task<IEnumerable<PermissionUsageStatistic>> GetPermissionTrendsAsync(
        Guid tenantId,
        PermissionType? permission = null,
        DateTimeRange? period = null);

    /// <summary>
    /// Identify users with unusual permission patterns
    /// </summary>
    Task<IEnumerable<UserPermissionActivity>> GetAnomalousUsersAsync(
        Guid tenantId,
        DateTimeRange period,
        double suspiciousThreshold = 0.3);

    /// <summary>
    /// Get most denied permissions for security analysis
    /// </summary>
    Task<IEnumerable<PermissionDenialStatistic>> GetMostDeniedPermissionsAsync(
        Guid tenantId,
        DateTimeRange period,
        int limit = 10);

    /// <summary>
    /// Analyze permission changes over time
    /// </summary>
    Task<IEnumerable<PermissionChangeStatistic>> GetPermissionChangeAnalysisAsync(
        Guid tenantId,
        DateTimeRange period);

    /// <summary>
    /// Get recommendations for permission optimization
    /// </summary>
    Task<IEnumerable<PermissionRecommendation>> GetPermissionRecommendationsAsync(
        Guid tenantId,
        DateTimeRange? analysisWindow = null);

    /// <summary>
    /// Detect potential security issues
    /// </summary>
    Task<PermissionSecurityReport> GetSecurityAnalysisAsync(
        Guid tenantId,
        DateTimeRange period);

    /// <summary>
    /// Get real-time permission usage metrics
    /// </summary>
    Task<PermissionMetrics> GetRealTimeMetricsAsync(Guid tenantId);

    /// <summary>
    /// Export analytics data for external analysis
    /// </summary>
    Task<byte[]> ExportAnalyticsDataAsync(
        Guid tenantId,
        DateTimeRange period,
        AnalyticsExportFormat format = AnalyticsExportFormat.Json);
}

/// <summary>
/// Permission optimization recommendations
/// </summary>
public class PermissionRecommendation
{
    public string Type { get; set; } = null!; // Optimization, Security, Cleanup
    public string Title { get; set; } = null!;
    public string Description { get; set; } = null!;
    public string Impact { get; set; } = null!; // High, Medium, Low
    public string ActionRequired { get; set; } = null!;
    public IEnumerable<Guid> AffectedUsers { get; set; } = Enumerable.Empty<Guid>();
    public IEnumerable<PermissionType> AffectedPermissions { get; set; } = Enumerable.Empty<PermissionType>();
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Real-time permission metrics
/// </summary>
public class PermissionMetrics
{
    public int ActiveUsers { get; set; }
    public int TotalPermissionChecks { get; set; }
    public int SuccessfulChecks { get; set; }
    public int DeniedChecks { get; set; }
    public double SuccessRate { get; set; }
    public int AverageResponseTime { get; set; } // milliseconds
    public DateTime LastUpdated { get; set; }
    public Dictionary<PermissionType, int> PermissionUsageCount { get; set; } = new();
}

/// <summary>
/// Export formats for analytics data
/// </summary>
public enum AnalyticsExportFormat
{
    Json,
    Csv,
    Excel,
    Pdf
}