using GameGuild.CQRS;
using GameGuild.Modules.Payments.Domain.Entities;
using GameGuild.Modules.Payments.Payments.Application.Services;
using Microsoft.Extensions.Logging;

namespace GameGuild.Modules.Payments.Commands;

/// <summary>
///     Handler for CreateWalletCommand
/// </summary>
public class CreateWalletCommandHandler : IRequestHandler<CreateWalletCommand, UserWallet>
{
    private readonly IWalletService _walletService;
    private readonly ILogger<CreateWalletCommandHandler> _logger;

    public CreateWalletCommandHandler(
        IWalletService walletService,
        ILogger<CreateWalletCommandHandler> logger)
    {
        _walletService = walletService;
        _logger = logger;
    }

    public async Task<UserWallet> Handle(CreateWalletCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Creating wallet for user {UserId} with currency {Currency}",
            request.UserId, request.Currency);

        var wallet = await _walletService.CreateWalletAsync(request.UserId, request.Currency, cancellationToken);

        _logger.LogInformation("Wallet created: {WalletId}", wallet.Id);
        return wallet;
    }
}
