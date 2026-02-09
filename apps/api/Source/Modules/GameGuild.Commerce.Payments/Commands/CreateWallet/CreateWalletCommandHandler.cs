using GameGuild.CQRS;

namespace GameGuild.Commerce.Payments;

/// <summary>
///     Handler for CreateWalletCommand
/// </summary>
public sealed class CreateWalletCommandHandler(IWalletService walletService) : ICommandHandler<CreateWalletCommand, UserWallet>
{
    public async Task<UserWallet> Handle(CreateWalletCommand request, CancellationToken cancellationToken) { return await walletService.CreateWalletAsync(request.UserId, request.Currency, cancellationToken).ConfigureAwait(false); }
}
