using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GameGuild.Analytics;

/// <summary>
///     Represents a tracked analytics event.
/// </summary>
[Table("analytics_events")]
[Index(nameof(EventName))]
[Index(nameof(UserId))]
[Index(nameof(TenantId))]
[Index(nameof(Timestamp))]
public class AnalyticsEvent : EntityBase
{
    [Required]
    [MaxLength(200)]
    public string EventName { get; set; } = string.Empty;

    /// <summary>
    ///     Event properties stored as JSONB.
    /// </summary>
    [Column(TypeName = "jsonb")]
    public string? Properties { get; set; }

    public Guid? UserId { get; set; }

    [MaxLength(100)]
    public string? SessionId { get; set; }

    [MaxLength(45)]
    public string? IpAddress { get; set; }

    [MaxLength(500)]
    public string? UserAgent { get; set; }

    [MaxLength(2000)]
    public string? PageUrl { get; set; }

    [MaxLength(2000)]
    public string? Referrer { get; set; }

    [MaxLength(50)]
    public string? Environment { get; set; }

    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

/// <summary>
///     Defines a KPI metric that can be calculated from analytics events.
/// </summary>
[Table("analytics_kpi_definitions")]
[Index(nameof(Name), IsUnique = true)]
public class KpiDefinition : EntityBase
{
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    /// <summary>
    ///     The event name this KPI is based on.
    /// </summary>
    [MaxLength(200)]
    public string? EventName { get; set; }

    /// <summary>
    ///     The aggregation function to use (Count, Sum, Average, Min, Max, DistinctCount).
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string AggregationFunction { get; set; } = "Count";

    /// <summary>
    ///     The property field to aggregate on (null for Count).
    /// </summary>
    [MaxLength(200)]
    public string? AggregateField { get; set; }

    public bool IsActive { get; set; } = true;
}

/// <summary>
///     A dashboard containing multiple widgets.
/// </summary>
[Table("analytics_dashboards")]
[Index(nameof(Slug), IsUnique = true)]
public class Dashboard : EntityBase
{
    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(200)]
    public string Slug { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Description { get; set; }

    public bool IsDefault { get; set; }

    public ICollection<DashboardWidget> Widgets { get; set; } = new List<DashboardWidget>();
}

/// <summary>
///     A widget on an analytics dashboard.
/// </summary>
[Table("analytics_dashboard_widgets")]
[Index(nameof(DashboardId))]
public class DashboardWidget : EntityBase
{
    public Guid DashboardId { get; set; }

    [ForeignKey(nameof(DashboardId))]
    public Dashboard? Dashboard { get; set; }

    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    public WidgetType Type { get; set; }

    public int SortOrder { get; set; }

    /// <summary>
    ///     Widget configuration stored as JSONB (KPI name, filters, etc.).
    /// </summary>
    [Column(TypeName = "jsonb")]
    public string? Configuration { get; set; }
}

public enum WidgetType
{
    Counter,
    Chart,
    Table,
    Gauge,
    TimeSeries,
    Funnel
}

public enum ReportFormat
{
    Json,
    Csv
}

public enum TimeSeriesGranularity
{
    Hour,
    Day,
    Week,
    Month
}

public enum AggregationFunction
{
    Count,
    Sum,
    Average,
    Min,
    Max,
    DistinctCount
}
