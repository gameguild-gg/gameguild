using GameGuild.CQRS;
using GameGuild.Payments.Abstractions;
using GameGuild.Payments.Entities;

namespace GameGuild.Payments.Commands;

/// <summary>
///     Handler for CreateWalletCommand
/// </summary>
public class CreateWalletCommandHandler(IWalletService walletService) : ICommandHandler<CreateWalletCommand, UserWallet>
{
    public async Task<UserWallet> Handle(CreateWalletCommand request, CancellationToken cancellationToken) { return await walletService.CreateWalletAsync(request.UserId, request.Currency, cancellationToken); }
}
