namespace GameGuild.Commerce.Payments;

/// <summary>
///     Result for transfer funds operation
/// </summary>
/// <param name="DebitTransaction">Transaction for funds deducted from source wallet</param>
/// <param name="CreditTransaction">Transaction for funds added to destination wallet</param>
public record TransferResult(WalletTransaction DebitTransaction, WalletTransaction CreditTransaction);
