namespace GameGuild.Commerce.Payments;

public abstract record LockWalletRequest
{
    public required string Reason { get; init; }
}
