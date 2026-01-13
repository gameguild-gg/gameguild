using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using GameGuild.Entities;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.Commerce.Payments;

/// <summary>Entity representing a revenue event</summary>
[Table("revenue_events")]
[Index(nameof(EventType))]
[Index(nameof(Source))]
[Index(nameof(Status))]
[Index(nameof(Timestamp))]
[Index(nameof(UserId))]
[Index(nameof(ReferenceId))]
public class RevenueEvent : EntityBase
{
    /// <summary>Default constructor</summary>
    public RevenueEvent() { }

    /// <summary>Constructor for partial initialization</summary>
    /// <param name="partial">Partial event data</param>
    public RevenueEvent(object partial) : base(partial) { }

    /// <summary>Revenue event type</summary>
    public RevenueEventType EventType { get; set; }

    /// <summary>Event amount</summary>
    [Column(TypeName = "decimal(18,2)")]
    public decimal Amount { get; set; }

    /// <summary>Currency code</summary>
    [Required]
    [MaxLength(3)]
    public string Currency { get; set; } = "USD";

    /// <summary>Revenue source</summary>
    public RevenueSource Source { get; set; }

    /// <summary>Reference ID</summary>
    [Required]
    [MaxLength(200)]
    public string ReferenceId { get; set; } = string.Empty;

    /// <summary>Event timestamp</summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>Metadata (JSON)</summary>
    [MaxLength(2000)]
    public string? Metadata { get; set; }

    /// <summary>Foreign key to ledger entry</summary>
    public Guid? LedgerEntryId { get; set; }

    /// <summary>Navigation property to ledger entry</summary>
    public virtual FinancialLedgerEntry? LedgerEntry { get; set; }

    /// <summary>Processed timestamp</summary>
    public DateTime? ProcessedAt { get; set; }

    /// <summary>Event status</summary>
    public RevenueEventStatus Status { get; set; } = RevenueEventStatus.Pending;

    /// <summary>Processing notes</summary>
    [MaxLength(1000)]
    public string? ProcessingNotes { get; set; }

    /// <summary>User ID</summary>
    public Guid? UserId { get; set; }

    /// <summary>Mark event as processed</summary>
    public void MarkAsProcessed(Guid? ledgerEntryId = null)
    {
        Status = RevenueEventStatus.Processed;
        ProcessedAt = DateTime.UtcNow;
        LedgerEntryId = ledgerEntryId;
    }

    /// <summary>Mark event as failed</summary>
    public void MarkAsFailed(string reason)
    {
        Status = RevenueEventStatus.Failed;
        ProcessedAt = DateTime.UtcNow;
        ProcessingNotes = reason;
    }
}
