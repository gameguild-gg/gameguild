using GameGuild.CQRS;
using GameGuild.Modules.Payments.Domain.Entities;
using GameGuild.Modules.Payments.Payments.Application.Services;


namespace GameGuild.Modules.Payments.Commands;

/// <summary>
///     Handler for TransferFundsCommand
/// </summary>
public class TransferFundsCommandHandler : IRequestHandler<TransferFundsCommand, (WalletTransaction DebitTransaction, WalletTransaction CreditTransaction)>
{
    private readonly IWalletService _walletService;
    private readonly ILogger<TransferFundsCommandHandler> _logger;

    public TransferFundsCommandHandler(
        IWalletService walletService,
        ILogger<TransferFundsCommandHandler> logger)
    {
        _walletService = walletService;
        _logger = logger;
    }

    public async Task<(WalletTransaction DebitTransaction, WalletTransaction CreditTransaction)> Handle(
        TransferFundsCommand request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Transferring funds from user {FromUserId} to user {ToUserId}: {Amount}",
            request.FromUserId, request.ToUserId, request.Amount);

        var (debitTransaction, creditTransaction) = await _walletService.TransferFundsAsync(
            request.FromUserId,
            request.ToUserId,
            request.Amount,
            request.Description,
            request.ReferenceId,
            cancellationToken);

        _logger.LogInformation("Funds transferred: Debit {DebitId}, Credit {CreditId}",
            debitTransaction.Id, creditTransaction.Id);

        return (debitTransaction, creditTransaction);
    }
}
