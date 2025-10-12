namespace GameGuild.Modules.Payments.Domain.Entities;

/// <summary>Entity representing a user's wallet</summary>
[Table("user_wallets")]
[Index(nameof(UserId), IsUnique = true)]
[Index(nameof(Currency))]
[Index(nameof(IsActive))]
public class UserWallet : EntityBase
{
    /// <summary>Default constructor</summary>
    public UserWallet() { }

    /// <summary>Constructor for partial initialization</summary>
    /// <param name="partial">Partial wallet data</param>
    public UserWallet(object partial) : base(partial) { }

    /// <summary>Foreign key to the User entity</summary>
    [Required]
    public Guid UserId { get; set; }

    /// <summary>Current balance</summary>
    [Column(TypeName = "decimal(18,2)")]
    public decimal Balance { get; set; } = 0;

    /// <summary>Currency code</summary>
    [Required]
    [MaxLength(3)]
    public string Currency { get; set; } = "USD";

    /// <summary>Whether this wallet is active</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>Whether this wallet is locked</summary>
    public bool IsLocked { get; set; } = false;

    /// <summary>Reason for lock (if locked)</summary>
    [MaxLength(500)]
    public string? LockReason { get; set; }

    /// <summary>Last transaction timestamp</summary>
    public DateTime? LastTransactionAt { get; set; }

    /// <summary>Navigation property to wallet transactions</summary>
    public virtual ICollection<WalletTransaction> Transactions { get; set; } = new List<WalletTransaction>();

    /// <summary>Add funds to the wallet</summary>
    public void AddFunds(decimal amount, string description, string? referenceId = null)
    {
        if (!IsActive) throw new InvalidOperationException("Wallet is not active");
        if (IsLocked) throw new InvalidOperationException($"Wallet is locked: {LockReason}");
        if (amount <= 0) throw new ArgumentException("Amount must be positive", nameof(amount));

        Balance += amount;
        LastTransactionAt = DateTime.UtcNow;

        var transaction = new WalletTransaction
        {
            WalletId = Id,
            Type = WalletTransactionType.Credit,
            Amount = amount,
            BalanceAfter = Balance,
            Description = description,
            ReferenceId = referenceId,
            Status = TransactionStatus.Completed
        };

        Transactions.Add(transaction);
    }

    /// <summary>Deduct funds from the wallet</summary>
    public void DeductFunds(decimal amount, string description, string? referenceId = null)
    {
        if (!IsActive) throw new InvalidOperationException("Wallet is not active");
        if (IsLocked) throw new InvalidOperationException($"Wallet is locked: {LockReason}");
        if (amount <= 0) throw new ArgumentException("Amount must be positive", nameof(amount));
        if (Balance < amount) throw new InvalidOperationException("Insufficient balance");

        Balance -= amount;
        LastTransactionAt = DateTime.UtcNow;

        var transaction = new WalletTransaction
        {
            WalletId = Id,
            Type = WalletTransactionType.Debit,
            Amount = amount,
            BalanceAfter = Balance,
            Description = description,
            ReferenceId = referenceId,
            Status = TransactionStatus.Completed
        };

        Transactions.Add(transaction);
    }

    /// <summary>Lock the wallet</summary>
    public void Lock(string reason)
    {
        IsLocked = true;
        LockReason = reason;
    }

    /// <summary>Unlock the wallet</summary>
    public void Unlock()
    {
        IsLocked = false;
        LockReason = null;
    }
}
