using MediatR;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using FluentValidation;
using FluentValidation.Results;
using Serilog.Context;


namespace GameGuild.Common.Behaviors;

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
    var stopwatch = Stopwatch.StartNew();

    // Enhanced logging with request details (excluding sensitive data)
    using (LogContext.PushProperty("RequestName", requestName)) {
      using (LogContext.PushProperty("RequestId", Guid.NewGuid())) {
        logger.LogInformation(
          "Processing {RequestType}: {RequestName}",
          GetRequestType(typeof(TRequest)),
          requestName
        );

        try {
          var response = await next();
          stopwatch.Stop();

          // Handle Result pattern logging
          if (response is Result result) {
            if (result.IsSuccess) {
              logger.LogInformation(
                "Successfully completed {RequestName} in {ElapsedMilliseconds}ms",
                requestName,
                stopwatch.ElapsedMilliseconds
              );
            }
            else {
              using (LogContext.PushProperty("Error", result.Error, true)) {
                logger.LogWarning(
                  "Completed {RequestName} with error in {ElapsedMilliseconds}ms: {ErrorCode} - {ErrorDescription}",
                  requestName,
                  stopwatch.ElapsedMilliseconds,
                  result.Error.Code,
                  result.Error.Description
                );
              }
            }
          }
          else {
            logger.LogInformation(
              "Successfully completed {RequestName} in {ElapsedMilliseconds}ms",
              requestName,
              stopwatch.ElapsedMilliseconds
            );
          }

          // Performance warning for slow requests
          if (stopwatch.ElapsedMilliseconds > 1000) {
            logger.LogWarning(
              "Slow request detected: {RequestName} took {ElapsedMilliseconds}ms",
              requestName,
              stopwatch.ElapsedMilliseconds
            );
          }

          return response;
        }
        catch (Exception ex) {
          stopwatch.Stop();
          logger.LogError(
            ex,
            "Error processing {RequestName} after {ElapsedMilliseconds}ms: {ErrorMessage}",
            requestName,
            stopwatch.ElapsedMilliseconds,
            ex.Message
          );

          throw;
        }
      }
    }
  }

  private static string GetRequestType(Type requestType) {
    if (requestType.Name.EndsWith("Command")) return "Command";
    if (requestType.Name.EndsWith("Query")) return "Query";

    return "Request";
  }
}
