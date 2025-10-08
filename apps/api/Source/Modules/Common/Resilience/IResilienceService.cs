namespace GameGuild.Modules.Common.Resilience;

/// <summary>
/// Service for executing operations with resilience policies (retry, circuit breaker, timeout)
/// </summary>
public interface IResilienceService
{
    /// <summary>
    /// Executes an async operation with retry policy
    /// </summary>
    Task<TResult> ExecuteWithRetryAsync<TResult>(
        Func<CancellationToken, Task<TResult>> operation,
        int maxRetries = 3,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes an async operation with circuit breaker policy
    /// </summary>
    Task<TResult> ExecuteWithCircuitBreakerAsync<TResult>(
        Func<CancellationToken, Task<TResult>> operation,
        int failureThreshold = 5,
        TimeSpan? breakDuration = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes an async operation with timeout policy
    /// </summary>
    Task<TResult> ExecuteWithTimeoutAsync<TResult>(
        Func<CancellationToken, Task<TResult>> operation,
        TimeSpan timeout,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes an async operation with combined policies (retry + circuit breaker + timeout)
    /// </summary>
    Task<TResult> ExecuteWithPolicyAsync<TResult>(
        Func<CancellationToken, Task<TResult>> operation,
        ResiliencePolicy policy,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Configuration for resilience policies
/// </summary>
public sealed class ResiliencePolicy
{
    public int MaxRetries { get; init; } = 3;
    public TimeSpan InitialDelay { get; init; } = TimeSpan.FromMilliseconds(100);
    public double BackoffMultiplier { get; init; } = 2.0;
    public int CircuitBreakerFailureThreshold { get; init; } = 5;
    public TimeSpan CircuitBreakerBreakDuration { get; init; } = TimeSpan.FromSeconds(30);
    public TimeSpan Timeout { get; init; } = TimeSpan.FromSeconds(30);
    public bool EnableRetry { get; init; } = true;
    public bool EnableCircuitBreaker { get; init; } = true;
    public bool EnableTimeout { get; init; } = true;

    public static ResiliencePolicy Default => new();

    public static ResiliencePolicy HttpClient => new()
    {
        MaxRetries = 3,
        InitialDelay = TimeSpan.FromMilliseconds(200),
        BackoffMultiplier = 2.0,
        CircuitBreakerFailureThreshold = 10,
        CircuitBreakerBreakDuration = TimeSpan.FromMinutes(1),
        Timeout = TimeSpan.FromSeconds(30)
    };

    public static ResiliencePolicy Database => new()
    {
        MaxRetries = 2,
        InitialDelay = TimeSpan.FromMilliseconds(50),
        BackoffMultiplier = 1.5,
        CircuitBreakerFailureThreshold = 5,
        CircuitBreakerBreakDuration = TimeSpan.FromSeconds(15),
        Timeout = TimeSpan.FromSeconds(10)
    };
}
