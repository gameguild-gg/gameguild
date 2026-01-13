namespace GameGuild.Commerce.Payments;

public record TransferFundsResponse
{
    public required WalletTransaction DebitTransaction { get; init; }

    public required WalletTransaction CreditTransaction { get; init; }
}
