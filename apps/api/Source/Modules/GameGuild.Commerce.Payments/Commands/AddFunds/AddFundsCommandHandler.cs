using GameGuild.CQRS;

namespace GameGuild.Commerce.Payments;

/// <summary>
///     Handler for AddFundsCommand
/// </summary>
public sealed class AddFundsCommandHandler(IWalletService walletService) : ICommandHandler<AddFundsCommand, WalletTransaction>
{
    public async Task<WalletTransaction> Handle(AddFundsCommand request, CancellationToken cancellationToken)
    {
        return await walletService.AddFundsAsync(request.UserId, request.Amount, request.Description, request.ReferenceId, cancellationToken).ConfigureAwait(false);
    }
}
