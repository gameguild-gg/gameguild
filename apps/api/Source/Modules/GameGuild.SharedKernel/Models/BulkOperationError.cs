namespace GameGuild;

/// <summary>
///     Error details for failed bulk operations
/// </summary>
public abstract class BulkOperationError
{
    /// <summary>Identifier of the tenant the failed operation belongs to.</summary>
    public Guid TenantId { get; init; }

    /// <summary>Display name of the tenant for logging/reporting.</summary>
    public string TenantName { get; init; } = string.Empty;

    /// <summary>Human-readable description of why the operation failed.</summary>
    public string ErrorMessage { get; init; } = string.Empty;

    /// <summary>Machine-readable error code for programmatic handling.</summary>
    public string ErrorCode { get; init; } = string.Empty;
}
