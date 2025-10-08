using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace GameGuild.Modules.Common.Chaos;

/// <summary>
/// Latency injection chaos policy.
/// </summary>
public sealed class LatencyInjectionPolicy : IChaosPolicy
{
    private readonly ILogger<LatencyInjectionPolicy> _logger;
    private readonly Random _random = new();
    private readonly TimeSpan _latencyMin;
    private readonly TimeSpan _latencyMax;
    private bool _isEnabled;
    private double _injectionProbability;

    public string Name { get; }

    public bool IsEnabled => _isEnabled;

    public double InjectionProbability => _injectionProbability;

    public LatencyInjectionPolicy(
        ILogger<LatencyInjectionPolicy> logger,
        string name,
        TimeSpan latencyMin,
        TimeSpan latencyMax,
        double injectionProbability = 0.1,
        bool isEnabled = false)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        Name = name ?? throw new ArgumentNullException(nameof(name));
        _latencyMin = latencyMin;
        _latencyMax = latencyMax;
        _injectionProbability = injectionProbability;
        _isEnabled = isEnabled;

        if (latencyMin > latencyMax)
            throw new ArgumentException("Latency min cannot be greater than latency max");

        if (injectionProbability is < 0 or > 1)
            throw new ArgumentOutOfRangeException(nameof(injectionProbability), "Must be between 0.0 and 1.0");
    }

    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        if (!_isEnabled)
            return;

        var shouldInject = _random.NextDouble() < _injectionProbability;
        if (!shouldInject)
            return;

        var latencyMs = _random.Next((int)_latencyMin.TotalMilliseconds, (int)_latencyMax.TotalMilliseconds);
        var latency = TimeSpan.FromMilliseconds(latencyMs);

        _logger.LogWarning(
            "[CHAOS] Injecting latency: {Latency}ms (policy: {PolicyName}, probability: {Probability:P0})",
            latencyMs, Name, _injectionProbability);

        var stopwatch = Stopwatch.StartNew();
        await Task.Delay(latency, cancellationToken);
        stopwatch.Stop();

        _logger.LogInformation(
            "[CHAOS] Latency injection completed: {ActualLatency}ms",
            stopwatch.ElapsedMilliseconds);
    }

    public void Enable()
    {
        _isEnabled = true;
        _logger.LogWarning("[CHAOS] Latency injection policy '{PolicyName}' ENABLED", Name);
    }

    public void Disable()
    {
        _isEnabled = false;
        _logger.LogInformation("[CHAOS] Latency injection policy '{PolicyName}' disabled", Name);
    }

    public void SetInjectionProbability(double probability)
    {
        if (probability is < 0 or > 1)
            throw new ArgumentOutOfRangeException(nameof(probability), "Must be between 0.0 and 1.0");

        _injectionProbability = probability;
        _logger.LogInformation(
            "[CHAOS] Latency injection probability updated: {OldProbability:P0} → {NewProbability:P0}",
            _injectionProbability, probability);
    }
}
