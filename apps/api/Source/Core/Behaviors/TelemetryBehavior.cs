using System.Diagnostics;
using GameGuild.Core.Configuration;
using GameGuild.CQRS;

namespace GameGuild.Core.Behaviors;

/// <summary>
/// MediatR pipeline behavior that adds OpenTelemetry tracing and metrics to CQRS operations.
/// Tracks execution time, operation types, and success/failure rates.
/// </summary>
public class TelemetryBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IBaseRequest {

    private readonly ILogger<TelemetryBehavior<TRequest, TResponse>> _logger;

    public TelemetryBehavior(ILogger<TelemetryBehavior<TRequest, TResponse>> logger) {
        _logger = logger;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegateBase<TResponse> next, CancellationToken cancellationToken) {
        ArgumentNullException.ThrowIfNull(next);

        var requestName = typeof(TRequest).Name;
        var operationType = DetermineOperationType(requestName);
        var isCommand = operationType == "Command";

        // Start activity for distributed tracing
        using var activity = OpenTelemetryConfiguration.CqrsActivitySource.StartActivity($"CQRS.{operationType}");
        activity?.SetTag("operation.type", operationType);
        activity?.SetTag("operation.name", requestName);
        activity?.SetTag("cqrs.request_type", typeof(TRequest).FullName);
        activity?.SetTag("cqrs.response_type", typeof(TResponse).FullName);

        // Add correlation ID if available
        if (Activity.Current?.GetBaggageItem("CorrelationId") is string correlationId) {
            activity?.SetTag("correlation.id", correlationId);
        }

        var stopwatch = Stopwatch.StartNew();
        var success = false;
        Exception? exception = null;

        try {
            _logger.LogDebug("Executing {OperationType} {RequestName}", operationType, requestName);

            var result = await next().ConfigureAwait(false);
            success = true;

            // Check if result indicates success for Result<T> pattern
            if (result is IResult resultPattern) {
                success = resultPattern.IsSuccess;
                if (!success && resultPattern.Error != null) {
                    activity?.SetTag("error.message", resultPattern.Error.Message);
                    activity?.SetTag("error.code", resultPattern.Error.Code);
                }
            }

            return result;
        }
        catch (Exception ex) {
            exception = ex;
            success = false;
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.SetTag("error.type", ex.GetType().Name);
            activity?.SetTag("error.message", ex.Message);
            throw;
        }
        finally {
            stopwatch.Stop();
            var durationMs = stopwatch.Elapsed.TotalMilliseconds;

            // Record metrics
            if (isCommand) {
                OpenTelemetryConfiguration.CommandCounter.Add(1,
                    new KeyValuePair<string, object?>("command.name", requestName),
                    new KeyValuePair<string, object?>("success", success));
                OpenTelemetryConfiguration.CommandDuration.Record(durationMs,
                    new KeyValuePair<string, object?>("command.name", requestName),
                    new KeyValuePair<string, object?>("success", success));
            }
            else {
                OpenTelemetryConfiguration.QueryCounter.Add(1,
                    new KeyValuePair<string, object?>("query.name", requestName),
                    new KeyValuePair<string, object?>("success", success));
                OpenTelemetryConfiguration.QueryDuration.Record(durationMs,
                    new KeyValuePair<string, object?>("query.name", requestName),
                    new KeyValuePair<string, object?>("success", success));
            }

            // Add telemetry tags
            activity?.SetTag("operation.duration_ms", durationMs);
            activity?.SetTag("operation.success", success);

            // Log completion
            if (success) {
                _logger.LogDebug("Completed {OperationType} {RequestName} in {Duration}ms",
                    operationType, requestName, durationMs);
            }
            else {
                _logger.LogWarning("Failed {OperationType} {RequestName} in {Duration}ms: {Error}",
                    operationType, requestName, durationMs, exception?.Message ?? "Operation failed");
            }
        }
    }

    private static string DetermineOperationType(string requestName) {
        if (requestName.EndsWith("Command", StringComparison.OrdinalIgnoreCase)) {
            return "Command";
        }
        if (requestName.EndsWith("Query", StringComparison.OrdinalIgnoreCase)) {
            return "Query";
        }
        return "Request";
    }
}

/// <summary>
/// Interface to check if a result indicates success
/// </summary>
public interface IResult {
    bool IsSuccess { get; }
    IError? Error { get; }
}

/// <summary>
/// Interface for error information
/// </summary>
public interface IError {
    string Code { get; }
    string Message { get; }
}
