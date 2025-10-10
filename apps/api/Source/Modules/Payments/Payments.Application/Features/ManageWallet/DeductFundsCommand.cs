using GameGuild.Modules.Payments.Domain.Entities;
using GameGuild.Modules.Payments.Payments.Domain.Entities;
using GameGuild.CQRS;

namespace GameGuild.Modules.Payments.Payments.Application.Features.ManageWallet;

/// <summary>Command to deduct funds from a wallet</summary>
public record DeductFundsCommand(
    Guid UserId,
    decimal Amount,
    string Description,
    string? ReferenceId = null
) : IRequest<WalletTransaction>;
