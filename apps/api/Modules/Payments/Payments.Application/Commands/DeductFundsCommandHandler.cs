using GameGuild.CQRS;
using GameGuild.Modules.Payments.Domain.Entities;
using GameGuild.Modules.Payments.Payments.Application.Services;


namespace GameGuild.Modules.Payments.Commands;

/// <summary>
///     Handler for DeductFundsCommand
/// </summary>
public class DeductFundsCommandHandler : IRequestHandler<DeductFundsCommand, WalletTransaction>
{
    private readonly IWalletService _walletService;
    private readonly ILogger<DeductFundsCommandHandler> _logger;

    public DeductFundsCommandHandler(
        IWalletService walletService,
        ILogger<DeductFundsCommandHandler> logger)
    {
        _walletService = walletService;
        _logger = logger;
    }

    public async Task<WalletTransaction> Handle(DeductFundsCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Deducting funds from wallet for user {UserId}: {Amount}", 
            request.UserId, request.Amount);

        var transaction = await _walletService.DeductFundsAsync(
            request.UserId, 
            request.Amount, 
            request.Description, 
            request.ReferenceId, 
            cancellationToken);

        _logger.LogInformation("Funds deducted: Transaction {TransactionId}", transaction.Id);
        return transaction;
    }
}
