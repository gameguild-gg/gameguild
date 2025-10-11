using System.Diagnostics;


namespace GameGuild.Modules.Common.Deployment;

/// <summary>
/// Blue-green deployment strategy with instant cutover.
/// </summary>
public sealed class BlueGreenStrategy : IDeploymentStrategy
{
    private readonly ILogger<BlueGreenStrategy> _logger;
    private readonly IHealthCheckService _healthCheckService;
    private DeploymentState _state = DeploymentState.Idle;
    private string _currentVersion;
    private string? _newVersion;
    private DateTime? _startedAt;
    private bool _isCutoverComplete;

    public DeploymentMode Mode => DeploymentMode.BlueGreen;

    public int TrafficPercentage => _isCutoverComplete ? 100 : 0;

    public BlueGreenStrategy(
        ILogger<BlueGreenStrategy> logger,
        IHealthCheckService healthCheckService,
        string currentVersion)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _healthCheckService = healthCheckService ?? throw new ArgumentNullException(nameof(healthCheckService));
        _currentVersion = currentVersion ?? throw new ArgumentNullException(nameof(currentVersion));
    }

    /// <summary>
    /// Checks health of the new (green) version before cutover.
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
                    ["deployment_mode"] = "blue_green",
                    ["cutover_complete"] = _isCutoverComplete
                }
            };

            _logger.LogInformation(
                "Blue-green health check for version {Version}: {Status} ({Duration}ms)",
                targetVersion, isHealthy ? "Healthy" : "Unhealthy", stopwatch.ElapsedMilliseconds);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Blue-green health check failed");
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
    /// Not applicable for blue-green (instant cutover only).
    /// </summary>
    public Task<bool> ProgressTrafficAsync(int incrementPercentage, CancellationToken cancellationToken = default)
    {
        _logger.LogWarning("ProgressTrafficAsync not supported in blue-green mode (use CompleteCutoverAsync)");
        return Task.FromResult(false);
    }

    /// <summary>
    /// Performs instant cutover from blue to green version.
    /// </summary>
    public async Task<bool> CompleteCutoverAsync(CancellationToken cancellationToken = default)
    {
        if (_newVersion == null)
        {
            _logger.LogError("No new version specified for cutover");
            return false;
        }

        _state = DeploymentState.Progressing;
        _startedAt ??= DateTime.UtcNow;

        try
        {
            // Perform final health check before cutover
            var healthCheck = await CheckHealthAsync(cancellationToken);
            if (!healthCheck.IsHealthy)
            {
                _logger.LogError("Health check failed, aborting cutover");
                _state = DeploymentState.Failed;
                return false;
            }

            // Instant cutover
            _logger.LogInformation(
                "Performing blue-green cutover from {OldVersion} to {NewVersion}",
                _currentVersion, _newVersion);

            _currentVersion = _newVersion;
            _isCutoverComplete = true;
            _state = DeploymentState.Completed;

            _logger.LogInformation("Blue-green cutover completed successfully");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Blue-green cutover failed");
            _state = DeploymentState.Failed;
            return false;
        }
    }

    /// <summary>
    /// Rolls back to the previous (blue) version.
    /// </summary>
    public async Task<bool> RollbackAsync(CancellationToken cancellationToken = default)
    {
        if (_newVersion == null || !_isCutoverComplete)
        {
            _logger.LogWarning("No cutover to rollback");
            return false;
        }

        _state = DeploymentState.RollingBack;

        try
        {
            _logger.LogWarning("Rolling back from {NewVersion} to {OldVersion}", _currentVersion, _newVersion);

            // Instant rollback (just reverse the versions)
            var temp = _currentVersion;
            _currentVersion = _newVersion;
            _newVersion = temp;
            _isCutoverComplete = false;
            _state = DeploymentState.RolledBack;

            _logger.LogInformation("Rollback completed successfully");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Rollback failed");
            _state = DeploymentState.Failed;
            return false;
        }
    }

    /// <summary>
    /// Gets the current deployment status.
    /// </summary>
    public DeploymentStatus GetStatus()
    {
        return new DeploymentStatus
        {
            Mode = DeploymentMode.BlueGreen,
            CurrentVersion = _currentVersion,
            NewVersion = _newVersion,
            TrafficPercentage = TrafficPercentage,
            State = _state,
            StartedAt = _startedAt,
            CompletedAt = _state == DeploymentState.Completed ? DateTime.UtcNow : null,
            IsHealthy = _state == DeploymentState.Completed || _state == DeploymentState.Idle,
            StatusMessage = _state switch
            {
                DeploymentState.Idle => "No deployment in progress",
                DeploymentState.Initiated => "Deployment initiated, waiting for health checks",
                DeploymentState.HealthChecking => "Running health checks on new version",
                DeploymentState.Progressing => "Performing cutover",
                DeploymentState.Completed => "Cutover completed successfully",
                DeploymentState.RollingBack => "Rolling back to previous version",
                DeploymentState.RolledBack => "Rollback completed",
                DeploymentState.Failed => "Deployment failed",
                _ => "Unknown state"
            }
        };
    }

    /// <summary>
    /// Initiates a new blue-green deployment with the specified version.
    /// </summary>
    public void InitiateDeployment(string newVersion)
    {
        if (string.IsNullOrWhiteSpace(newVersion))
            throw new ArgumentException("New version cannot be null or empty", nameof(newVersion));

        _newVersion = newVersion;
        _state = DeploymentState.Initiated;
        _startedAt = DateTime.UtcNow;
        _isCutoverComplete = false;

        _logger.LogInformation(
            "Blue-green deployment initiated: {OldVersion} → {NewVersion}",
            _currentVersion, _newVersion);
    }
}
