namespace GameGuild.Modules.Permissions.Entities;

/// <summary>
/// Represents a migration plan for upgrading permission templates
/// </summary>
[Table("PermissionTemplateMigrations")]
[Index(nameof(Status), Name = "IX_PermissionTemplateMigrations_Status")]
[Index(nameof(ScheduledFor), Name = "IX_PermissionTemplateMigrations_ScheduledFor")]
[Index(nameof(CreatedAt), Name = "IX_PermissionTemplateMigrations_CreatedAt")]
public class PermissionTemplateMigration : EntityBase
{
    /// <summary>
    /// Template being migrated
    /// </summary>
    public Guid TemplateId { get; set; }

    /// <summary>
    /// Source version
    /// </summary>
    public int FromVersion { get; set; }

    /// <summary>
    /// Target version
    /// </summary>
    public int ToVersion { get; set; }

    /// <summary>
    /// Migration status
    /// </summary>
    public MigrationStatus Status { get; set; } = MigrationStatus.Planned;

    /// <summary>
    /// Migration strategy
    /// </summary>
    public MigrationStrategy Strategy { get; set; }

    /// <summary>
    /// When migration is scheduled to run
    /// </summary>
    public DateTime? ScheduledFor { get; set; }

    /// <summary>
    /// When migration started
    /// </summary>
    public DateTime? StartedAt { get; set; }

    /// <summary>
    /// When migration completed
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// User who initiated the migration
    /// </summary>
    public Guid InitiatedByUserId { get; set; }

    /// <summary>
    /// Tenants affected by this migration
    /// </summary>
    public Guid[]? AffectedTenantIds { get; set; }

    /// <summary>
    /// Users affected by this migration
    /// </summary>
    public Guid[]? AffectedUserIds { get; set; }

    /// <summary>
    /// Number of entities successfully migrated
    /// </summary>
    public int SuccessCount { get; set; }

    /// <summary>
    /// Number of entities that failed migration
    /// </summary>
    public int FailureCount { get; set; }

    /// <summary>
    /// Number of entities skipped
    /// </summary>
    public int SkippedCount { get; set; }

    /// <summary>
    /// Total entities to migrate
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// Error messages encountered
    /// </summary>
    [Column(TypeName = "jsonb")]
    public List<MigrationError>? Errors { get; set; }

    /// <summary>
    /// Detailed migration log
    /// </summary>
    [Column(TypeName = "jsonb")]
    public List<MigrationLogEntry>? Log { get; set; }

    /// <summary>
    /// Rollback plan (if migration fails)
    /// </summary>
    [Column(TypeName = "jsonb")]
    public Dictionary<string, object>? RollbackPlan { get; set; }

    /// <summary>
    /// Dry run results (if performed)
    /// </summary>
    [Column(TypeName = "jsonb")]
    public DryRunResult? DryRunResult { get; set; }

    /// <summary>
    /// Whether this is a dry run
    /// </summary>
    public bool IsDryRun { get; set; }

    /// <summary>
    /// Notes about the migration
    /// </summary>
    [MaxLength(1000)]
    public string? Notes { get; set; }

    /// <summary>
    /// Calculate progress percentage
    /// </summary>
    public double ProgressPercentage =>
        TotalCount > 0 ? ((SuccessCount + FailureCount + SkippedCount) / (double)TotalCount) * 100 : 0;

    /// <summary>
    /// Check if migration is complete
    /// </summary>
    public bool IsComplete =>
        Status == MigrationStatus.Completed || Status == MigrationStatus.Failed || Status == MigrationStatus.RolledBack;
}

/// <summary>
/// Migration status
/// </summary>
public enum MigrationStatus
{
    Planned = 0,
    Scheduled = 1,
    InProgress = 2,
    Completed = 3,
    Failed = 4,
    RolledBack = 5,
    Cancelled = 6
}

/// <summary>
/// Migration strategy
/// </summary>
public enum MigrationStrategy
{
    /// <summary>
    /// Migrate all at once
    /// </summary>
    BigBang = 0,

    /// <summary>
    /// Migrate in batches
    /// </summary>
    Batched = 1,

    /// <summary>
    /// Migrate one tenant at a time
    /// </summary>
    PerTenant = 2,

    /// <summary>
    /// Gradual rollout with percentage
    /// </summary>
    Canary = 3,

    /// <summary>
    /// Blue/Green deployment style
    /// </summary>
    BlueGreen = 4
}

/// <summary>
/// Migration error entry
/// </summary>
public class MigrationError
{
    public Guid EntityId { get; set; }
    public string EntityType { get; set; } = null!;
    public string ErrorMessage { get; set; } = null!;
    public string? StackTrace { get; set; }
    public DateTime Timestamp { get; set; }
}

/// <summary>
/// Migration log entry
/// </summary>
public class MigrationLogEntry
{
    public DateTime Timestamp { get; set; }
    public string Level { get; set; } = null!;
    public string Message { get; set; } = null!;
    public Dictionary<string, object>? Data { get; set; }
}

/// <summary>
/// Dry run result
/// </summary>
public class DryRunResult
{
    public int EstimatedSuccessCount { get; set; }
    public int EstimatedFailureCount { get; set; }
    public int EstimatedSkippedCount { get; set; }
    public List<string>? Warnings { get; set; }
    public List<string>? BlockingIssues { get; set; }
    public TimeSpan EstimatedDuration { get; set; }
    public bool IsRecommended { get; set; }
    public string? RecommendationReason { get; set; }
}
