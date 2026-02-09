namespace GameGuild.Commerce.Payments;

public sealed record CreateWalletRequest
{
    public required Guid UserId { get; init; }

    public string? Currency { get; init; }
}
