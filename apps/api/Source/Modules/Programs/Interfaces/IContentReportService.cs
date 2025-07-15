using GameGuild.Modules.Programs.Models;

namespace GameGuild.Modules.Programs.Interfaces;

/// <summary>
/// Interface for content report services
/// </summary>
public interface IContentReportService
{
    /// <summary>
    /// Create a new content report
    /// </summary>
    Task<ContentReport> CreateReportAsync(Guid userId, Guid contentId, ReportType type, string reason, string? description = null);

    /// <summary>
    /// Get report by ID
    /// </summary>
    Task<ContentReport?> GetReportByIdAsync(Guid reportId);

    /// <summary>
    /// Get all reports for specific content
    /// </summary>
    Task<IEnumerable<ContentReport>> GetContentReportsAsync(Guid contentId);

    /// <summary>
    /// Get all reports by user
    /// </summary>
    Task<IEnumerable<ContentReport>> GetUserReportsAsync(Guid userId);

    /// <summary>
    /// Get pending reports for moderation
    /// </summary>
    Task<IEnumerable<ContentReport>> GetPendingReportsAsync();

    /// <summary>
    /// Update report status
    /// </summary>
    Task<ContentReport> UpdateReportStatusAsync(Guid reportId, ReportStatus status, Guid? reviewedBy = null, string? resolution = null);

    /// <summary>
    /// Escalate report to higher authority
    /// </summary>
    Task<ContentReport> EscalateReportAsync(Guid reportId, Guid escalatedBy, string reason);

    /// <summary>
    /// Get report statistics
    /// </summary>
    Task<ReportStatistics> GetReportStatisticsAsync(DateTime? fromDate = null, DateTime? toDate = null);

    /// <summary>
    /// Get reports requiring attention (high priority, escalated, etc.)
    /// </summary>
    Task<IEnumerable<ContentReport>> GetReportsRequiringAttentionAsync();

    /// <summary>
    /// Mark report as spam/invalid
    /// </summary>
    Task<ContentReport> MarkAsSpamAsync(Guid reportId, Guid reviewedBy);

    /// <summary>
    /// Bulk update report statuses
    /// </summary>
    Task<IEnumerable<ContentReport>> BulkUpdateStatusAsync(IEnumerable<Guid> reportIds, ReportStatus status, Guid reviewedBy);

    /// <summary>
    /// Delete report (admin only)
    /// </summary>
    Task<bool> DeleteReportAsync(Guid reportId, Guid deletedBy);
}

/// <summary>
/// Report statistics model
/// </summary>
public class ReportStatistics
{
    public int TotalReports { get; set; }
    public int PendingReports { get; set; }
    public int ResolvedReports { get; set; }
    public int EscalatedReports { get; set; }
    public int SpamReports { get; set; }
    public Dictionary<ReportType, int> ReportsByType { get; set; } = new();
    public Dictionary<ReportStatus, int> ReportsByStatus { get; set; } = new();
    public decimal AverageResolutionTimeHours { get; set; }
    public DateTime? OldestPendingReport { get; set; }
}
