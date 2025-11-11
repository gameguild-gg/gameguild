namespace GameGuild.Payments.Commands;

public abstract record CreateWalletRequest
{
    public required Guid UserId { get; init; }

    public string? Currency { get; init; }
}
