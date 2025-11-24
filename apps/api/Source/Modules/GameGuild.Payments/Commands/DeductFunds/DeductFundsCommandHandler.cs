using GameGuild.CQRS;
using GameGuild.Payments.Abstractions;
using GameGuild.Payments.Entities;

namespace GameGuild.Payments.Commands;

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
