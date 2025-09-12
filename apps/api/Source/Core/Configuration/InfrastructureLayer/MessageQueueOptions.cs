namespace GameGuild;

/// <summary>
/// Configuration options for message queue services.
/// </summary>
public class MessageQueueOptions
{
    /// <summary>
    /// The message queue provider (RabbitMQ, Azure Service Bus, etc.).
    /// </summary>
    public string Provider { get; set; } = "RabbitMQ";

    /// <summary>
    /// Connection string for the message queue.
    /// </summary>
    public string? ConnectionString { get; set; }

    /// <summary>
    /// Default queue name.
    /// </summary>
    public string DefaultQueue { get; set; } = "default";

    /// <summary>
    /// Number of retry attempts for failed messages.
    /// </summary>
    public int RetryAttempts { get; set; } = 3;

    /// <summary>
    /// Validates the message queue options.
    /// </summary>
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Provider))
        {
            throw new ArgumentException("Message queue provider must be specified.");
        }

        if (string.IsNullOrWhiteSpace(ConnectionString))
        {
            throw new ArgumentException("Message queue connection string is required.");
        }

        if (RetryAttempts < 0)
        {
            throw new ArgumentException("Retry attempts must be non-negative.");
        }
    }
}
