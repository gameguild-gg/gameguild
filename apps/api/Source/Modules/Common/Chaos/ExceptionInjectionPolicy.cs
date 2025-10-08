using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace GameGuild.Modules.Common.Chaos;

/// <summary>
/// Exception injection chaos policy.
/// </summary>
public sealed class ExceptionInjectionPolicy : IChaosPolicy
{
    private readonly ILogger<ExceptionInjectionPolicy> _logger;
    private readonly Random _random = new();
    private readonly Type _exceptionType;
    private readonly string _exceptionMessage;
    private bool _isEnabled;
    private double _injectionProbability;

    public string Name { get; }

    public bool IsEnabled => _isEnabled;

    public double InjectionProbability => _injectionProbability;

    public ExceptionInjectionPolicy(
        ILogger<ExceptionInjectionPolicy> logger,
        string name,
        Type? exceptionType = null,
        string? exceptionMessage = null,
        double injectionProbability = 0.1,
        bool isEnabled = false)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        Name = name ?? throw new ArgumentNullException(nameof(name));
        _exceptionType = exceptionType ?? typeof(InvalidOperationException);
        _exceptionMessage = exceptionMessage ?? "Chaos engineering exception injection";
        _injectionProbability = injectionProbability;
        _isEnabled = isEnabled;

        if (!typeof(Exception).IsAssignableFrom(_exceptionType))
            throw new ArgumentException("Exception type must derive from System.Exception", nameof(exceptionType));

        if (injectionProbability is < 0 or > 1)
            throw new ArgumentOutOfRangeException(nameof(injectionProbability), "Must be between 0.0 and 1.0");
    }

    public Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        if (!_isEnabled)
            return Task.CompletedTask;

        var shouldInject = _random.NextDouble() < _injectionProbability;
        if (!shouldInject)
            return Task.CompletedTask;

        _logger.LogWarning(
            "[CHAOS] Injecting exception: {ExceptionType} (policy: {PolicyName}, probability: {Probability:P0})",
            _exceptionType.Name, Name, _injectionProbability);

        // Create and throw the exception
        var exception = (Exception)Activator.CreateInstance(_exceptionType, _exceptionMessage)!;
        throw exception;
    }

    public void Enable()
    {
        _isEnabled = true;
        _logger.LogWarning("[CHAOS] Exception injection policy '{PolicyName}' ENABLED", Name);
    }

    public void Disable()
    {
        _isEnabled = false;
        _logger.LogInformation("[CHAOS] Exception injection policy '{PolicyName}' disabled", Name);
    }

    public void SetInjectionProbability(double probability)
    {
        if (probability is < 0 or > 1)
            throw new ArgumentOutOfRangeException(nameof(probability), "Must be between 0.0 and 1.0");

        _injectionProbability = probability;
        _logger.LogInformation(
            "[CHAOS] Exception injection probability updated: {OldProbability:P0} → {NewProbability:P0}",
            _injectionProbability, probability);
    }
}
