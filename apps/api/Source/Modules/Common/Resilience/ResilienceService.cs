using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace GameGuild.Modules.Common.Resilience;

/// <summary>
/// Implementation of resilience service using manual retry/circuit breaker logic
/// (Polly-free implementation for simplicity and control)
/// </summary>
internal sealed class ResilienceService : IResilienceService
{
    private readonly ILogger<ResilienceService> _logger;
    private readonly ConcurrentDictionary<string, CircuitBreakerState> _circuitBreakers = new();

    public ResilienceService(ILogger<ResilienceService> logger)
    {
        _logger = logger;
    }

    public async Task<TResult> ExecuteWithRetryAsync<TResult>(
        Func<CancellationToken, Task<TResult>> operation,
        int maxRetries = 3,
        CancellationToken cancellationToken = default)
    {
        var attempt = 0;
        var delay = TimeSpan.FromMilliseconds(100);

        while (true)
        {
            attempt++;
            try
            {
                return await operation(cancellationToken);
            }
            catch (Exception ex) when (attempt <= maxRetries && IsTransientException(ex))
            {
                _logger.LogWarning(ex,
                    "Operation failed (attempt {Attempt}/{MaxRetries}). Retrying after {Delay}ms",
                    attempt, maxRetries, delay.TotalMilliseconds);

                await Task.Delay(delay, cancellationToken);
                delay = TimeSpan.FromMilliseconds(delay.TotalMilliseconds * 2); // Exponential backoff
            }
        }
    }

    public async Task<TResult> ExecuteWithCircuitBreakerAsync<TResult>(
        Func<CancellationToken, Task<TResult>> operation,
        int failureThreshold = 5,
        TimeSpan? breakDuration = null,
        CancellationToken cancellationToken = default)
    {
        var operationKey = operation.Method.Name;
        var circuitBreaker = _circuitBreakers.GetOrAdd(operationKey, _ => new CircuitBreakerState
        {
            FailureThreshold = failureThreshold,
            BreakDuration = breakDuration ?? TimeSpan.FromSeconds(30)
        });

        // Check if circuit is open
        if (circuitBreaker.IsOpen)
        {
            if (DateTime.UtcNow < circuitBreaker.OpenUntil)
            {
                _logger.LogWarning("Circuit breaker is OPEN for {Operation}. Rejecting request.", operationKey);
                throw new InvalidOperationException($"Circuit breaker is open for operation: {operationKey}");
            }

            // Try half-open state
            circuitBreaker.State = CircuitState.HalfOpen;
            _logger.LogInformation("Circuit breaker entering HALF-OPEN state for {Operation}", operationKey);
        }

        try
        {
            var result = await operation(cancellationToken);

            // Success - reset circuit breaker
            if (circuitBreaker.State == CircuitState.HalfOpen)
            {
                circuitBreaker.Close();
                _logger.LogInformation("Circuit breaker CLOSED for {Operation}", operationKey);
            }
            else
            {
                circuitBreaker.ResetFailureCount();
            }

            return result;
        }
        catch (Exception ex)
        {
            circuitBreaker.RecordFailure();

            if (circuitBreaker.FailureCount >= circuitBreaker.FailureThreshold)
            {
                circuitBreaker.Open();
                _logger.LogError(ex, "Circuit breaker OPENED for {Operation} after {Failures} failures",
                    operationKey, circuitBreaker.FailureCount);
            }

            throw;
        }
    }

    public async Task<TResult> ExecuteWithTimeoutAsync<TResult>(
        Func<CancellationToken, Task<TResult>> operation,
        TimeSpan timeout,
        CancellationToken cancellationToken = default)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(timeout);

        try
        {
            return await operation(cts.Token);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogError("Operation timed out after {Timeout}ms", timeout.TotalMilliseconds);
            throw new TimeoutException($"Operation timed out after {timeout.TotalSeconds} seconds");
        }
    }

    public async Task<TResult> ExecuteWithPolicyAsync<TResult>(
        Func<CancellationToken, Task<TResult>> operation,
        ResiliencePolicy policy,
        CancellationToken cancellationToken = default)
    {
        Func<CancellationToken, Task<TResult>> wrappedOperation = operation;

        // Wrap with timeout (innermost)
        if (policy.EnableTimeout)
        {
            var timeoutOperation = wrappedOperation;
            wrappedOperation = ct => ExecuteWithTimeoutAsync(timeoutOperation, policy.Timeout, ct);
        }

        // Wrap with circuit breaker
        if (policy.EnableCircuitBreaker)
        {
            var circuitBreakerOperation = wrappedOperation;
            wrappedOperation = ct => ExecuteWithCircuitBreakerAsync(
                circuitBreakerOperation,
                policy.CircuitBreakerFailureThreshold,
                policy.CircuitBreakerBreakDuration,
                ct);
        }

        // Wrap with retry (outermost)
        if (policy.EnableRetry)
        {
            var retryOperation = wrappedOperation;
            wrappedOperation = ct => ExecuteWithRetryAsync(retryOperation, policy.MaxRetries, ct);
        }

        return await wrappedOperation(cancellationToken);
    }

    private static bool IsTransientException(Exception ex)
    {
        // Common transient exceptions
        return ex is TimeoutException
            || ex is TaskCanceledException
            || ex is HttpRequestException
            || (ex.InnerException != null && IsTransientException(ex.InnerException));
    }
}

internal sealed class CircuitBreakerState
{
    public CircuitState State { get; set; } = CircuitState.Closed;
    public int FailureCount { get; private set; }
    public int FailureThreshold { get; init; }
    public TimeSpan BreakDuration { get; init; }
    public DateTime OpenUntil { get; private set; }

    public bool IsOpen => State == CircuitState.Open && DateTime.UtcNow < OpenUntil;

    public void RecordFailure()
    {
        FailureCount++;
    }

    public void ResetFailureCount()
    {
        FailureCount = 0;
    }

    public void Open()
    {
        State = CircuitState.Open;
        OpenUntil = DateTime.UtcNow.Add(BreakDuration);
    }

    public void Close()
    {
        State = CircuitState.Closed;
        FailureCount = 0;
    }
}

internal enum CircuitState
{
    Closed,
    Open,
    HalfOpen
}
