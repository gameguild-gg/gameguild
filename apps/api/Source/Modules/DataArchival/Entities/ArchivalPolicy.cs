using GameGuild.Core.Domain;

namespace GameGuild.Modules.DataArchival.Entities;

/// <summary>
/// Represents a data archival policy with tiered storage lifecycle rules.
/// </summary>
public class ArchivalPolicy : EntityBase
{
    /// <summary>
    /// Gets or sets the unique identifier for the archival policy.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the name of the archival policy.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the description of the archival policy.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether the policy is enabled.
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the tenant ID this policy belongs to (null for global policies).
    /// </summary>
    public Guid? TenantId { get; set; }

    /// <summary>
    /// Gets or sets the entity type this policy applies to.
    /// </summary>
    public string EntityType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the number of days before moving data to cool storage.
    /// </summary>
    public int CoolStorageAfterDays { get; set; }

    /// <summary>
    /// Gets or sets the number of days before moving data to archive storage.
    /// </summary>
    public int ArchiveStorageAfterDays { get; set; }

    /// <summary>
    /// Gets or sets the number of days before permanently deleting data.
    /// </summary>
    public int? DeleteAfterDays { get; set; }

    /// <summary>
    /// Gets or sets whether to compress data when archiving.
    /// </summary>
    public bool CompressOnArchive { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to encrypt data when archiving.
    /// </summary>
    public bool EncryptOnArchive { get; set; } = true;

    /// <summary>
    /// Gets or sets the priority of this policy (higher numbers = higher priority).
    /// </summary>
    public int Priority { get; set; } = 0;

    /// <summary>
    /// Gets or sets the last time this policy was executed.
    /// </summary>
    public DateTime? LastExecutedAt { get; set; }

    /// <summary>
    /// Gets or sets the next scheduled execution time.
    /// </summary>
    public DateTime? NextExecutionAt { get; set; }

    /// <summary>
    /// Determines if data should be moved to cool storage based on age.
    /// </summary>
    public bool ShouldMoveToCoolStorage(DateTime dataCreatedAt)
    {
        return IsEnabled && (DateTime.UtcNow - dataCreatedAt).TotalDays >= CoolStorageAfterDays;
    }

    /// <summary>
    /// Determines if data should be moved to archive storage based on age.
    /// </summary>
    public bool ShouldMoveToArchiveStorage(DateTime dataCreatedAt)
    {
        return IsEnabled && (DateTime.UtcNow - dataCreatedAt).TotalDays >= ArchiveStorageAfterDays;
    }

    /// <summary>
    /// Determines if data should be deleted based on age.
    /// </summary>
    public bool ShouldDelete(DateTime dataCreatedAt)
    {
        return IsEnabled && DeleteAfterDays.HasValue && (DateTime.UtcNow - dataCreatedAt).TotalDays >= DeleteAfterDays.Value;
    }
}
