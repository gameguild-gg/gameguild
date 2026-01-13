using GameGuild.CQRS;

namespace GameGuild.Commerce.Payments;

/// <summary>
///     Handler for DeductFundsCommand
/// </summary>
public class DeductFundsCommandHandler(IWalletService walletService) : ICommandHandler<DeductFundsCommand, WalletTransaction>
{
    public async Task<WalletTransaction> Handle(DeductFundsCommand request, CancellationToken cancellationToken)
    {
        return await walletService.DeductFundsAsync(request.UserId, request.Amount, request.Description, request.ReferenceId, cancellationToken);
    }
}
