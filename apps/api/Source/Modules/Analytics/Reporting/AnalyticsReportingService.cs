using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace GameGuild.Modules.Analytics.Reporting;

/// <summary>
/// Analytics and reporting service interface.
/// </summary>
public interface IAnalyticsReportingService
{
    Task<AnalyticsEvent> TrackEventAsync(
        string eventName,
        Dictionary<string, object> properties,
        Guid? userId = null,
        CancellationToken cancellationToken = default);

    Task<IEnumerable<AnalyticsEvent>> TrackEventsAsync(
        IEnumerable<AnalyticsEvent> events,
        CancellationToken cancellationToken = default);

    Task<KpiResult> CalculateKpiAsync(
        string kpiName,
        DateTime startDate,
        DateTime endDate,
        Dictionary<string, string>? filters = null,
        CancellationToken cancellationToken = default);

    Task<DashboardData> GetDashboardDataAsync(
        string dashboardId,
        DateTime? startDate = null,
        DateTime? endDate = null,
        CancellationToken cancellationToken = default);

    Task<ReportResult> GenerateReportAsync(
        ReportDefinition definition,
        ReportFormat format = ReportFormat.Json,
        CancellationToken cancellationToken = default);

    Task<TimeSeriesData> GetTimeSeriesDataAsync(
        string metricName,
        DateTime startDate,
        DateTime endDate,
        TimeSeriesGranularity granularity = TimeSeriesGranularity.Day,
        CancellationToken cancellationToken = default);

    Task<IEnumerable<AggregationResult>> AggregateEventsAsync(
        string eventName,
        string[] groupBy,
        string aggregateField,
        AggregationFunction function,
        DateTime? startDate = null,
        DateTime? endDate = null,
        CancellationToken cancellationToken = default);

