using GameGuild.Modules.Payments.Domain.Entities;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using GameGuild;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.Modules.Payments.Domain.Entities;

/// <summary>Entity representing a wallet transaction</summary>
[Table("wallet_transactions")]
[Index(nameof(WalletId))]
[Index(nameof(Type))]
[Index(nameof(Status))]
[Index(nameof(CreatedAt))]
[Index(nameof(ReferenceId))]
public class WalletTransaction : EntityBase
{
    /// <summary>Default constructor</summary>
    public WalletTransaction() { }

    /// <summary>Constructor for partial initialization</summary>
    /// <param name="partial">Partial transaction data</param>
    public WalletTransaction(object partial) : base(partial) { }

    /// <summary>Foreign key to the UserWallet entity</summary>
    [Required]
    public Guid WalletId { get; set; }

    /// <summary>Navigation property to the UserWallet entity</summary>
    [ForeignKey(nameof(WalletId))]
    public virtual UserWallet Wallet { get; set; } = null!;

    /// <summary>Transaction type</summary>
    public WalletTransactionType Type { get; set; }

    /// <summary>Transaction amount</summary>
    [Column(TypeName = "decimal(18,2)")]
    public decimal Amount { get; set; }

    /// <summary>Balance after this transaction</summary>
    [Column(TypeName = "decimal(18,2)")]
    public decimal BalanceAfter { get; set; }

    /// <summary>Transaction description</summary>
    [Required]
    [MaxLength(500)]
    public string Description { get; set; } = string.Empty;

    /// <summary>Reference ID (e.g., order ID, payment ID)</summary>
    [MaxLength(200)]
    public string? ReferenceId { get; set; }

    /// <summary>Transaction status</summary>
    public TransactionStatus Status { get; set; } = TransactionStatus.Pending;

    /// <summary>Transaction metadata (JSON)</summary>
    [MaxLength(2000)]
    public string? Metadata { get; set; }

    /// <summary>Processing notes</summary>
    [MaxLength(1000)]
    public string? Notes { get; set; }

    /// <summary>Processed timestamp</summary>
    public DateTime? ProcessedAt { get; set; }

    /// <summary>Mark transaction as completed</summary>
    public void Complete()
    {
        Status = TransactionStatus.Completed;
        ProcessedAt = DateTime.UtcNow;
    }

    /// <summary>Mark transaction as failed</summary>
    public void Fail(string reason)
    {
        Status = TransactionStatus.Failed;
        Notes = reason;
        ProcessedAt = DateTime.UtcNow;
    }
}

/// <summary>Wallet transaction types</summary>
public enum WalletTransactionType
{
    /// <summary>Credit (add funds)</summary>
    Credit = 0,

    /// <summary>Debit (deduct funds)</summary>
    Debit = 1,

    /// <summary>Transfer in</summary>
    TransferIn = 2,

    /// <summary>Transfer out</summary>
    TransferOut = 3,

    /// <summary>Refund</summary>
    Refund = 4,

    /// <summary>Fee</summary>
    Fee = 5,

    /// <summary>Adjustment</summary>
    Adjustment = 6
}

/// <summary>Transaction status</summary>
public enum TransactionStatus
{
    /// <summary>Pending processing</summary>
    Pending = 0,

    /// <summary>Processing</summary>
    Processing = 1,

    /// <summary>Completed successfully</summary>
    Completed = 2,

    /// <summary>Failed</summary>
    Failed = 3,

    /// <summary>Cancelled</summary>
    Cancelled = 4,

    /// <summary>Reversed</summary>
    Reversed = 5
}
