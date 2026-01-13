namespace GameGuild.Commerce.Billing;

/// <summary>
///     Result of webhook processing
/// </summary>
public class WebhookProcessingResult
{
    public bool Processed { get; set; }

    public string? EventId { get; set; }

    public string? ErrorMessage { get; set; }

    public bool RequiresRetry { get; set; }
}