    Task<FunnelAnalysisResult> AnalyzeFunnelAsync(
        string[] steps,
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Analytics and reporting service implementation.
/// </summary>
public sealed class AnalyticsReportingService : IAnalyticsReportingService
{
    private readonly ILogger<AnalyticsReportingService> _logger;
    private readonly List<AnalyticsEvent> _events;
    private readonly Dictionary<string, KpiDefinition> _kpiDefinitions;
    private readonly Dictionary<string, Dashboard> _dashboards;

    public AnalyticsReportingService(ILogger<AnalyticsReportingService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _events = new List<AnalyticsEvent>();
        _kpiDefinitions = new Dictionary<string, KpiDefinition>();
        _dashboards = new Dictionary<string, Dashboard>();

        InitializeDefaultKpis();
    }

    public Task<AnalyticsEvent> TrackEventAsync(
        string eventName,
        Dictionary<string, object> properties,
        Guid? userId = null,
        CancellationToken cancellationToken = default)
    {
        var analyticsEvent = new AnalyticsEvent
        {
            Id = Guid.NewGuid(),
            EventName = eventName,
            Properties = properties,
            UserId = userId,
            Timestamp = DateTime.UtcNow
        };

        _events.Add(analyticsEvent);

        _logger.LogInformation("Tracked event {EventName} for user {UserId}",
            eventName, userId);

        return Task.FromResult(analyticsEvent);
    }

    public Task<IEnumerable<AnalyticsEvent>> TrackEventsAsync(
        IEnumerable<AnalyticsEvent> events,
        CancellationToken cancellationToken = default)
    {
        var trackedEvents = new List<AnalyticsEvent>();

        foreach (var evt in events)
        {
            evt.Timestamp = DateTime.UtcNow;
            _events.Add(evt);
            trackedEvents.Add(evt);
        }

        _logger.LogInformation("Bulk tracked {Count} events", trackedEvents.Count);

        return Task.FromResult<IEnumerable<AnalyticsEvent>>(trackedEvents);
    }

    public Task<KpiResult> CalculateKpiAsync(
        string kpiName,
        DateTime startDate,
        DateTime endDate,
        Dictionary<string, string>? filters = null,
        CancellationToken cancellationToken = default)
    {
        if (!_kpiDefinitions.TryGetValue(kpiName, out var definition))
        {
            throw new InvalidOperationException($"KPI definition '{kpiName}' not found");
        }

        var filteredEvents = _events
            .Where(e => e.Timestamp >= startDate && e.Timestamp <= endDate)
            .Where(e => definition.EventName == null || e.EventName == definition.EventName);

        if (filters != null)
        {
            foreach (var filter in filters)
            {
                var key = filter.Key;
                var value = filter.Value;
                filteredEvents = filteredEvents.Where(e =>
                    e.Properties.TryGetValue(key, out var propValue) &&
                    propValue?.ToString() == value);
            }
        }

        var eventList = filteredEvents.ToList();
        var value = definition.CalculationFunction(eventList);

        var result = new KpiResult
        {
            KpiName = kpiName,
            Value = value,
            StartDate = startDate,
            EndDate = endDate,
            CalculatedAt = DateTime.UtcNow
        };

        _logger.LogInformation("Calculated KPI {KpiName}: {Value}", kpiName, value);

        return Task.FromResult(result);
    }

    public Task<DashboardData> GetDashboardDataAsync(
        string dashboardId,
        DateTime? startDate = null,
        DateTime? endDate = null,
        CancellationToken cancellationToken = default)
    {
        if (!_dashboards.TryGetValue(dashboardId, out var dashboard))
        {
            throw new InvalidOperationException($"Dashboard '{dashboardId}' not found");
        }

        startDate ??= DateTime.UtcNow.AddDays(-30);
        endDate ??= DateTime.UtcNow;

        var widgets = new List<DashboardWidget>();

        foreach (var widgetDef in dashboard.Widgets)
        {
            var widget = new DashboardWidget
            {
                Id = widgetDef.Id,
                Title = widgetDef.Title,
                Type = widgetDef.Type,
                Data = CalculateWidgetData(widgetDef, startDate.Value, endDate.Value)
            };
            widgets.Add(widget);
        }

        var data = new DashboardData
        {
            DashboardId = dashboardId,
            Title = dashboard.Title,
            Widgets = widgets,
            GeneratedAt = DateTime.UtcNow
        };

        return Task.FromResult(data);
    }

    public Task<ReportResult> GenerateReportAsync(
        ReportDefinition definition,
        ReportFormat format = ReportFormat.Json,
        CancellationToken cancellationToken = default)
    {
        var filteredEvents = _events
            .Where(e => e.Timestamp >= definition.StartDate && e.Timestamp <= definition.EndDate);

        if (!string.IsNullOrEmpty(definition.EventName))
        {
            filteredEvents = filteredEvents.Where(e => e.EventName == definition.EventName);
        }

        var eventList = filteredEvents.ToList();
        var reportData = new Dictionary<string, object>
        {
            ["TotalEvents"] = eventList.Count,
            ["UniqueUsers"] = eventList.Where(e => e.UserId.HasValue).Select(e => e.UserId).Distinct().Count(),
            ["StartDate"] = definition.StartDate,
            ["EndDate"] = definition.EndDate,
            ["Events"] = eventList.Take(1000) // Limit for performance
        };

        var result = new ReportResult
        {
            ReportName = definition.Name,
            Format = format,
            Data = reportData,
            GeneratedAt = DateTime.UtcNow
        };

        _logger.LogInformation("Generated report {ReportName} with {EventCount} events",
            definition.Name, eventList.Count);

        return Task.FromResult(result);
    }

    public Task<TimeSeriesData> GetTimeSeriesDataAsync(
        string metricName,
        DateTime startDate,
        DateTime endDate,
        TimeSeriesGranularity granularity = TimeSeriesGranularity.Day,
        CancellationToken cancellationToken = default)
    {
        var filteredEvents = _events
            .Where(e => e.Timestamp >= startDate && e.Timestamp <= endDate)
            .ToList();

        var groupedData = granularity switch
        {
            TimeSeriesGranularity.Hour => filteredEvents.GroupBy(e => new DateTime(e.Timestamp.Year, e.Timestamp.Month, e.Timestamp.Day, e.Timestamp.Hour, 0, 0)),
            TimeSeriesGranularity.Day => filteredEvents.GroupBy(e => e.Timestamp.Date),
            TimeSeriesGranularity.Week => filteredEvents.GroupBy(e => GetWeekStart(e.Timestamp)),
            TimeSeriesGranularity.Month => filteredEvents.GroupBy(e => new DateTime(e.Timestamp.Year, e.Timestamp.Month, 1)),
            _ => throw new ArgumentException($"Unknown granularity: {granularity}")
        };

        var dataPoints = groupedData
            .OrderBy(g => g.Key)
            .Select(g => new TimeSeriesDataPoint
            {
                Timestamp = g.Key,
                Value = g.Count()
            })
            .ToList();

        var result = new TimeSeriesData
        {
            MetricName = metricName,
            Granularity = granularity,
            DataPoints = dataPoints
        };

        return Task.FromResult(result);
    }

    public Task<IEnumerable<AggregationResult>> AggregateEventsAsync(
        string eventName,
        string[] groupBy,
        string aggregateField,
        AggregationFunction function,
        DateTime? startDate = null,
        DateTime? endDate = null,
        CancellationToken cancellationToken = default)
    {
        var filteredEvents = _events.Where(e => e.EventName == eventName);

        if (startDate.HasValue)
            filteredEvents = filteredEvents.Where(e => e.Timestamp >= startDate.Value);

        if (endDate.HasValue)
            filteredEvents = filteredEvents.Where(e => e.Timestamp <= endDate.Value);

        var eventList = filteredEvents.ToList();

        var grouped = eventList.GroupBy(e =>
        {
            var key = new Dictionary<string, string>();
            foreach (var field in groupBy)
            {
                if (e.Properties.TryGetValue(field, out var value))
                {
                    key[field] = value?.ToString() ?? "";
                }
            }
            return string.Join("|", key.Values);
        });

        var results = grouped.Select(g =>
        {
            var groupKey = g.First().Properties
                .Where(p => groupBy.Contains(p.Key))
                .ToDictionary(p => p.Key, p => p.Value?.ToString() ?? "");

            var aggregatedValue = function switch
            {
                AggregationFunction.Count => g.Count(),
                AggregationFunction.Sum => g.Sum(e => Convert.ToDouble(e.Properties.GetValueOrDefault(aggregateField, 0))),
                AggregationFunction.Average => g.Average(e => Convert.ToDouble(e.Properties.GetValueOrDefault(aggregateField, 0))),
                AggregationFunction.Min => g.Min(e => Convert.ToDouble(e.Properties.GetValueOrDefault(aggregateField, 0))),
                AggregationFunction.Max => g.Max(e => Convert.ToDouble(e.Properties.GetValueOrDefault(aggregateField, 0))),
                _ => throw new ArgumentException($"Unknown function: {function}")
            };

            return new AggregationResult
            {
                GroupKey = groupKey,
                Value = aggregatedValue
            };
        }).ToList();

        return Task.FromResult<IEnumerable<AggregationResult>>(results);
    }

    public Task<FunnelAnalysisResult> AnalyzeFunnelAsync(
        string[] steps,
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default)
    {
        var filteredEvents = _events
            .Where(e => e.Timestamp >= startDate && e.Timestamp <= endDate)
            .Where(e => steps.Contains(e.EventName))
            .GroupBy(e => e.UserId)
            .Where(g => g.Key.HasValue)
            .ToList();

        var funnelSteps = new List<FunnelStep>();
        var previousCount = filteredEvents.Count;

        for (var i = 0; i < steps.Length; i++)
        {
            var stepName = steps[i];
            var usersAtStep = filteredEvents.Count(g => g.Any(e => e.EventName == stepName));
            var dropOffRate = previousCount > 0 ? (previousCount - usersAtStep) / (double)previousCount * 100 : 0;

            funnelSteps.Add(new FunnelStep
            {
                StepName = stepName,
                UserCount = usersAtStep,
                DropOffRate = dropOffRate
            });

            previousCount = usersAtStep;
        }

        var result = new FunnelAnalysisResult
        {
            Steps = funnelSteps,
            StartDate = startDate,
            EndDate = endDate,
            TotalUsers = filteredEvents.Count
        };

        return Task.FromResult(result);
    }

    private void InitializeDefaultKpis()
    {
        _kpiDefinitions["TotalEvents"] = new KpiDefinition
        {
            Name = "TotalEvents",
            EventName = null,
            CalculationFunction = events => events.Count
        };

        _kpiDefinitions["UniqueUsers"] = new KpiDefinition
        {
            Name = "UniqueUsers",
            EventName = null,
            CalculationFunction = events => events.Where(e => e.UserId.HasValue).Select(e => e.UserId).Distinct().Count()
        };
    }

    private object CalculateWidgetData(WidgetDefinition widget, DateTime startDate, DateTime endDate)
    {
        // Simplified widget calculation - in production would be more sophisticated
        return widget.Type switch
        {
            WidgetType.Counter => _events.Count(e => e.Timestamp >= startDate && e.Timestamp <= endDate),
            WidgetType.Chart => GetTimeSeriesDataAsync(widget.Title, startDate, endDate).Result,
            _ => null
        };
    }

    private static DateTime GetWeekStart(DateTime date)
    {
        var diff = (7 + (date.DayOfWeek - DayOfWeek.Monday)) % 7;
        return date.AddDays(-1 * diff).Date;
    }
}

/// <summary>
/// Analytics event entity.
/// </summary>
public sealed class AnalyticsEvent
{
    public required Guid Id { get; init; }
    public required string EventName { get; init; }
    public required Dictionary<string, object> Properties { get; init; }
    public Guid? UserId { get; init; }
    public required DateTime Timestamp { get; set; }
}

/// <summary>
/// KPI definition.
/// </summary>
public sealed class KpiDefinition
{
    public required string Name { get; init; }
    public string? EventName { get; init; }
    public required Func<List<AnalyticsEvent>, double> CalculationFunction { get; init; }
}

/// <summary>
/// KPI result.
/// </summary>
public sealed class KpiResult
{
    public required string KpiName { get; init; }
    public required double Value { get; init; }
    public required DateTime StartDate { get; init; }
    public required DateTime EndDate { get; init; }
    public required DateTime CalculatedAt { get; init; }
}

/// <summary>
/// Dashboard entity.
/// </summary>
public sealed class Dashboard
{
    public required string Id { get; init; }
    public required string Title { get; init; }
    public required List<WidgetDefinition> Widgets { get; init; }
}

/// <summary>
/// Widget definition.
/// </summary>
public sealed class WidgetDefinition
{
    public required string Id { get; init; }
    public required string Title { get; init; }
    public required WidgetType Type { get; init; }
}

/// <summary>
/// Dashboard data.
/// </summary>
public sealed class DashboardData
{
    public required string DashboardId { get; init; }
    public required string Title { get; init; }
    public required List<DashboardWidget> Widgets { get; init; }
    public required DateTime GeneratedAt { get; init; }
}

/// <summary>
/// Dashboard widget.
/// </summary>
public sealed class DashboardWidget
{
    public required string Id { get; init; }
    public required string Title { get; init; }
    public required WidgetType Type { get; init; }
    public required object Data { get; init; }
}

/// <summary>
/// Report definition.
/// </summary>
public sealed class ReportDefinition
{
    public required string Name { get; init; }
    public string? EventName { get; init; }
    public required DateTime StartDate { get; init; }
    public required DateTime EndDate { get; init; }
}

/// <summary>
/// Report result.
/// </summary>
public sealed class ReportResult
{
    public required string ReportName { get; init; }
    public required ReportFormat Format { get; init; }
    public required Dictionary<string, object> Data { get; init; }
    public required DateTime GeneratedAt { get; init; }
}

/// <summary>
/// Time series data.
/// </summary>
public sealed class TimeSeriesData
{
    public required string MetricName { get; init; }
    public required TimeSeriesGranularity Granularity { get; init; }
    public required List<TimeSeriesDataPoint> DataPoints { get; init; }
}

/// <summary>
/// Time series data point.
/// </summary>
public sealed class TimeSeriesDataPoint
{
    public required DateTime Timestamp { get; init; }
    public required double Value { get; init; }
}

/// <summary>
/// Aggregation result.
/// </summary>
public sealed class AggregationResult
{
    public required Dictionary<string, string> GroupKey { get; init; }
    public required double Value { get; init; }
}

/// <summary>
/// Funnel analysis result.
/// </summary>
public sealed class FunnelAnalysisResult
{
    public required List<FunnelStep> Steps { get; init; }
    public required DateTime StartDate { get; init; }
    public required DateTime EndDate { get; init; }
    public required int TotalUsers { get; init; }
}

/// <summary>
/// Funnel step.
/// </summary>
public sealed class FunnelStep
{
    public required string StepName { get; init; }
    public required int UserCount { get; init; }
    public required double DropOffRate { get; init; }
}

/// <summary>
/// Widget type.
/// </summary>
public enum WidgetType
{
    Counter,
    Chart,
    Table,
    Gauge
}

/// <summary>
/// Report format.
/// </summary>
public enum ReportFormat
{
    Json,
    Csv,
    Pdf,
    Excel
}

/// <summary>
/// Time series granularity.
/// </summary>
public enum TimeSeriesGranularity
{
    Hour,
    Day,
    Week,
    Month
}

/// <summary>
/// Aggregation function.
/// </summary>
public enum AggregationFunction
{
    Count,
    Sum,
    Average,
    Min,
    Max
}
