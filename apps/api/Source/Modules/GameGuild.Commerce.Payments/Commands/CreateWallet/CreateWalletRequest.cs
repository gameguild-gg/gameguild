namespace GameGuild.Commerce.Payments;

public sealed record CreateWalletRequest
{
    public string? Currency { get; init; }
}
