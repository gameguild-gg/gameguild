namespace GameGuild.Modules.Billing.Models;

/// <summary>
/// Result of webhook processing operation
/// </summary>
public class WebhookProcessingResult
{
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }
    public Dictionary<string, object>? ProcessedData { get; set; }

    public static WebhookProcessingResult Success(Dictionary<string, object>? data = null)
        => new() { IsSuccess = true, ProcessedData = data };

    public static WebhookProcessingResult Failure(string errorMessage)
        => new() { IsSuccess = false, ErrorMessage = errorMessage };
}
