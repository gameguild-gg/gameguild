using GameGuild.Modules.Payments.Domain.Entities;
using GameGuild.CQRS;

namespace GameGuild.Modules.Payments.Payments.Application.Features.ManageWallet;

/// <summary>Result for transfer funds operation</summary>
public record TransferResult(
    WalletTransaction DebitTransaction,
    WalletTransaction CreditTransaction
);

/// <summary>Command to transfer funds between wallets</summary>
public record TransferFundsCommand(
    Guid FromUserId,
    Guid ToUserId,
    decimal Amount,
    string Description,
    string? ReferenceId = null
) : IRequest<TransferResult>;
