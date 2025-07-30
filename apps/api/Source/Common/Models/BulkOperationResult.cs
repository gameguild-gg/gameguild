namespace GameGuild.Common;

/// <summary>
/// Represents the result of a bulk operation
/// </summary>
public class BulkOperationResult {
  public BulkOperationResult(int totalRequested, int successful, int failed) {
    TotalRequested = totalRequested;
    Successful = successful;
    Failed = failed;
    Errors = new List<string>();
  }

  /// <summary>
  /// Total number of items requested to be processed
  /// </summary>
  public int TotalRequested { get; }

  /// <summary>
  /// Number of items successfully processed
  /// </summary>
  public int Successful { get; }

  /// <summary>
  /// Number of items that failed to process
  /// </summary>
  public int Failed { get; }

  /// <summary>
  /// List of error messages for failed items
  /// </summary>
  public IList<string> Errors { get; }

  /// <summary>
  /// Whether the operation was completely successful
  /// </summary>
  public bool IsCompletelySuccessful {
    get => Failed == 0;
  }

  /// <summary>
  /// Whether the operation was partially successful
  /// </summary>
  public bool IsPartiallySuccessful {
    get => Successful > 0 && Failed > 0;
  }

  /// <summary>
  /// Whether the operation completely failed
  /// </summary>
  public bool IsCompletelyFailed {
    get => Successful == 0 && Failed > 0;
  }

  /// <summary>
  /// Add an error message to the result
  /// </summary>
  public void AddError(string error) { Errors.Add(error); }
}
