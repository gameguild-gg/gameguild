using System.Diagnostics;
using MediatR;


namespace GameGuild.Common;

/// <summary>
/// Unified logging behavior that supports both MediatR and Result patterns
/// </summary>
public class LoggingBehavior<TRequest, TResponse>(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
  : IPipelineBehavior<TRequest, TResponse> where TRequest : IRequest<TResponse> {
  public async Task<TResponse> Handle(
    TRequest request,
    RequestHandlerDelegate<TResponse> next,
    CancellationToken cancellationToken
  ) {
    var requestName = typeof(TRequest).Name;
    var requestId = Guid.NewGuid();
    var stopwatch = Stopwatch.StartNew();

    // Enhanced logging with request details (excluding sensitive data)
    logger.LogInformation(
      "Processing {RequestType}: {RequestName} (RequestId: {RequestId})",
      GetRequestType(typeof(TRequest)),
      requestName,
      requestId
    );

    try {
      var response = await next();
      stopwatch.Stop();

      // Handle Result pattern logging
      if (response is Result result) {
        if (result.IsSuccess)
          logger.LogInformation(
            "Successfully completed {RequestName} in {ElapsedMilliseconds}ms (RequestId: {RequestId})",
            requestName,
            stopwatch.ElapsedMilliseconds,
            requestId
          );
        else
          logger.LogWarning(
            "Completed {RequestName} with error in {ElapsedMilliseconds}ms: {ErrorCode} - {ErrorDescription} (RequestId: {RequestId})",
            requestName,
            stopwatch.ElapsedMilliseconds,
            result.ErrorMessage.Code,
            result.ErrorMessage.Description,
            requestId
          );
      }
      else {
        logger.LogInformation(
          "Successfully completed {RequestName} in {ElapsedMilliseconds}ms (RequestId: {RequestId})",
          requestName,
          stopwatch.ElapsedMilliseconds,
          requestId
        );
      }

      // Performance warning for slow requests
      if (stopwatch.ElapsedMilliseconds > 1000)
        logger.LogWarning(
          "Slow request detected: {RequestName} took {ElapsedMilliseconds}ms (RequestId: {RequestId})",
          requestName,
          stopwatch.ElapsedMilliseconds,
          requestId
        );

      return response;
    }
    catch (Exception ex) {
      stopwatch.Stop();
      logger.LogError(
        ex,
        "ErrorMessage processing {RequestName} after {ElapsedMilliseconds}ms: {ErrorMessage} (RequestId: {RequestId})",
        requestName,
        stopwatch.ElapsedMilliseconds,
        ex.Message,
        requestId
      );

      throw;
    }
  }

  private static string GetRequestType(Type requestType) {
    if (requestType.Name.EndsWith("Command")) return "Command";
    if (requestType.Name.EndsWith("Query")) return "Query";

    return "Request";
  }
}
