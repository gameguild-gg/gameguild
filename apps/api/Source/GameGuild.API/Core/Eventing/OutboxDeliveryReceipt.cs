namespace GameGuild.API.Eventing;

/// <summary>Permanent event identity; completion evidence survives retirement of the queue payload.</summary>
public sealed class OutboxDeliveryReceipt
{
    public Guid EventId { get; set; }
    public DateTimeOffset? CompletedAtUtc { get; set; }

    // Operational HTTP measurements need only the identity above. Business envelopes
    // retain their delivery metadata for existing administrative read models, never their payload.
    public string? EventName { get; set; }
    public string? EventType { get; set; }
    public string? SourceModule { get; set; }
    public string? AggregateType { get; set; }
    public string? AggregateId { get; set; }
    public Guid? TenantId { get; set; }
    public Guid? CorrelationId { get; set; }
    public DateTime? OccurredAtUtc { get; set; }
    public int? SchemaVersion { get; set; }

    internal static OutboxDeliveryReceipt FromCompleted(OutboxMessage message)
    {
        if (message.CompletedAtUtc is not { } completed || message.DeadLetteredAtUtc is not null)
        {
            throw new InvalidOperationException("Only successful delivery can be retired from the outbox.");
        }

        var receipt = new OutboxDeliveryReceipt { EventId = message.EventId, CompletedAtUtc = completed };
        if (message.SourceModule != "Platform.Http")
        {
            receipt.EventName = message.EventName;
            receipt.EventType = message.EventType;
            receipt.SourceModule = message.SourceModule;
            receipt.AggregateType = message.AggregateType;
            receipt.AggregateId = message.AggregateId;
            receipt.TenantId = message.TenantId;
            receipt.CorrelationId = message.CorrelationId;
            receipt.OccurredAtUtc = message.OccurredAtUtc;
            receipt.SchemaVersion = message.SchemaVersion;
        }

        return receipt;
    }
}
