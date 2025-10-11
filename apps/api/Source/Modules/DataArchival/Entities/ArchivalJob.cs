namespace GameGuild.Modules.DataArchival.Entities;

/// <summary>
/// Represents an archival job execution record.
/// </summary>
public class ArchivalJob : EntityBase {
    /// <summary>
    /// Gets or sets the archival policy ID.
    /// </summary>
    public Guid ArchivalPolicyId { get; set; }

    /// <summary>
    /// Gets or sets the archival policy.
    /// </summary>
    public ArchivalPolicy? ArchivalPolicy { get; set; }

    /// <summary>
    /// Gets or sets the job status.
    /// </summary>
    public ArchivalJobStatus Status { get; set; } = ArchivalJobStatus.Pending;

    /// <summary>
    /// Gets or sets the start time of the job.
    /// </summary>
    public DateTime? StartedAt { get; set; }

    /// <summary>
    /// Gets or sets the completion time of the job.
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// Gets or sets the number of items processed.
    /// </summary>
    public int ItemsProcessed { get; set; }

    /// <summary>
    /// Gets or sets the number of items moved to cool storage.
    /// </summary>
    public int ItemsMovedToCool { get; set; }

    /// <summary>
    /// Gets or sets the number of items moved to archive storage.
    /// </summary>
    public int ItemsMovedToArchive { get; set; }

    /// <summary>
    /// Gets or sets the number of items deleted.
    /// </summary>
    public int ItemsDeleted { get; set; }

    /// <summary>
    /// Gets or sets the number of items that failed to process.
    /// </summary>
    public int ItemsFailed { get; set; }

    /// <summary>
    /// Gets or sets the total bytes processed.
    /// </summary>
    public long BytesProcessed { get; set; }

    /// <summary>
    /// Gets or sets the error message if the job failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets additional job details in JSON format.
    /// </summary>
    public string? Details { get; set; }

    // Backward compatibility aliases
    public Guid PolicyId { get => ArchivalPolicyId; set => ArchivalPolicyId = value; }
    public int ItemsArchived { get => ItemsMovedToArchive; set => ItemsMovedToArchive = value; }

    /// <summary>
    /// Marks the job as started.
    /// </summary>
    public void Start() {
        Status = ArchivalJobStatus.Running;
        StartedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Marks the job as completed.
    /// </summary>
    public void Complete() {
        Status = ArchivalJobStatus.Completed;
        CompletedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Marks the job as failed.
    /// </summary>
    public void Fail(string errorMessage) {
        Status = ArchivalJobStatus.Failed;
        CompletedAt = DateTime.UtcNow;
        ErrorMessage = errorMessage;
    }
}

/// <summary>
/// Represents the status of an archival job.
/// </summary>
public enum ArchivalJobStatus {
    Pending = 0,
    Running = 1,
    Completed = 2,
    Failed = 3,
    Cancelled = 4
}
