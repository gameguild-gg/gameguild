using System.Diagnostics;


namespace GameGuild.Modules.Common.Metrics;

/// <summary>
/// Middleware to automatically record HTTP request metrics.
/// </summary>
public sealed class MetricsMiddleware
{
    private readonly RequestDelegate _next;
    private readonly MetricsService _metrics;
    private readonly ILogger<MetricsMiddleware> _logger;

    public MetricsMiddleware(
        RequestDelegate next,
        MetricsService metrics,
        ILogger<MetricsMiddleware> logger)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _metrics = metrics ?? throw new ArgumentNullException(nameof(metrics));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var sw = Stopwatch.StartNew();
        var originalBodyStream = context.Response.Body;

        try
        {
            await _next(context);
        }
        finally
        {
            sw.Stop();

            try
            {
                var method = context.Request.Method;
                var endpoint = context.Request.Path.Value ?? "/";
                var statusCode = context.Response.StatusCode;
                var durationMs = sw.Elapsed.TotalMilliseconds;

                _metrics.RecordHttpRequest(method, endpoint, statusCode, durationMs);

                _logger.LogDebug(
                    "HTTP {Method} {Endpoint} - {StatusCode} in {Duration}ms",
                    method, endpoint, statusCode, durationMs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to record HTTP metrics");
            }
        }
    }
}
