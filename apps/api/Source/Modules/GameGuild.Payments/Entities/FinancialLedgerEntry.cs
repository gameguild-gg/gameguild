using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.Payments.Entities;

/// <summary>Entity representing a financial ledger entry</summary>
[Table("financial_ledger_entries")]
[Index(nameof(EntryType))]
[Index(nameof(DebitAccount))]
[Index(nameof(CreditAccount))]
[Index(nameof(ReferenceNumber))]
[Index(nameof(IsReconciled))]
[Index(nameof(FiscalYear))]
[Index(nameof(FiscalPeriod))]
[Index(nameof(CreatedAt))]
public class FinancialLedgerEntry : EntityBase
{
    /// <summary>Default constructor</summary>
    public FinancialLedgerEntry() { }

    /// <summary>Constructor for partial initialization</summary>
    /// <param name="partial">Partial entry data</param>
    public FinancialLedgerEntry(object partial) : base(partial) { }

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

    /// <summary>Entry amount</summary>
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

    /// <summary>Navigation property to revenue event (inverse of RevenueEvent.LedgerEntry)</summary>
    public virtual RevenueEvent? RevenueEvent { get; set; }

    /// <summary>Reference number</summary>
    [MaxLength(100)]
    public string? ReferenceNumber { get; set; }

    /// <summary>Whether this entry is reconciled</summary>
    public bool IsReconciled { get; set; }

    /// <summary>Reconciled timestamp</summary>
    public DateTime? ReconciledAt { get; set; }

    /// <summary>Reconciled by user ID</summary>
    public Guid? ReconciledBy { get; set; }

    /// <summary>Entry notes</summary>
    [MaxLength(1000)]
    public string? Notes { get; set; }

    /// <summary>Fiscal year</summary>
    public int FiscalYear { get; set; }

    /// <summary>Fiscal period (month)</summary>
    public int FiscalPeriod { get; set; }

    /// <summary>Reconcile the entry</summary>
    public void Reconcile(Guid reconciledBy, string? notes = null)
    {
        if (IsReconciled) throw new InvalidOperationException("Entry is already reconciled");

        IsReconciled = true;
        ReconciledAt = DateTime.UtcNow;
        ReconciledBy = reconciledBy;
        if (notes != null) Notes = notes;
    }

    /// <summary>Unreconcile the entry</summary>
    public void Unreconcile()
    {
        if (!IsReconciled) throw new InvalidOperationException("Entry is not reconciled");

        IsReconciled = false;
        ReconciledAt = null;
        ReconciledBy = null;
    }
}
