using System;
using System.Threading;
using System.Threading.Tasks;

namespace GameGuild.Modules.Common.Deployment;

/// <summary>
/// Deployment strategy interface for controlling traffic and health gating.
/// </summary>
public interface IDeploymentStrategy
{
    /// <summary>
    /// Gets the current deployment mode.
    /// </summary>
    DeploymentMode Mode { get; }

    /// <summary>
    /// Gets the percentage of traffic routed to the new version (0-100).
    /// </summary>
    int TrafficPercentage { get; }

    /// <summary>
    /// Checks if the deployment is healthy and can accept traffic.
    /// </summary>
    Task<HealthCheckResult> CheckHealthAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Increases traffic to the new version (canary progression).
    /// </summary>
    Task<bool> ProgressTrafficAsync(int incrementPercentage, CancellationToken cancellationToken = default);

    /// <summary>
    /// Performs a full cutover to the new version.
    /// </summary>
    Task<bool> CompleteCutoverAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Rolls back to the previous version.
    /// </summary>
    Task<bool> RollbackAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current deployment status.
    /// </summary>
    DeploymentStatus GetStatus();
}

/// <summary>
/// Deployment modes supported.
/// </summary>
public enum DeploymentMode
{
    /// <summary>
    /// Single active deployment (no canary/blue-green).
    /// </summary>
    Standard,

    /// <summary>
    /// Blue-green deployment with instant cutover.
    /// </summary>
    BlueGreen,

    /// <summary>
    /// Canary deployment with gradual traffic increase.
    /// </summary>
    Canary
}

/// <summary>
/// Health check result for deployment gating.
/// </summary>
public sealed class HealthCheckResult
{
    public bool IsHealthy { get; init; }
    public string Version { get; init; } = string.Empty;
    public DateTime CheckedAt { get; init; }
    public TimeSpan ResponseTime { get; init; }
    public string? ErrorMessage { get; init; }
    public Dictionary<string, object> Metrics { get; init; } = new();
}

/// <summary>
/// Current deployment status.
/// </summary>
public sealed class DeploymentStatus
{
    public DeploymentMode Mode { get; init; }
    public string CurrentVersion { get; init; } = string.Empty;
    public string? NewVersion { get; init; }
    public int TrafficPercentage { get; init; }
    public DeploymentState State { get; init; }
    public DateTime? StartedAt { get; init; }
    public DateTime? CompletedAt { get; init; }
    public bool IsHealthy { get; init; }
    public string? StatusMessage { get; init; }
}

/// <summary>
/// Deployment state machine states.
/// </summary>
public enum DeploymentState
{
    /// <summary>
    /// No deployment in progress.
    /// </summary>
    Idle,

    /// <summary>
    /// Deployment has been initiated.
    /// </summary>
    Initiated,

    /// <summary>
    /// Health checks are running.
    /// </summary>
    HealthChecking,

    /// <summary>
    /// Traffic is being shifted.
    /// </summary>
    Progressing,

    /// <summary>
    /// Deployment completed successfully.
    /// </summary>
    Completed,

    /// <summary>
    /// Deployment failed and is rolling back.
    /// </summary>
    RollingBack,

    /// <summary>
    /// Deployment was rolled back.
    /// </summary>
    RolledBack,

    /// <summary>
    /// Deployment failed.
    /// </summary>
    Failed
}
