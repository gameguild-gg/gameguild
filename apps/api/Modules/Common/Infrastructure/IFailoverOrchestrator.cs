namespace GameGuild.Modules.Common.Infrastructure;

/// <summary>
/// Orchestrator for multi-region failover operations.
/// </summary>
public interface IFailoverOrchestrator
{
    /// <summary>
    /// Gets the current active region.
    /// </summary>
    Task<Region> GetActiveRegionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all configured regions.
    /// </summary>
    Task<List<Region>> GetAllRegionsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks health of all regions.
    /// </summary>
    Task<List<RegionHealth>> CheckRegionsHealthAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes failover to a specific region.
    /// </summary>
    Task<FailoverResult> ExecuteFailoverAsync(string targetRegionId, FailoverOptions options, CancellationToken cancellationToken = default);

    /// <summary>
    /// Automatically fails over to the best available region.
    /// </summary>
    Task<FailoverResult> AutoFailoverAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes a failover runbook.
    /// </summary>
    Task<RunbookExecution> ExecuteRunbookAsync(string runbookId, Dictionary<string, object> parameters, CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents a deployment region.
/// </summary>
public sealed class Region
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string Location { get; init; }
    public string Status { get; set; }
    public bool IsPrimary { get; init; }
    public int Priority { get; init; }
    public RegionCapabilities Capabilities { get; init; } = new();
}

/// <summary>
/// Region operational status.
/// </summary>
public enum RegionStatus
{
    Active,
    Standby,
    Degraded,
    Unavailable
}

/// <summary>
/// Region capabilities and resources.
/// </summary>
public sealed class RegionCapabilities
{
    public bool SupportsReadTraffic { get; init; } = true;
    public bool SupportsWriteTraffic { get; init; } = true;
    public int MaxCapacity { get; init; }
    public List<string> AvailableServices { get; init; } = new();
}

/// <summary>
/// Health status of a region.
/// </summary>
public sealed class RegionHealth
{
    public required Region Region { get; init; }
    public required HealthStatus OverallHealth { get; init; }
    public double HealthScore { get; init; }
    public List<ServiceHealth> Services { get; init; } = new();
    public DateTime CheckedAt { get; init; } = DateTime.UtcNow;
    public TimeSpan ResponseTime { get; init; }
}

/// <summary>
/// Health status levels.
/// </summary>
public enum HealthStatus
{
    Healthy,
    Degraded,
    Unhealthy,
    Unknown
}

/// <summary>
/// Health of an individual service.
/// </summary>
public sealed class ServiceHealth
{
    public required string ServiceName { get; init; }
    public required HealthStatus Status { get; init; }
    public string? Message { get; init; }
}

/// <summary>
/// Result of a failover operation.
/// </summary>
public sealed class FailoverResult
{
    public required Region FromRegion { get; init; }
    public required Region ToRegion { get; init; }
    public required FailoverStatus Status { get; init; }
    public DateTime StartedAt { get; init; }
    public DateTime? CompletedAt { get; set; }
    public TimeSpan Duration => CompletedAt.HasValue ? CompletedAt.Value - StartedAt : TimeSpan.Zero;
    public List<FailoverStep> Steps { get; init; } = new();
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Failover operation status.
/// </summary>
public enum FailoverStatus
{
    Initiated,
    InProgress,
    Completed,
    Failed,
    RolledBack
}

/// <summary>
/// Individual step in a failover operation.
/// </summary>
public sealed class FailoverStep
{
    public required string Name { get; init; }
    public required string Description { get; init; }
    public required FailoverStepStatus Status { get; init; }
    public DateTime StartedAt { get; init; }
    public DateTime? CompletedAt { get; set; }
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Status of a failover step.
/// </summary>
public enum FailoverStepStatus
{
    Pending,
    Running,
    Completed,
    Failed,
    Skipped
}

/// <summary>
/// Options for failover operation.
/// </summary>
public sealed class FailoverOptions
{
    public bool RequireApproval { get; init; } = true;
    public bool DryRun { get; init; }
    public TimeSpan Timeout { get; init; } = TimeSpan.FromMinutes(30);
    public bool AutoRollbackOnFailure { get; init; } = true;
}

/// <summary>
/// Execution of a runbook.
/// </summary>
public sealed class RunbookExecution
{
    public required string RunbookId { get; init; }
    public required string RunbookName { get; init; }
    public DateTime StartedAt { get; init; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
    public required RunbookStatus Status { get; init; }
    public List<RunbookStep> Steps { get; init; } = new();
    public Dictionary<string, object> Parameters { get; init; } = new();
    public Dictionary<string, object> Output { get; init; } = new();
}

/// <summary>
/// Runbook execution status.
/// </summary>
public enum RunbookStatus
{
    Pending,
    Running,
    Completed,
    Failed,
    Cancelled
}

/// <summary>
/// Step in a runbook execution.
/// </summary>
public sealed class RunbookStep
{
    public required string Name { get; init; }
    public required string Action { get; init; }
    public required RunbookStepStatus Status { get; init; }
    public DateTime? StartedAt { get; init; }
    public DateTime? CompletedAt { get; set; }
    public string? Output { get; init; }
    public string? Error { get; init; }
}

/// <summary>
/// Status of a runbook step.
/// </summary>
public enum RunbookStepStatus
{
    Pending,
    Running,
    Completed,
    Failed,
    Skipped
}
