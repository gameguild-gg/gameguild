namespace GameGuild.Modules.DataArchival.Services;

/// <summary>
/// Service for managing data archival and tiered storage lifecycle policies.
/// </summary>
public interface IDataArchivalService
{
    /// <summary>
    /// Executes an archival policy for eligible data.
    /// </summary>
    Task<Guid> ExecuteArchivalPolicyAsync(Guid policyId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Moves data to cool storage tier.
    /// </summary>
    Task MoveToCoolStorageAsync(string entityType, Guid entityId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Moves data to archive storage tier.
    /// </summary>
    Task MoveToArchiveStorageAsync(string entityType, Guid entityId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Restores data from archive storage to hot storage.
    /// </summary>
    Task<bool> RestoreFromArchiveAsync(string entityType, Guid entityId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current storage tier for an entity.
    /// </summary>
    Task<StorageTier> GetStorageTierAsync(string entityType, Guid entityId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets archival job status.
    /// </summary>
    Task<ArchivalJobDto?> GetArchivalJobStatusAsync(Guid jobId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Calculates storage cost savings from archival policies.
    /// </summary>
    Task<ArchivalCostSavingsDto> CalculateCostSavingsAsync(Guid? tenantId = null, CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents storage tier levels.
/// </summary>
public enum StorageTier
{
    Hot = 0,
    Cool = 1,
    Archive = 2,
    Deleted = 3
}

/// <summary>
/// DTO for archival job information.
/// </summary>
public record ArchivalJobDto(
    Guid Id,
    Guid PolicyId,
    string PolicyName,
    string Status,
    DateTime? StartedAt,
    DateTime? CompletedAt,
    int ItemsProcessed,
    int ItemsMovedToCool,
    int ItemsMovedToArchive,
    int ItemsDeleted,
    int ItemsFailed,
    long BytesProcessed,
    string? ErrorMessage
);

/// <summary>
/// DTO for cost savings information.
/// </summary>
public record ArchivalCostSavingsDto(
    decimal TotalSavingsPerMonth,
    long HotStorageBytes,
    long CoolStorageBytes,
    long ArchiveStorageBytes,
    decimal HotStorageCost,
    decimal CoolStorageCost,
    decimal ArchiveStorageCost
);
