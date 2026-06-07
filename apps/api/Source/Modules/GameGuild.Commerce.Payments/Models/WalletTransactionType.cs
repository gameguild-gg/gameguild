namespace GameGuild.Commerce.Payments;

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
