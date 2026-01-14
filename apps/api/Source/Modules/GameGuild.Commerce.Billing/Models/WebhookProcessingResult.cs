namespace GameGuild.Commerce.Billing;

/// <summary>
///     Result of webhook processing
/// </summary>
public class WebhookProcessingResult
{
    /// <summary>Whether the webhook was processed successfully</summary>
    public bool Processed { get; set; }

    /// <summary>External event ID (used for idempotency)</summary>
    public string? EventId { get; set; }

    /// <summary>Error message if processing failed</summary>
    public string? ErrorMessage { get; set; }

    /// <summary>Whether the webhook should be retried</summary>
    public bool RequiresRetry { get; set; }

    /// <summary>Whether this was a duplicate event (already processed)</summary>
    public bool WasAlreadyProcessed { get; set; }

    /// <summary>When the event was originally processed (for duplicates)</summary>
    public DateTime? ProcessedAt { get; set; }

    /// <summary>Creates a success result</summary>
    public static WebhookProcessingResult Success(string eventId) => new()
    {
        Processed = true,
        EventId = eventId,
        ProcessedAt = DateTime.UtcNow
    };

    /// <summary>Creates a result for an already-processed event (idempotent response)</summary>
    public static WebhookProcessingResult AlreadyProcessed(string eventId, DateTime? originalProcessedAt = null) => new()
    {
        Processed = true,
        WasAlreadyProcessed = true,
        EventId = eventId,
        ProcessedAt = originalProcessedAt ?? DateTime.UtcNow
    };

    /// <summary>Creates a failure result</summary>
    public static WebhookProcessingResult Failed(string eventId, string errorMessage, bool requiresRetry = true) => new()
    {
        Processed = false,
        EventId = eventId,
        ErrorMessage = errorMessage,
        RequiresRetry = requiresRetry
    };
}
