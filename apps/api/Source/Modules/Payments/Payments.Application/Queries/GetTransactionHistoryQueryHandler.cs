using GameGuild.CQRS;
using GameGuild.Modules.Payments.Entities;
using GameGuild.Modules.Payments.Services;
using Microsoft.Extensions.Logging;

namespace GameGuild.Modules.Payments.Queries;

/// <summary>
///     Handler for GetTransactionHistoryQuery
/// </summary>
public class GetTransactionHistoryQueryHandler : IRequestHandler<GetTransactionHistoryQuery, List<WalletTransaction>>
{
    private readonly IWalletService _walletService;
    private readonly ILogger<GetTransactionHistoryQueryHandler> _logger;

    public GetTransactionHistoryQueryHandler(
        IWalletService walletService,
        ILogger<GetTransactionHistoryQueryHandler> logger)
    {
        _walletService = walletService;
        _logger = logger;
    }

    public async Task<List<WalletTransaction>> Handle(GetTransactionHistoryQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting transaction history for user {UserId}", request.UserId);

        var transactions = await _walletService.GetTransactionHistoryAsync(
            request.UserId, 
            request.Skip, 
            request.Take, 
            request.TypeFilter, 
            request.StatusFilter, 
            cancellationToken);

        _logger.LogInformation("Retrieved {Count} transactions for user {UserId}", 
            transactions.Count, request.UserId);
            
        return transactions;
    }
}
