namespace GameGuild.Commerce.Payments;

public sealed record LockWalletRequest
{
    public required string Reason { get; init; }
}
