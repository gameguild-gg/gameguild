using MediatR;
using System.Diagnostics;


namespace GameGuild.Common.Behaviors;

/// <summary>
/// Pipeline behavior for logging requests and performance monitoring
/// </summary>
public class LoggingBehavior<TRequest, TResponse>(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
  : IPipelineBehavior<TRequest, TResponse> where TRequest : IRequest<TResponse> {
  public async Task<TResponse> Handle(
    TRequest request, RequestHandlerDelegate<TResponse> next,
    CancellationToken cancellationToken
  ) {
    string requestName = typeof(TRequest).Name;
    var stopwatch = Stopwatch.StartNew();

    logger.LogInformation("Handling {RequestName}", requestName);

    try {
      TResponse response = await next();

      stopwatch.Stop();
      logger.LogInformation(
        "Handled {RequestName} in {ElapsedMilliseconds}ms",
        requestName,
        stopwatch.ElapsedMilliseconds
      );

      return response;
    }
    catch (Exception ex) {
      stopwatch.Stop();
      logger.LogError(
        ex,
        "Error handling {RequestName} after {ElapsedMilliseconds}ms",
        requestName,
        stopwatch.ElapsedMilliseconds
      );

      throw;
    }
  }
}
