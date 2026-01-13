namespace GameGuild.Commerce.Payments;

public record CreateWalletRequest
{
    public required Guid UserId { get; init; }

    public string? Currency { get; init; }
}
