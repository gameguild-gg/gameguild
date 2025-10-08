using System.Diagnostics;
using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace GameGuild.Core.Services;

/// <summary>
/// Service for monitoring performance metrics
/// </summary>
public interface IPerformanceMonitoringService
{
    /// <summary>
    /// Records operation timing
    /// </summary>
    void RecordOperationTiming(string operationName, TimeSpan duration);

    /// <summary>
    /// Increments a counter
    /// </summary>
    void IncrementCounter(string counterName);

    /// <summary>
    /// Sets a gauge value
    /// </summary>
    void SetGauge(string gaugeName, double value);

    /// <summary>
    /// Gets performance statistics
    /// </summary>
    PerformanceStatistics GetStatistics();

    /// <summary>
    /// Starts a timing scope
    /// </summary>
    IDisposable StartTimingScope(string operationName);
}

public class PerformanceMonitoringService : IPerformanceMonitoringService
{
    private readonly ILogger<PerformanceMonitoringService> _logger;
    private readonly ConcurrentDictionary<string, List<TimeSpan>> _timings = new();
    private readonly ConcurrentDictionary<string, long> _counters = new();
    private readonly ConcurrentDictionary<string, double> _gauges = new();

    public PerformanceMonitoringService(ILogger<PerformanceMonitoringService> logger)
    {
        _logger = logger;
    }

    public void RecordOperationTiming(string operationName, TimeSpan duration)
    {
        _timings.AddOrUpdate(
            operationName,
            _ => new List<TimeSpan> { duration },
            (_, list) =>
            {
                lock (list)
                {
                    list.Add(duration);
                    // Keep only last 1000 entries
                    if (list.Count > 1000)
                    {
                        list.RemoveAt(0);
                    }
                }
                return list;
            });

        _logger.LogDebug("Operation {OperationName} completed in {Duration}ms",
            operationName, duration.TotalMilliseconds);
    }

    public void IncrementCounter(string counterName)
    {
        _counters.AddOrUpdate(counterName, 1, (_, current) => current + 1);
    }

    public void SetGauge(string gaugeName, double value)
    {
        _gauges[gaugeName] = value;
    }

    public PerformanceStatistics GetStatistics()
    {
        var operationStats = new Dictionary<string, OperationStatistics>();

        foreach (var (operationName, timings) in _timings)
        {
            lock (timings)
            {
                if (timings.Count > 0)
                {
                    var orderedTimings = timings.OrderBy(t => t).ToList();
                    operationStats[operationName] = new OperationStatistics
                    {
                        Count = timings.Count,
                        Average = TimeSpan.FromTicks((long)timings.Average(t => t.Ticks)),
                        Min = timings.Min(),
                        Max = timings.Max(),
                        P50 = orderedTimings[orderedTimings.Count / 2],
                        P95 = orderedTimings[(int)(orderedTimings.Count * 0.95)],
                        P99 = orderedTimings[(int)(orderedTimings.Count * 0.99)]
                    };
                }
            }
        }

        return new PerformanceStatistics
        {
            Operations = operationStats,
            Counters = new Dictionary<string, long>(_counters),
            Gauges = new Dictionary<string, double>(_gauges)
        };
    }

    public IDisposable StartTimingScope(string operationName)
    {
        return new TimingScope(this, operationName);
    }

    private class TimingScope : IDisposable
    {
        private readonly PerformanceMonitoringService _service;
        private readonly string _operationName;
        private readonly Stopwatch _stopwatch;

        public TimingScope(PerformanceMonitoringService service, string operationName)
        {
            _service = service;
            _operationName = operationName;
            _stopwatch = Stopwatch.StartNew();
        }

        public void Dispose()
        {
            _stopwatch.Stop();
            _service.RecordOperationTiming(_operationName, _stopwatch.Elapsed);
        }
    }
}

public class PerformanceStatistics
{
    public Dictionary<string, OperationStatistics> Operations { get; set; } = new();
    public Dictionary<string, long> Counters { get; set; } = new();
    public Dictionary<string, double> Gauges { get; set; } = new();
}

public class OperationStatistics
{
    public int Count { get; set; }
    public TimeSpan Average { get; set; }
    public TimeSpan Min { get; set; }
    public TimeSpan Max { get; set; }
    public TimeSpan P50 { get; set; }
    public TimeSpan P95 { get; set; }
    public TimeSpan P99 { get; set; }
}
