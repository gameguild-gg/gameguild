namespace GameGuild;

/// <summary>
///     Response for bulk operations
/// </summary>
public class BulkOperationResponse
{
    public int TotalRequested { get; init; }

    public int SuccessfulOperations { get; init; }

    public int FailedOperations { get; init; }

    public IEnumerable<BulkOperationError> Errors { get; init; } = [];

    public bool IsComplete { get => FailedOperations == 0; }

    public double SuccessRate { get => TotalRequested > 0 ? (double) SuccessfulOperations / TotalRequested : 0; }
}

/// <summary>
///     Error information for bulk operations
/// </summary>
public abstract record BulkOperationError(Guid Id, string ErrorMessage, string? Details = null);
