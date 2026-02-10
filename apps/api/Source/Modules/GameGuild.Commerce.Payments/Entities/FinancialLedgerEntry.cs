using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.Commerce.Payments;

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

    /// <summary>
    /// Debit account (legacy string format for backward compatibility)
    /// Prefer using DebitLedgerAccount for new code.
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string DebitAccount { get; set; } = string.Empty;

    /// <summary>
    /// Credit account (legacy string format for backward compatibility)
    /// Prefer using CreditLedgerAccount for new code.
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string CreditAccount { get; set; } = string.Empty;

    /// <summary>
    /// Strongly-typed debit account (preferred).
    /// Stored as int in database, maps to LedgerAccount enum.
    /// </summary>
    public LedgerAccount? DebitLedgerAccount { get; set; }

    /// <summary>
    /// Strongly-typed credit account (preferred).
    /// Stored as int in database, maps to LedgerAccount enum.
    /// </summary>
    public LedgerAccount? CreditLedgerAccount { get; set; }

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

    /// <summary>Reconcile the entry (IMMUTABLE - cannot be undone)</summary>
    /// <remarks>
    /// Once reconciled, ledger entries cannot be unreconciled to protect audit trail integrity.
    /// Any corrections must be made via new adjusting entries.
    /// </remarks>
    public void Reconcile(Guid reconciledBy, string? notes = null)
    {
        if (IsReconciled) throw new InvalidOperationException("Entry is already reconciled");

        IsReconciled = true;
        ReconciledAt = SystemClock.UtcNow;
        ReconciledBy = reconciledBy;
        if (notes != null) Notes = notes;
    }

    // NOTE: Unreconcile() has been removed to ensure audit trail immutability.
    // Reconciled entries cannot be changed. Create adjusting entries instead.
}
