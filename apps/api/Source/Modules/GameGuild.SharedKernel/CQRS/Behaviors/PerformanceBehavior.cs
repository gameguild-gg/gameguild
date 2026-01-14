using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace GameGuild.CQRS;

/// <summary>
///     Pipeline behavior for measuring request performance
/// </summary>
/// <typeparam name="TRequest">Request type</typeparam>
/// <typeparam name="TResponse">Response type</typeparam>
public class PerformanceBehavior<TRequest, TResponse>(ILogger<PerformanceBehavior<TRequest, TResponse>> logger, int warningThresholdMs = 3000) : IPipelineBehavior<TRequest, TResponse> where TRequest : IRequestBase
{
    private readonly TimeSpan _warningThreshold = TimeSpan.FromMilliseconds(warningThresholdMs);

    /// <summary>
    ///     Handles the request pipeline with performance monitoring
    /// </summary>
    /// <param name="request">Request</param>
    /// <param name="next">Next handler delegate</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Response</returns>
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(next);

        var requestName = typeof(TRequest).Name;
        var stopwatch = Stopwatch.StartNew();

        try
        {
            var response = await next().ConfigureAwait(false);

            stopwatch.Stop();
            var elapsedMs = stopwatch.ElapsedMilliseconds;

            if (stopwatch.Elapsed > _warningThreshold) { logger.LogWarning("Slow request detected: {RequestName} took {ElapsedMs}ms", requestName, elapsedMs); }
            else { logger.LogDebug("Request {RequestName} completed in {ElapsedMs}ms", requestName, elapsedMs); }

            return response;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            logger.LogError(ex, "Request {RequestName} failed after {ElapsedMs}ms", requestName, stopwatch.ElapsedMilliseconds);

            throw;
        }
    }
}
