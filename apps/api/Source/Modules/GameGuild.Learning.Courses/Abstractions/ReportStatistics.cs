namespace GameGuild.Learning.Courses;

/// <summary>
/// Report statistics model
/// </summary>
public class ReportStatistics {
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