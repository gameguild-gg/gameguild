using GameGuild.Payments.Entities;

namespace GameGuild.Payments.Commands;

public record TransferFundsResponse
{
    public required WalletTransaction DebitTransaction { get; init; }

    public required WalletTransaction CreditTransaction { get; init; }
}
