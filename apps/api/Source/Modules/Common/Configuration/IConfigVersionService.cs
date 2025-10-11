namespace GameGuild.Modules.Common.Configuration;

/// <summary>
/// Service for configuration versioning and drift detection.
/// </summary>
public interface IConfigVersionService
{
    /// <summary>
    /// Creates a snapshot of the current configuration.
    /// </summary>
    Task<ConfigSnapshot> CreateSnapshotAsync(string label = "", CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a configuration snapshot by ID.
    /// </summary>
    Task<ConfigSnapshot?> GetSnapshotAsync(Guid snapshotId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets configuration version history.
    /// </summary>
    Task<List<ConfigSnapshot>> GetHistoryAsync(int limit = 50, CancellationToken cancellationToken = default);

    /// <summary>
    /// Compares two configuration snapshots and returns differences.
    /// </summary>
    Task<ConfigDiff> CompareSnapshotsAsync(Guid fromSnapshotId, Guid toSnapshotId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Detects drift between current configuration and a snapshot.
    /// </summary>
    Task<ConfigDrift> DetectDriftAsync(Guid baselineSnapshotId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Rolls back configuration to a previous snapshot.
    /// </summary>
    Task RollbackToSnapshotAsync(Guid snapshotId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Configuration snapshot at a point in time.
/// </summary>
public sealed class ConfigSnapshot
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public required string Label { get; init; }
    public required string ConfigurationHash { get; init; }
    public required Dictionary<string, object> Configuration { get; init; }
    public string? CreatedBy { get; init; }
    public ConfigSnapshotMetadata Metadata { get; init; } = new();
}

/// <summary>
/// Metadata about a configuration snapshot.
/// </summary>
public sealed class ConfigSnapshotMetadata
{
    public string? Environment { get; init; }
    public string? Version { get; init; }
    public Dictionary<string, string> Tags { get; init; } = new();
}

/// <summary>
/// Difference between two configuration snapshots.
/// </summary>
public sealed class ConfigDiff
{
    public required ConfigSnapshot FromSnapshot { get; init; }
    public required ConfigSnapshot ToSnapshot { get; init; }
    public List<ConfigChange> Changes { get; init; } = new();
    public int TotalChanges => Changes.Count;
    public bool HasChanges => TotalChanges > 0;
}

/// <summary>
/// A single configuration change.
/// </summary>
public sealed class ConfigChange
{
    public required string Path { get; init; }
    public required ConfigChangeType ChangeType { get; init; }
    public object? OldValue { get; init; }
    public object? NewValue { get; init; }
}

/// <summary>
/// Type of configuration change.
/// </summary>
public enum ConfigChangeType
{
    Added,
    Modified,
    Removed
}

/// <summary>
/// Configuration drift detection result.
/// </summary>
public sealed class ConfigDrift
{
    public required ConfigSnapshot BaselineSnapshot { get; init; }
    public required ConfigSnapshot CurrentSnapshot { get; init; }
    public required ConfigDiff Diff { get; init; }
    public bool HasDrift => Diff.HasChanges;
    public DriftSeverity Severity { get; init; }
    public DateTime DetectedAt { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Drift severity levels.
/// </summary>
public enum DriftSeverity
{
    None = 0,
    Low = 1,
    Medium = 2,
    High = 3,
    Critical = 4
}
