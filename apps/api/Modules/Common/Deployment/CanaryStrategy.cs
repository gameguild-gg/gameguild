using System.Diagnostics;


namespace GameGuild.Modules.Common.Deployment;

/// <summary>
/// Canary deployment strategy with gradual traffic progression.
/// </summary>
public sealed class CanaryStrategy : IDeploymentStrategy
{
    private readonly ILogger<CanaryStrategy> _logger;
    private readonly IHealthCheckService _healthCheckService;
    private DeploymentState _state = DeploymentState.Idle;
    private string _currentVersion;
    private string? _newVersion;
    private DateTime? _startedAt;
    private int _trafficPercentage = 0;
    private readonly int _maxTrafficPercentage;
    private readonly int _defaultIncrementPercentage;

    public DeploymentMode Mode => DeploymentMode.Canary;

    public int TrafficPercentage => _trafficPercentage;

    public CanaryStrategy(
        ILogger<CanaryStrategy> logger,
        IHealthCheckService healthCheckService,
        string currentVersion,
        int maxTrafficPercentage = 100,
        int defaultIncrementPercentage = 10)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _healthCheckService = healthCheckService ?? throw new ArgumentNullException(nameof(healthCheckService));
        _currentVersion = currentVersion ?? throw new ArgumentNullException(nameof(currentVersion));
        _maxTrafficPercentage = maxTrafficPercentage;
        _defaultIncrementPercentage = defaultIncrementPercentage;

        if (maxTrafficPercentage is < 1 or > 100)
            throw new ArgumentOutOfRangeException(nameof(maxTrafficPercentage), "Must be between 1 and 100");

