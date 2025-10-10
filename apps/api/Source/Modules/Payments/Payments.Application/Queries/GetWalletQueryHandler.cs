using GameGuild.CQRS;
using GameGuild.Modules.Payments.Entities;
using GameGuild.Modules.Payments.Services;
using Microsoft.Extensions.Logging;

namespace GameGuild.Modules.Payments.Queries;

/// <summary>
///     Handler for GetWalletQuery
/// </summary>
public class GetWalletQueryHandler : IRequestHandler<GetWalletQuery, UserWallet?>
{
    private readonly IWalletService _walletService;
    private readonly ILogger<GetWalletQueryHandler> _logger;

    public GetWalletQueryHandler(
        IWalletService walletService,
        ILogger<GetWalletQueryHandler> logger)
    {
        _walletService = walletService;
        _logger = logger;
    }

    public async Task<UserWallet?> Handle(GetWalletQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting wallet for user {UserId}", request.UserId);

        var wallet = await _walletService.GetWalletByUserIdAsync(request.UserId, cancellationToken);

        if (wallet == null)
        {
            _logger.LogWarning("Wallet not found for user {UserId}", request.UserId);
        }

        return wallet;
    }
}
