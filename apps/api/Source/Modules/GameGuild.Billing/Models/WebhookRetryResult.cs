namespace GameGuild.Billing.Models;

/// <summary>
///     Webhook retry result
/// </summary>
public class WebhookRetryResult
{
    public bool Success { get; set; }

    public string? ErrorMessage { get; set; }

    public int AttemptNumber { get; set; }
}
