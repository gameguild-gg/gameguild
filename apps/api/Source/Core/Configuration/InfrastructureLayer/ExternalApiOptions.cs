namespace GameGuild;

/// <summary> Configuration options for external API integration. </summary>
public class ExternalApiOptions {
  /// <summary> HTTP client timeout in seconds. </summary>
  public int TimeoutSeconds { get; set; } = 30;

  /// <summary> Number of retry attempts for failed API calls. </summary>
  public int RetryAttempts { get; set; } = 3;

  /// <summary> Base delay between retries in milliseconds. </summary>
  public int RetryDelayMs { get; set; } = 1000;

  /// <summary> API endpoints configuration. </summary>
  public Dictionary<string, string> Endpoints { get; set; } = new Dictionary<string, string>();

  /// <summary> API keys for external services. </summary>
  public Dictionary<string, string> ApiKeys { get; set; } = new Dictionary<string, string>();

  /// <summary> Validates the external API options. </summary>
  public void Validate() {
    if (TimeoutSeconds <= 0) { throw new ArgumentException("Timeout seconds must be greater than 0."); }

    if (RetryAttempts < 0) { throw new ArgumentException("Retry attempts must be non-negative."); }

    if (RetryDelayMs <= 0) { throw new ArgumentException("Retry delay must be greater than 0."); }
  }
}
