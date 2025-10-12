namespace GameGuild.Modules.Users;

/// <summary>
///     Transaction ledger for user balance changes
///     Provides audit trail and balance reconciliation
/// </summary>
[Table("user_balance_transactions")]
[Index(nameof(UserId), nameof(CreatedAt))]
[Index(nameof(TransactionType))]
[Index(nameof(ReferenceId))]
public sealed class UserBalanceTransaction : EntityBase
{
    /// <summary>
    ///     User ID associated with this transaction
    /// </summary>
    [Required]
    public Guid UserId { get; set; }

    /// <summary>
    ///     Navigation property to the user
    /// </summary>
    public User? User { get; set; }

    /// <summary>
    ///     Transaction amount (positive for credit, negative for debit)
    /// </summary>
    [Column(TypeName = "decimal(18,8)")]
    public decimal Amount { get; set; }

    /// <summary>
    ///     Balance before this transaction
    /// </summary>
    [Column(TypeName = "decimal(18,8)")]
    public decimal BalanceBefore { get; set; }

    /// <summary>
    ///     Balance after this transaction
    /// </summary>
    [Column(TypeName = "decimal(18,8)")]
    public decimal BalanceAfter { get; set; }

    /// <summary>
    ///     Type of transaction
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string TransactionType { get; set; } = string.Empty;

    /// <summary>
    ///     Description of the transaction
    /// </summary>
    [MaxLength(500)]
    public string? Description { get; set; }

    /// <summary>
    ///     Reference ID to external transaction (e.g., payment ID, order ID)
    /// </summary>
    [MaxLength(100)]
    public string? ReferenceId { get; set; }

    /// <summary>
    ///     Additional metadata stored as JSON
    /// </summary>
    [Column(TypeName = "jsonb")]
    public string? Metadata { get; set; }

    /// <summary>
    ///     Create a new balance transaction
    /// </summary>
    public static UserBalanceTransaction Create(
        Guid userId,
        decimal amount,
        decimal balanceBefore,
        string transactionType,
        string? description = null,
        string? referenceId = null,
        string? metadata = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(transactionType);

        return new UserBalanceTransaction
        {
            UserId = userId,
            Amount = amount,
            BalanceBefore = balanceBefore,
            BalanceAfter = balanceBefore + amount,
            TransactionType = transactionType,
            Description = description,
            ReferenceId = referenceId,
            Metadata = metadata
        };
    }
}

/// <summary>
///     Common transaction types
/// </summary>
public static class TransactionTypes
{
    public const string Deposit = "DEPOSIT";
    public const string Withdrawal = "WITHDRAWAL";
    public const string Purchase = "PURCHASE";
    public const string Refund = "REFUND";
    public const string Reward = "REWARD";
    public const string Adjustment = "ADJUSTMENT";
    public const string Transfer = "TRANSFER";
    public const string Fee = "FEE";
}
