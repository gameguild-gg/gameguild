namespace GameGuild.Billing.Exceptions;

/// <summary>
///     Exception thrown when webhook event type is not supported
/// </summary>
public class UnsupportedWebhookEventException : Exception
{
    public UnsupportedWebhookEventException() : this("Unknown") { }

    public UnsupportedWebhookEventException(string eventType) : base($"Unsupported webhook event type: {eventType}") { EventType = eventType; }

    public UnsupportedWebhookEventException(string eventType, Exception innerException) : base($"Unsupported webhook event type: {eventType}", innerException) { EventType = eventType; }

    public string EventType { get; }
}
