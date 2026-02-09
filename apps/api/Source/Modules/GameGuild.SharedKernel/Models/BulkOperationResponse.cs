namespace GameGuild;

/// <summary>
///     Response for bulk operations
/// </summary>
public class BulkOperationResponse
{
    /// <summary>Total number of items submitted for processing.</summary>
    public int TotalRequested { get; init; }

    /// <summary>Number of items that completed successfully.</summary>
    public int SuccessfulOperations { get; init; }

    /// <summary>Number of items that failed.</summary>
    public int FailedOperations { get; init; }

    /// <summary>Per-item error details for failed operations.</summary>
    public IEnumerable<BulkOperationError> Errors { get; init; } = [];

    /// <summary>Whether every item in the batch succeeded.</summary>
    public bool IsComplete { get => FailedOperations == 0; }

    /// <summary>Ratio of successful operations to total requested (0.0–1.0).</summary>
    public double SuccessRate { get => TotalRequested > 0 ? (double) SuccessfulOperations / TotalRequested : 0; }
}
