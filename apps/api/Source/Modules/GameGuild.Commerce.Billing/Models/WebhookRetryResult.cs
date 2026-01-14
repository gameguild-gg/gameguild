namespace GameGuild.Commerce.Billing;

/// <summary>
///     Webhook retry result
/// </summary>
public class WebhookRetryResult
{
    public bool Success { get; set; }

    public string? ErrorMessage { get; set; }

    public string? Message { get; set; }

    public int AttemptNumber { get; set; }
}
