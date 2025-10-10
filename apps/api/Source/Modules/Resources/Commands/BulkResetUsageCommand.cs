using GameGuild.CQRS;
using GameGuild.Messaging;

namespace GameGuild.Modules.Resources.Commands;

/// <summary>
/// Command to reset usage for multiple tenants in bulk
/// </summary>
/// <param name="TenantIds">List of tenant IDs to reset usage for</param>
/// <param name="UsageType">Optional usage type filter (null = reset all types)</param>
public record BulkResetUsageCommand(
    List<Guid> TenantIds,
    ResourceUsageType? UsageType = null) : IRequest<Result<BulkResetUsageResult>>;

/// <summary>
/// Result of bulk reset operation
/// </summary>
public class BulkResetUsageResult
{
    /// <summary>
    /// Number of tenants successfully reset
    /// </summary>
    public int SuccessCount { get; set; }

    /// <summary>
    /// Number of tenants that failed to reset
    /// </summary>
    public int FailureCount { get; set; }

    /// <summary>
    /// List of tenant IDs that failed with error messages
    /// </summary>
    public List<BulkResetFailure> Failures { get; set; } = new();

    /// <summary>
    /// Total number of usage records reset
    /// </summary>
    public int TotalRecordsReset { get; set; }
}

/// <summary>
/// Details of a failed reset operation
/// </summary>
public class BulkResetFailure
{
    /// <summary>
    /// Tenant ID that failed
    /// </summary>
    public Guid TenantId { get; set; }

    /// <summary>
    /// Error message
    /// </summary>
    public string ErrorMessage { get; set; } = string.Empty;
}
