namespace GameGuild;

/// <summary> Configuration options for background services. </summary>
public class BackgroundServiceOptions {
  /// <summary> Whether background services are enabled. </summary>
  public bool Enabled { get; set; } = true;

  /// <summary> Maximum number of concurrent background tasks. </summary>
  public int MaxConcurrentTasks { get; set; } = Environment.ProcessorCount;

  /// <summary> Task timeout in seconds. </summary>
  public int TaskTimeoutSeconds { get; set; } = 300; // 5 minutes

  /// <summary> Validates the background service options. </summary>
  public void Validate() {
    if (MaxConcurrentTasks <= 0) throw new InvalidOperationException("Max concurrent tasks must be greater than zero.");

    if (TaskTimeoutSeconds <= 0) throw new InvalidOperationException("Task timeout seconds must be greater than zero.");
  }
}
