namespace GameGuild.Modules.Common.Chaos;

/// <summary>
/// Chaos policy interface for fault injection.
/// </summary>
public interface IChaosPolicy
{
    /// <summary>
    /// Gets the name of the chaos policy.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets whether the chaos policy is currently enabled.
    /// </summary>
    bool IsEnabled { get; }

    /// <summary>
    /// Gets the probability of fault injection (0.0 - 1.0).
    /// </summary>
    double InjectionProbability { get; }

    /// <summary>
    /// Executes the chaos policy, potentially injecting faults.
    /// </summary>
    Task ExecuteAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Enables the chaos policy.
    /// </summary>
    void Enable();

    /// <summary>
    /// Disables the chaos policy.
    /// </summary>
    void Disable();

    /// <summary>
    /// Sets the injection probability (0.0 - 1.0).
    /// </summary>
    void SetInjectionProbability(double probability);
}

/// <summary>
/// Chaos fault types.
/// </summary>
public enum ChaosFaultType
{
    /// <summary>
    /// No fault injected.
    /// </summary>
    None,

    /// <summary>
    /// Inject latency delay.
    /// </summary>
    Latency,

    /// <summary>
    /// Throw an exception.
    /// </summary>
    Exception,

    /// <summary>
    /// Return null or empty result.
    /// </summary>
    Null,

    /// <summary>
    /// Return corrupted data.
    /// </summary>
    Corruption,

    /// <summary>
    /// Simulate timeout.
    /// </summary>
    Timeout
}

/// <summary>
/// Chaos injection result.
/// </summary>
public sealed class ChaosInjectionResult
{
    public required string PolicyName { get; init; }
    public required ChaosFaultType FaultType { get; init; }
    public bool WasInjected { get; init; }
    public DateTime InjectedAt { get; init; }
    public TimeSpan? Latency { get; init; }
    public Exception? Exception { get; init; }
    public string? Message { get; init; }
}

/// <summary>
/// Chaos policy configuration.
/// </summary>
public sealed class ChaosPolicyConfig
{
    public required string Name { get; init; }
    public bool IsEnabled { get; init; }
    public double InjectionProbability { get; init; } = 0.1;
    public ChaosFaultType FaultType { get; init; }
    public TimeSpan? LatencyMin { get; init; }
    public TimeSpan? LatencyMax { get; init; }
    public string? ExceptionMessage { get; init; }
    public Type? ExceptionType { get; init; }
}
