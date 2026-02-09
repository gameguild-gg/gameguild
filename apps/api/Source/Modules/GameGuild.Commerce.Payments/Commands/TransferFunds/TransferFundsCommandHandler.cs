using GameGuild.CQRS;

namespace GameGuild.Commerce.Payments;

/// <summary>
///     Handler for TransferFundsCommand
/// </summary>
public sealed class TransferFundsCommandHandler(IWalletService walletService) : ICommandHandler<TransferFundsCommand, TransferResult>
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
