using GameGuild.Modules.Payments.Domain.Entities;
using GameGuild.CQRS;

namespace GameGuild.Modules.Payments.Payments.Application.Features.ManageWallet;

/// <summary>Command to add funds to a wallet</summary>
public record AddFundsCommand(
    Guid UserId,
    decimal Amount,
    string Description,
    string? ReferenceId = null
) : IRequest<WalletTransaction>;
