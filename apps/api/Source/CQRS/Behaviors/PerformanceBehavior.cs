using System.Diagnostics;


namespace GameGuild.CQRS;

/// <summary>
/// Pipeline behavior for measuring request performance
/// </summary>
/// <typeparam name="TRequest">Request type</typeparam>
/// <typeparam name="TResponse">Response type</typeparam>
public class PerformanceBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IBaseRequest
{
    private readonly ILogger<PerformanceBehavior<TRequest, TResponse>> _logger;

    private readonly TimeSpan _warningThreshold;

    /// <summary>
    /// Initializes a new instance of the PerformanceBehavior class
    /// </summary>
    /// <param name="logger">Logger</param>
    /// <param name="warningThresholdMs">Warning threshold in milliseconds</param>
    public PerformanceBehavior(ILogger<PerformanceBehavior<TRequest, TResponse>> logger, int warningThresholdMs = 3000)
    {
        _logger = logger;
        _warningThreshold = TimeSpan.FromMilliseconds(warningThresholdMs);
    }

    /// <summary>
    /// Handles the request pipeline with performance monitoring
    /// </summary>
    /// <param name="request">Request</param>
    /// <param name="next">Next handler delegate</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Response</returns>
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegateBase<TResponse> next, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(next);

        var requestName = typeof(TRequest).Name;
        var stopwatch = Stopwatch.StartNew();

        try
        {
            var response = await next().ConfigureAwait(false);

            stopwatch.Stop();
            var elapsedMs = stopwatch.ElapsedMilliseconds;

            if (stopwatch.Elapsed > _warningThreshold)
            {
                _logger.LogWarning("Slow request detected: {RequestName} took {ElapsedMs}ms", requestName, elapsedMs);
            }
            else
            {
                _logger.LogDebug("Request {RequestName} completed in {ElapsedMs}ms", requestName, elapsedMs);
            }

            return response;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Request {RequestName} failed after {ElapsedMs}ms", requestName, stopwatch.ElapsedMilliseconds);

            throw;
        }
    }
}
