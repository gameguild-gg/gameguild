using GameGuild.CQRS;
using GameGuild.Modules.Payments.Services;
using Microsoft.Extensions.Logging;

namespace GameGuild.Modules.Payments.Queries;

/// <summary>
///     Handler for GetWalletBalanceQuery
/// </summary>
public class GetWalletBalanceQueryHandler : IRequestHandler<GetWalletBalanceQuery, decimal>
{
    private readonly IWalletService _walletService;
    private readonly ILogger<GetWalletBalanceQueryHandler> _logger;

    public GetWalletBalanceQueryHandler(
        IWalletService walletService,
        ILogger<GetWalletBalanceQueryHandler> logger)
    {
        _walletService = walletService;
        _logger = logger;
    }

    public async Task<decimal> Handle(GetWalletBalanceQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting balance for user {UserId}", request.UserId);

        var balance = await _walletService.GetBalanceAsync(request.UserId, cancellationToken);

        _logger.LogInformation("Balance retrieved for user {UserId}: {Balance}", request.UserId, balance);

        return balance;
    }
}
