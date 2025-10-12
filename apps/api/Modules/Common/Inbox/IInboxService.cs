namespace GameGuild.Modules.Common.Inbox;

/// <summary>
/// Service for managing inbox pattern for message deduplication
/// </summary>
public interface IInboxService
{
    /// <summary>
    /// Checks if a message has already been processed
    /// </summary>
    Task<bool> HasBeenProcessedAsync(
        string messageId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks a message as processed (idempotent)
    /// </summary>
    Task MarkAsProcessedAsync(
        string messageId,
        string messageType,
        DateTime receivedAt,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes processed messages older than specified retention period
    /// </summary>
    Task DeleteProcessedMessagesAsync(
        TimeSpan retentionPeriod,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents an inbox message record
/// </summary>
public sealed class InboxMessage
{
    public required string MessageId { get; init; }
    public required string MessageType { get; init; }
    public required DateTime ReceivedAt { get; init; }
    public required DateTime ProcessedAt { get; init; }
}
