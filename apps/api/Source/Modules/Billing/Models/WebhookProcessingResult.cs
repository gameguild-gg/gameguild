namespace GameGuild.Modules.Billing.Models;

/// <summary> Result of webhook processing operation </summary>
public class WebhookProcessingResult {
  public bool IsSuccess { get; set; }

  public string? ErrorMessage { get; set; }

  public Dictionary<string, object>? ProcessedData { get; set; }

  public static WebhookProcessingResult Success(Dictionary<string, object>? data = null) { return new WebhookProcessingResult { IsSuccess = true, ProcessedData = data }; }

  public static WebhookProcessingResult Failure(string errorMessage) { return new WebhookProcessingResult { IsSuccess = false, ErrorMessage = errorMessage }; }
}
