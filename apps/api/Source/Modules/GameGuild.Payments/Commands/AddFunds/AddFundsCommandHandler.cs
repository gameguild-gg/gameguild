using GameGuild.CQRS;
using GameGuild.Payments.Abstractions;
using GameGuild.Payments.Entities;

namespace GameGuild.Payments.Commands;

/// <summary>
///     Handler for AddFundsCommand
/// </summary>
public class AddFundsCommandHandler(IWalletService walletService) : ICommandHandler<AddFundsCommand, WalletTransaction>
{
    public async Task<WalletTransaction> Handle(AddFundsCommand request, CancellationToken cancellationToken)
    {
        return await walletService.AddFundsAsync(request.UserId, request.Amount, request.Description, request.ReferenceId, cancellationToken);
    }
}
