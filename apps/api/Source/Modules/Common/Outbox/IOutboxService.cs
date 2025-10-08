namespace GameGuild.Modules.Common.Outbox;

/// <summary>
/// Service for managing outbox pattern for reliable message delivery
/// </summary>
public interface IOutboxService
{
    /// <summary>
    /// Adds a message to the outbox (transactional write)
    /// </summary>
    Task AddMessageAsync(
        string messageType,
        string payload,
        string? correlationId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets pending messages ready for processing
    /// </summary>
    Task<IReadOnlyList<OutboxMessage>> GetPendingMessagesAsync(
        int batchSize = 100,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks a message as processed
    /// </summary>
    Task MarkAsProcessedAsync(
        Guid messageId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks a message as failed with error details
    /// </summary>
    Task MarkAsFailedAsync(
        Guid messageId,
        string error,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes processed messages older than specified retention period
    /// </summary>
    Task DeleteProcessedMessagesAsync(
        TimeSpan retentionPeriod,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents an outbox message
/// </summary>
public sealed class OutboxMessage
{
    public required Guid Id { get; init; }
    public required string MessageType { get; init; }
    public required string Payload { get; init; }
    public string? CorrelationId { get; init; }
    public required OutboxMessageStatus Status { get; init; }
    public required DateTime CreatedAt { get; init; }
    public DateTime? ProcessedAt { get; init; }
    public int RetryCount { get; init; }
    public string? Error { get; init; }
}

/// <summary>
/// Status of an outbox message
/// </summary>
public enum OutboxMessageStatus
{
    Pending = 0,
    Processing = 1,
    Processed = 2,
    Failed = 3
}
