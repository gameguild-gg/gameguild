using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GameGuild.CQRS;

/// <summary>
///     Configuration options for the observability pipeline behavior.
/// </summary>
public sealed class ObservabilityOptions
{
    /// <summary>
    ///     Threshold in milliseconds above which a request is considered slow and logged as a warning.
    ///     Default is 3000ms (3 seconds).
    /// </summary>
    public int WarningThresholdMs { get; set; } = 3000;
}

/// <summary>
///     Unified pipeline behavior for request observability: logging, timing, and exception reporting.
///     Replaces the separate LoggingBehavior and PerformanceBehavior to eliminate DRY violation
///     (both were independently logging entry, exit, timing, and exceptions).
/// </summary>
/// <typeparam name="TRequest">Request type</typeparam>
/// <typeparam name="TResponse">Response type</typeparam>
public sealed class ObservabilityBehavior<TRequest, TResponse>(
    ILogger<ObservabilityBehavior<TRequest, TResponse>> logger,
    IOptions<ObservabilityOptions> options) : IPipelineBehavior<TRequest, TResponse> where TRequest : IRequestBase
{
    private static readonly string RequestName = typeof(TRequest).Name;
    private const string NoActivityCorrelation = "no-activity";
    private readonly TimeSpan _warningThreshold = TimeSpan.FromMilliseconds(options.Value.WarningThresholdMs);

    /// <summary>
    ///     Handles the request pipeline with logging and performance monitoring.
    ///     Uses Activity.Current?.Id for correlation (integrates with OpenTelemetry/APM tools)
    ///     instead of generating a useless random Guid.
    /// </summary>
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(next);

        var correlationId = Activity.Current?.Id ?? NoActivityCorrelation;

        logger.LogDebug("Handling {RequestName} [{CorrelationId}]", RequestName, correlationId);

        var stopwatch = Stopwatch.StartNew();

        try
        {
            var response = await next().ConfigureAwait(false);

            stopwatch.Stop();
            var elapsedMs = stopwatch.ElapsedMilliseconds;

            if (stopwatch.Elapsed > _warningThreshold)
            {
                logger.LogWarning(
                    "Slow request: {RequestName} took {ElapsedMs}ms [{CorrelationId}]",
                    RequestName, elapsedMs, correlationId);
            }
            else
            {
                logger.LogDebug(
                    "Handled {RequestName} in {ElapsedMs}ms [{CorrelationId}]",
                    RequestName, elapsedMs, correlationId);
            }

            return response;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            logger.LogError(ex,
                "Request {RequestName} failed after {ElapsedMs}ms [{CorrelationId}]",
                RequestName, stopwatch.ElapsedMilliseconds, correlationId);

            throw;
        }
    }
}
