using System.Diagnostics.Metrics;

namespace GameGuild.Modules.Common.Metrics;

/// <summary>
/// Centralized metrics service using System.Diagnostics.Metrics for OpenTelemetry/Prometheus export.
/// </summary>
public sealed class MetricsService : IDisposable
{
    private readonly Meter _meter;
    private readonly Dictionary<string, Counter<long>> _counters = new();
    private readonly Dictionary<string, Histogram<double>> _histograms = new();
    private readonly Dictionary<string, ObservableGauge<long>> _gauges = new();
    private readonly Dictionary<string, Func<long>> _gaugeCallbacks = new();

    public MetricsService(string serviceName = "GameGuild")
    {
        _meter = new Meter(serviceName, "1.0.0");
    }

    /// <summary>
    /// Increments a counter metric.
    /// </summary>
    /// <param name="name">Metric name (e.g., "http.requests.total")</param>
    /// <param name="increment">Value to add (default: 1)</param>
    /// <param name="tags">Optional tags (e.g., method, endpoint, status_code)</param>
    public void IncrementCounter(string name, long increment = 1, params KeyValuePair<string, object?>[] tags)
    {
        if (!_counters.TryGetValue(name, out var counter))
        {
            counter = _meter.CreateCounter<long>(name, unit: null, description: $"Counter: {name}");
            _counters[name] = counter;
        }

        counter.Add(increment, tags);
    }

    /// <summary>
    /// Records a histogram value (for latency, request size, etc.).
    /// </summary>
    /// <param name="name">Metric name (e.g., "http.request.duration")</param>
    /// <param name="value">Value to record</param>
    /// <param name="tags">Optional tags</param>
    public void RecordHistogram(string name, double value, params KeyValuePair<string, object?>[] tags)
    {
        if (!_histograms.TryGetValue(name, out var histogram))
        {
            histogram = _meter.CreateHistogram<double>(name, unit: null, description: $"Histogram: {name}");
            _histograms[name] = histogram;
        }

        histogram.Record(value, tags);
    }

    /// <summary>
    /// Registers an observable gauge that is evaluated on-demand.
    /// </summary>
    /// <param name="name">Metric name (e.g., "process.memory.usage")</param>
    /// <param name="observeValue">Callback function to get current value</param>
    /// <param name="tags">Optional tags</param>
    public void RegisterGauge(string name, Func<long> observeValue, params KeyValuePair<string, object?>[] tags)
    {
        if (_gauges.ContainsKey(name))
        {
            return; // Already registered
        }

        _gaugeCallbacks[name] = observeValue;

        var gauge = _meter.CreateObservableGauge(
            name,
            () => new Measurement<long>(observeValue(), tags),
            unit: null,
            description: $"Gauge: {name}");

        _gauges[name] = gauge;
    }

    /// <summary>
    /// Records HTTP request metrics (counter + histogram).
    /// </summary>
    public void RecordHttpRequest(string method, string endpoint, int statusCode, double durationMs)
    {
        var tags = new[]
        {
            new KeyValuePair<string, object?>("method", method),
            new KeyValuePair<string, object?>("endpoint", endpoint),
            new KeyValuePair<string, object?>("status_code", statusCode)
        };

        IncrementCounter("http.requests.total", 1, tags);
        RecordHistogram("http.request.duration", durationMs, tags);
    }

    /// <summary>
    /// Records database query metrics.
    /// </summary>
    public void RecordDatabaseQuery(string operation, string table, bool success, double durationMs)
    {
        var tags = new[]
        {
            new KeyValuePair<string, object?>("operation", operation),
            new KeyValuePair<string, object?>("table", table),
            new KeyValuePair<string, object?>("success", success)
        };

        IncrementCounter("db.queries.total", 1, tags);
        RecordHistogram("db.query.duration", durationMs, tags);
    }

    /// <summary>
    /// Records cache hit/miss metrics.
    /// </summary>
    public void RecordCacheOperation(string operation, bool hit)
    {
        var tags = new[]
        {
            new KeyValuePair<string, object?>("operation", operation),
            new KeyValuePair<string, object?>("result", hit ? "hit" : "miss")
        };

        IncrementCounter("cache.operations.total", 1, tags);
    }

    /// <summary>
    /// Records permission check metrics.
    /// </summary>
    public void RecordPermissionCheck(string permission, bool granted, double durationMs)
    {
        var tags = new[]
        {
            new KeyValuePair<string, object?>("permission", permission),
            new KeyValuePair<string, object?>("result", granted ? "granted" : "denied")
        };

        IncrementCounter("permissions.checks.total", 1, tags);
        RecordHistogram("permissions.check.duration", durationMs, tags);
    }

    /// <summary>
    /// Records background job metrics.
    /// </summary>
    public void RecordBackgroundJob(string jobName, bool success, double durationMs)
    {
        var tags = new[]
        {
            new KeyValuePair<string, object?>("job", jobName),
            new KeyValuePair<string, object?>("result", success ? "success" : "failure")
        };

        IncrementCounter("background.jobs.total", 1, tags);
        RecordHistogram("background.job.duration", durationMs, tags);
    }

    public void Dispose()
    {
        _meter?.Dispose();
    }
}
