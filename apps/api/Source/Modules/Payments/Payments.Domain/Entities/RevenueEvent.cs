using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using GameGuild;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.Modules.Payments.Payments.Domain.Entities;

/// <summary>Entity representing a revenue event for auditing</summary>
[Table("revenue_events")]
[Index(nameof(EventType))]
[Index(nameof(Source))]
[Index(nameof(ReferenceId))]
[Index(nameof(Timestamp))]
[Index(nameof(ProcessedAt))]
public class RevenueEvent : EntityBase
{
    /// <summary>Event type</summary>
    public RevenueEventType EventType { get; set; }

    /// <summary>Revenue amount</summary>
    [Column(TypeName = "decimal(18,2)")]
    public decimal Amount { get; set; }

    /// <summary>Currency code</summary>
    [Required]
    [MaxLength(3)]
    public string Currency { get; set; } = "USD";

    /// <summary>Revenue source</summary>
    public RevenueSource Source { get; set; }

    /// <summary>Reference ID (payment ID, subscription ID, etc.)</summary>
    [Required]
    [MaxLength(200)]
    public string ReferenceId { get; set; } = string.Empty;

    /// <summary>Event timestamp</summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>Event metadata (JSON)</summary>
    [MaxLength(5000)]
    public string? Metadata { get; set; }

    /// <summary>Foreign key to ledger entry</summary>
    public Guid? LedgerEntryId { get; set; }

    /// <summary>Navigation property to ledger entry</summary>
    [ForeignKey(nameof(LedgerEntryId))]
    public virtual FinancialLedgerEntry? LedgerEntry { get; set; }

    /// <summary>Processed timestamp</summary>
    public DateTime? ProcessedAt { get; set; }

    /// <summary>Processing status</summary>
    public RevenueEventStatus Status { get; set; } = RevenueEventStatus.Pending;

    /// <summary>Processing notes</summary>
    [MaxLength(1000)]
    public string? ProcessingNotes { get; set; }

    /// <summary>User ID associated with revenue</summary>
    public Guid? UserId { get; set; }

    /// <summary>Tenant ID for multi-tenancy</summary>
    // TenantId inherited from EntityBase (no override needed)

    /// <summary>Mark event as processed</summary>
    public void MarkAsProcessed(Guid? ledgerEntryId = null)
    {
        Status = RevenueEventStatus.Processed;
        ProcessedAt = DateTime.UtcNow;
        LedgerEntryId = ledgerEntryId;
    }

    /// <summary>Mark event as failed</summary>
    public void MarkAsFailed(string notes)
    {
        Status = RevenueEventStatus.Failed;
        ProcessedAt = DateTime.UtcNow;
        ProcessingNotes = notes;
    }
}

/// <summary>Revenue event types</summary>
public enum RevenueEventType
{
    /// <summary>Payment received</summary>
    PaymentReceived = 0,

    /// <summary>Refund issued</summary>
    RefundIssued = 1,

    /// <summary>Subscription started</summary>
    SubscriptionStarted = 2,

    /// <summary>Subscription renewed</summary>
    SubscriptionRenewed = 3,

    /// <summary>Subscription cancelled</summary>
    SubscriptionCancelled = 4,

    /// <summary>Chargeback received</summary>
    Chargeback = 5,

    /// <summary>Discount applied</summary>
    DiscountApplied = 6,

    /// <summary>Fee charged</summary>
    FeeCharged = 7,

    /// <summary>Credit issued</summary>
    CreditIssued = 8,

    /// <summary>Adjustment made</summary>
    Adjustment = 9
}

/// <summary>Revenue sources</summary>
public enum RevenueSource
{
    /// <summary>One-time payment</summary>
    Payment = 0,

    /// <summary>Subscription</summary>
    Subscription = 1,

    /// <summary>Wallet transaction</summary>
    Wallet = 2,

    /// <summary>Refund</summary>
    Refund = 3,

    /// <summary>Dispute/chargeback</summary>
    Dispute = 4,

    /// <summary>Fee</summary>
    Fee = 5,

    /// <summary>Credit</summary>
    Credit = 6,

    /// <summary>Other</summary>
    Other = 7
}

/// <summary>Revenue event status</summary>
public enum RevenueEventStatus
{
    /// <summary>Pending processing</summary>
    Pending = 0,

    /// <summary>Processed</summary>
    Processed = 1,

    /// <summary>Failed</summary>
    Failed = 2,

    /// <summary>Cancelled</summary>
    Cancelled = 3
}