        if (defaultIncrementPercentage is < 1 or > 100)
            throw new ArgumentOutOfRangeException(nameof(defaultIncrementPercentage), "Must be between 1 and 100");
    }

    /// <summary>
    /// Checks health of the canary version at current traffic level.
    /// </summary>
    public async Task<HealthCheckResult> CheckHealthAsync(CancellationToken cancellationToken = default)
    {
        _state = DeploymentState.HealthChecking;
        var stopwatch = Stopwatch.StartNew();

        try
        {
            var targetVersion = _newVersion ?? _currentVersion;
            var isHealthy = await _healthCheckService.CheckVersionHealthAsync(targetVersion, cancellationToken);

            stopwatch.Stop();

            var result = new HealthCheckResult
            {
                IsHealthy = isHealthy,
                Version = targetVersion,
                CheckedAt = DateTime.UtcNow,
                ResponseTime = stopwatch.Elapsed,
                Metrics = new Dictionary<string, object>
                {
                    ["deployment_mode"] = "canary",
                    ["traffic_percentage"] = _trafficPercentage,
                    ["max_traffic"] = _maxTrafficPercentage
                }
            };

            _logger.LogInformation(
                "Canary health check for version {Version} at {Traffic}% traffic: {Status} ({Duration}ms)",
                targetVersion, _trafficPercentage, isHealthy ? "Healthy" : "Unhealthy", stopwatch.ElapsedMilliseconds);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Canary health check failed");
            return new HealthCheckResult
            {
                IsHealthy = false,
                Version = _newVersion ?? _currentVersion,
                CheckedAt = DateTime.UtcNow,
                ResponseTime = stopwatch.Elapsed,
                ErrorMessage = ex.Message
            };
        }
    }

    /// <summary>
    /// Increases traffic to the canary version by the specified percentage.
    /// </summary>
    public async Task<bool> ProgressTrafficAsync(int incrementPercentage, CancellationToken cancellationToken = default)
    {
        if (_newVersion == null)
        {
            _logger.LogError("No canary version specified");
            return false;
        }

        if (_trafficPercentage >= _maxTrafficPercentage)
        {
            _logger.LogWarning("Canary already at maximum traffic ({Traffic}%)", _maxTrafficPercentage);
            return false;
        }

        _state = DeploymentState.Progressing;
        _startedAt ??= DateTime.UtcNow;

        try
        {
            // Health check before increasing traffic
            var healthCheck = await CheckHealthAsync(cancellationToken);
            if (!healthCheck.IsHealthy)
            {
                _logger.LogError("Health check failed at {Traffic}%, aborting progression", _trafficPercentage);
                _state = DeploymentState.Failed;
                return false;
            }

            // Increase traffic
            var previousTraffic = _trafficPercentage;
            _trafficPercentage = Math.Min(_trafficPercentage + incrementPercentage, _maxTrafficPercentage);

            _logger.LogInformation(
                "Canary traffic progression: {OldTraffic}% → {NewTraffic}% (version: {Version})",
                previousTraffic, _trafficPercentage, _newVersion);

            // Check if deployment is complete
            if (_trafficPercentage >= _maxTrafficPercentage)
            {
                _state = DeploymentState.Completed;
                _logger.LogInformation("Canary deployment completed at {Traffic}%", _trafficPercentage);
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Canary traffic progression failed");
            _state = DeploymentState.Failed;
            return false;
        }
    }

    /// <summary>
    /// Completes the canary deployment by increasing traffic to 100%.
    /// </summary>
    public async Task<bool> CompleteCutoverAsync(CancellationToken cancellationToken = default)
    {
        if (_newVersion == null)
        {
            _logger.LogError("No canary version specified for cutover");
            return false;
        }

        _state = DeploymentState.Progressing;
        _startedAt ??= DateTime.UtcNow;

        try
        {
            // Final health check
            var healthCheck = await CheckHealthAsync(cancellationToken);
            if (!healthCheck.IsHealthy)
            {
                _logger.LogError("Health check failed, aborting cutover");
                _state = DeploymentState.Failed;
                return false;
            }

            // Complete cutover
            _logger.LogInformation(
                "Completing canary cutover from {OldVersion} to {NewVersion} (traffic: {OldTraffic}% → 100%)",
                _currentVersion, _newVersion, _trafficPercentage);

            _currentVersion = _newVersion;
            _trafficPercentage = 100;
            _state = DeploymentState.Completed;

            _logger.LogInformation("Canary cutover completed successfully");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Canary cutover failed");
            _state = DeploymentState.Failed;
            return false;
        }
    }

    /// <summary>
    /// Rolls back the canary deployment by setting traffic to 0%.
    /// </summary>
    public async Task<bool> RollbackAsync(CancellationToken cancellationToken = default)
    {
        if (_newVersion == null)
        {
            _logger.LogWarning("No canary to rollback");
            return false;
        }

        _state = DeploymentState.RollingBack;

        try
        {
            _logger.LogWarning(
                "Rolling back canary deployment from {Traffic}% to 0% (version: {Version})",
                _trafficPercentage, _newVersion);

            _trafficPercentage = 0;
            _state = DeploymentState.RolledBack;

            _logger.LogInformation("Canary rollback completed successfully");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Canary rollback failed");
            _state = DeploymentState.Failed;
            return false;
        }
    }

    /// <summary>
    /// Gets the current canary deployment status.
    /// </summary>
    public DeploymentStatus GetStatus()
    {
        return new DeploymentStatus
        {
            Mode = DeploymentMode.Canary,
            CurrentVersion = _currentVersion,
            NewVersion = _newVersion,
            TrafficPercentage = _trafficPercentage,
            State = _state,
            StartedAt = _startedAt,
            CompletedAt = _state == DeploymentState.Completed ? DateTime.UtcNow : null,
            IsHealthy = _state is DeploymentState.Completed or DeploymentState.Idle or DeploymentState.Progressing,
            StatusMessage = _state switch
            {
                DeploymentState.Idle => "No canary deployment in progress",
                DeploymentState.Initiated => $"Canary initiated at 0% traffic",
                DeploymentState.HealthChecking => $"Running health checks at {_trafficPercentage}% traffic",
                DeploymentState.Progressing => $"Canary progressing at {_trafficPercentage}% traffic",
                DeploymentState.Completed => $"Canary completed at 100% traffic",
                DeploymentState.RollingBack => "Rolling back canary to 0%",
                DeploymentState.RolledBack => "Canary rolled back",
                DeploymentState.Failed => "Canary deployment failed",
                _ => "Unknown state"
            }
        };
    }

    /// <summary>
    /// Initiates a new canary deployment with the specified version.
    /// </summary>
    public void InitiateDeployment(string newVersion)
    {
        if (string.IsNullOrWhiteSpace(newVersion))
            throw new ArgumentException("New version cannot be null or empty", nameof(newVersion));

        _newVersion = newVersion;
        _state = DeploymentState.Initiated;
        _startedAt = DateTime.UtcNow;
        _trafficPercentage = 0;

        _logger.LogInformation(
            "Canary deployment initiated: {OldVersion} → {NewVersion} (max traffic: {MaxTraffic}%, increment: {Increment}%)",
            _currentVersion, _newVersion, _maxTrafficPercentage, _defaultIncrementPercentage);
    }

    /// <summary>
    /// Automatically progresses the canary by the default increment percentage.
    /// </summary>
    public Task<bool> AutoProgressAsync(CancellationToken cancellationToken = default)
    {
        return ProgressTrafficAsync(_defaultIncrementPercentage, cancellationToken);
    }
}
