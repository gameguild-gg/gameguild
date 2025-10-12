using GameGuild.CQRS;
using GameGuild.Modules.Payments.Domain.Entities;
using GameGuild.Modules.Payments.Payments.Application.Services;


namespace GameGuild.Modules.Payments.Commands;

/// <summary>
///     Handler for AddFundsCommand
/// </summary>
public class AddFundsCommandHandler : IRequestHandler<AddFundsCommand, WalletTransaction>
{
    private readonly IWalletService _walletService;
    private readonly ILogger<AddFundsCommandHandler> _logger;

    public AddFundsCommandHandler(
        IWalletService walletService,
        ILogger<AddFundsCommandHandler> logger)
    {
        _walletService = walletService;
        _logger = logger;
    }

    public async Task<WalletTransaction> Handle(AddFundsCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Adding funds to wallet for user {UserId}: {Amount}", 
            request.UserId, request.Amount);

        var transaction = await _walletService.AddFundsAsync(
            request.UserId, 
            request.Amount, 
            request.Description, 
            request.ReferenceId, 
            cancellationToken);

        _logger.LogInformation("Funds added: Transaction {TransactionId}", transaction.Id);
        return transaction;
    }
}
