namespace GameGuild.Payments.Commands;

public abstract record LockWalletRequest
{
    public required string Reason { get; init; }
}
