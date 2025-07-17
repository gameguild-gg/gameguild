using System.Diagnostics;
using MediatR;


namespace GameGuild.Common;

/// <summary>
/// Performance monitoring behavior for tracking slow requests and memory usage
/// </summary>
public class PerformanceBehavior<TRequest, TResponse>(ILogger<PerformanceBehavior<TRequest, TResponse>> logger, IDateTimeProvider dateTimeProvider) : IPipelineBehavior<TRequest, TResponse> where TRequest : IRequest<TResponse> {
  private const int SlowRequestThresholdMs = 1000; // 1 second
  private const int CriticalRequestThresholdMs = 5000; // 5 seconds

  public async Task<TResponse> Handle(
    TRequest request,
    RequestHandlerDelegate<TResponse> next,
    CancellationToken cancellationToken
  ) {
    var requestName = typeof(TRequest).Name;
    var stopwatch = Stopwatch.StartNew();
    var startTime = dateTimeProvider.UtcNow;

    // Memory usage before processing
    var memoryBefore = GC.GetTotalMemory(false);

    try {
      var response = await next();
      stopwatch.Stop();

      // Memory usage after processing
      var memoryAfter = GC.GetTotalMemory(false);
      var memoryUsed = memoryAfter - memoryBefore;

      LogPerformanceMetrics(requestName, stopwatch.ElapsedMilliseconds, memoryUsed, startTime);

      return response;
    }
    catch (Exception) {
      stopwatch.Stop();
      var memoryAfter = GC.GetTotalMemory(false);
      var memoryUsed = memoryAfter - memoryBefore;

      LogPerformanceMetrics(requestName, stopwatch.ElapsedMilliseconds, memoryUsed, startTime, true);

      throw;
    }
  }

  private void LogPerformanceMetrics(string requestName, long elapsedMilliseconds, long memoryUsed, DateTime startTime, bool hasError = false) {
    var logLevel = GetLogLevel(elapsedMilliseconds, hasError);
    var memoryUsedKb = memoryUsed / 1024.0;

    logger.Log(
      logLevel,
      "Performance: {RequestName} | Duration: {ElapsedMs}ms | Memory: {MemoryKB:F2}KB | Started: {StartTime} | Status: {Status}",
      requestName,
      elapsedMilliseconds,
      memoryUsedKb,
      startTime.ToString("yyyy-MM-dd HH:mm:ss.fff"),
      hasError ? "ERROR" : "SUCCESS"
    );

    // Additional warnings for resource-intensive operations
    if (memoryUsedKb > 1024) // More than 1MB
      logger.LogWarning(
        "High memory usage detected: {RequestName} used {MemoryKB:F2}KB",
        requestName,
        memoryUsedKb
      );

    if (elapsedMilliseconds > CriticalRequestThresholdMs)
      logger.LogCritical(
        "Critical performance issue: {RequestName} took {ElapsedMs}ms",
        requestName,
        elapsedMilliseconds
      );
  }

  private static LogLevel GetLogLevel(long elapsedMilliseconds, bool hasError) {
    if (hasError) return LogLevel.ErrorMessage;

    return elapsedMilliseconds switch { > CriticalRequestThresholdMs => LogLevel.Critical, > SlowRequestThresholdMs => LogLevel.Warning, _ => LogLevel.Information };
  }
}
