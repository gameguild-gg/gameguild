using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using GameGuild;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.Modules.Payments.Payments.Domain.Entities;

/// <summary>Entity representing a financial ledger entry</summary>
[Table("financial_ledger_entries")]
[Index(nameof(EntryType))]
[Index(nameof(DebitAccount))]
[Index(nameof(CreditAccount))]
[Index(nameof(RevenueEventId))]
[Index(nameof(IsReconciled))]
[Index(nameof(CreatedAt))]
public class FinancialLedgerEntry : EntityBase
{
    /// <summary>Entry type</summary>
    public LedgerEntryType EntryType { get; set; }

    /// <summary>Debit account</summary>
    [Required]
    [MaxLength(100)]
    public string DebitAccount { get; set; } = string.Empty;

    /// <summary>Credit account</summary>
    [Required]
    [MaxLength(100)]
    public string CreditAccount { get; set; } = string.Empty;

    /// <summary>Transaction amount</summary>
    [Column(TypeName = "decimal(18,2)")]
    public decimal Amount { get; set; }

    /// <summary>Currency code</summary>
    [Required]
    [MaxLength(3)]
    public string Currency { get; set; } = "USD";

    /// <summary>Entry description</summary>
    [Required]
    [MaxLength(500)]
    public string Description { get; set; } = string.Empty;

    /// <summary>Foreign key to revenue event</summary>
    public Guid? RevenueEventId { get; set; }

    /// <summary>Navigation property to revenue event</summary>
    [ForeignKey(nameof(RevenueEventId))]
    public virtual RevenueEvent? RevenueEvent { get; set; }

    /// <summary>Reference number for this entry</summary>
    [MaxLength(100)]
    public string? ReferenceNumber { get; set; }

    /// <summary>Whether this entry is reconciled</summary>
    public bool IsReconciled { get; set; } = false;

    /// <summary>Reconciled timestamp</summary>
    public DateTime? ReconciledAt { get; set; }

    /// <summary>Reconciled by user ID</summary>
    public Guid? ReconciledBy { get; set; }

    /// <summary>Additional notes</summary>
    [MaxLength(2000)]
    public string? Notes { get; set; }

    /// <summary>Fiscal year</summary>
    public int? FiscalYear { get; set; }

    /// <summary>Fiscal period (month)</summary>
    public int? FiscalPeriod { get; set; }

    /// <summary>Mark entry as reconciled</summary>
    public void Reconcile(Guid reconciledBy, string? notes = null)
    {
        IsReconciled = true;
        ReconciledAt = DateTime.UtcNow;
        ReconciledBy = reconciledBy;
        if (notes != null)
            Notes = notes;
    }

    /// <summary>Unreconcile entry</summary>
    public void Unreconcile()
    {
        IsReconciled = false;
        ReconciledAt = null;
        ReconciledBy = null;
    }
}

/// <summary>Ledger entry types</summary>
public enum LedgerEntryType
{
    /// <summary>Revenue entry</summary>
    Revenue = 0,

    /// <summary>Expense entry</summary>
    Expense = 1,

    /// <summary>Refund entry</summary>
    Refund = 2,

    /// <summary>Fee entry</summary>
    Fee = 3,

    /// <summary>Transfer entry</summary>
    Transfer = 4,

    /// <summary>Adjustment entry</summary>
    Adjustment = 5,

    /// <summary>Credit entry</summary>
    Credit = 6,

    /// <summary>Debit entry</summary>
    Debit = 7
}
