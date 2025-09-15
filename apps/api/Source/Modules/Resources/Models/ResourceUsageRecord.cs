using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.Modules.Resources.Models;

/// <summary>
/// Tracks resource usage over time for analytics and monitoring
/// </summary>
[Table("ResourceUsageRecords")]
[Index(nameof(TenantId), nameof(Type), nameof(PeriodStart))]
public class ResourceUsageRecord : EntityBase
{
    /// <summary>
    /// Type of resource usage being tracked
    /// </summary>
    public ResourceUsageType Type { get; set; }

    /// <summary>
    /// Tenant this usage record belongs to
    /// </summary>
    public Guid TenantId { get; set; }

    /// <summary>
    /// Usage count for this period
    /// </summary>
    public long Count { get; set; }

    /// <summary>
    /// Start of the tracking period
    /// </summary>
    public DateTime PeriodStart { get; set; }

    /// <summary>
    /// End of the tracking period
    /// </summary>
    public DateTime PeriodEnd { get; set; }

    /// <summary>
    /// Average usage per day in this period
    /// </summary>
    public double? AveragePerDay { get; set; }

    /// <summary>
    /// Peak usage in a single day during this period
    /// </summary>
    public long? PeakUsage { get; set; }

    /// <summary>
    /// Date when peak usage occurred
    /// </summary>
    public DateTime? PeakUsageDate { get; set; }

    /// <summary>
    /// Additional context or metadata about this usage (JSON)
    /// </summary>
    [MaxLength(1000)]
    public string? Metadata { get; set; }

    /// <summary>
    /// Source of the usage (API, UI, System, etc.)
    /// </summary>
    [MaxLength(50)]
    public string? Source { get; set; }

    /// <summary>
    /// User who generated this usage (if applicable)
    /// </summary>
    public Guid? UserId { get; set; }

    /// <summary>
    /// Resource identifier that was used (if applicable)
    /// </summary>
    public Guid? ResourceId { get; set; }

    /// <summary>
    /// Calculate usage for a specific number of days
    /// </summary>
    public double GetUsagePerDay()
    {
        var totalDays = (PeriodEnd - PeriodStart).TotalDays;
        return totalDays > 0 ? Count / totalDays : Count;
    }

    /// <summary>
    /// Create a daily usage record
    /// </summary>
    public static ResourceUsageRecord CreateDaily(
        ResourceUsageType type,
        Guid tenantId,
        long count,
        DateTime date,
        Guid? userId = null,
        string? source = null)
    {
        return new ResourceUsageRecord
        {
            Type = type,
            TenantId = tenantId,
            Count = count,
            PeriodStart = date.Date,
            PeriodEnd = date.Date.AddDays(1).AddTicks(-1),
            UserId = userId,
            Source = source,
            AveragePerDay = count
        };
    }

    /// <summary>
    /// Create a monthly usage record
    /// </summary>
    public static ResourceUsageRecord CreateMonthly(
        ResourceUsageType type,
        Guid tenantId,
        long count,
        DateTime month,
        long? peakUsage = null,
        DateTime? peakDate = null)
    {
        var startOfMonth = new DateTime(month.Year, month.Month, 1);
        var endOfMonth = startOfMonth.AddMonths(1).AddTicks(-1);
        var daysInMonth = DateTime.DaysInMonth(month.Year, month.Month);

        return new ResourceUsageRecord
        {
            Type = type,
            TenantId = tenantId,
            Count = count,
            PeriodStart = startOfMonth,
            PeriodEnd = endOfMonth,
            AveragePerDay = (double)count / daysInMonth,
            PeakUsage = peakUsage,
            PeakUsageDate = peakDate
        };
    }
}
