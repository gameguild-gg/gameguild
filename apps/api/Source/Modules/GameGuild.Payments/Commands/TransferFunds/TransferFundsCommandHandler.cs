using GameGuild.CQRS;
using GameGuild.Payments.Abstractions;
using GameGuild.Payments.Entities;

namespace GameGuild.Payments.Commands;

/// <summary>
///     Handler for TransferFundsCommand
/// </summary>
public class TransferFundsCommandHandler(IWalletService walletService) : ICommandHandler<TransferFundsCommand, TransferResult>
{
    public async Task<TransferResult> Handle(TransferFundsCommand request, CancellationToken cancellationToken)
    {
        (var debitTransaction, var creditTransaction) = await walletService.TransferFundsAsync(
            request.FromUserId,
            request.ToUserId,
            request.Amount,
            request.Description,
            request.ReferenceId,
            cancellationToken
        );

        return new TransferResult(debitTransaction, creditTransaction);
    }
}
