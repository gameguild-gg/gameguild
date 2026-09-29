using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace GameGuild.API.Core.ApiVersioning;

/// <summary>
/// Records bounded-cardinality API version request and latency metrics.
/// </summary>
public sealed class ApiVersionUsageMetrics
{
    public const string MeterName = "GameGuild.API.Versioning";

    private static readonly Meter Meter = new(MeterName, "1.0.0");
    private static readonly Counter<long> RequestCount = Meter.CreateCounter<long>(
        "gameguild.api.versioning.requests",
        "{request}",
        "Number of API requests grouped by selected API version and route.");
    private static readonly Histogram<double> RequestDuration = Meter.CreateHistogram<double>(
        "gameguild.api.versioning.request.duration",
        "ms",
        "Duration of API requests grouped by selected API version and route.");

    /// <summary>
    /// Records one request without recording raw or unrecognized client-supplied version strings.
    /// </summary>
    public void RecordRequest(string version, string route, string method, int statusCode, double durationMilliseconds)
    {
        var tags = new TagList
        {
            { "api.version", version },
            { "http.route", route },
            { "http.request.method", method },
            { "http.response.status_code", statusCode }
        };

        RequestCount.Add(1, tags);
        RequestDuration.Record(durationMilliseconds, tags);
    }
}