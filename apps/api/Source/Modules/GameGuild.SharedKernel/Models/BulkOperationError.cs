namespace GameGuild.Models;

/// <summary>
///     Error details for failed bulk operations
/// </summary>
public abstract class BulkOperationError
{
    public Guid TenantId { get; init; }

    public string TenantName { get; init; } = string.Empty;

    public string ErrorMessage { get; init; } = string.Empty;

    public string ErrorCode { get; init; } = string.Empty;
}
