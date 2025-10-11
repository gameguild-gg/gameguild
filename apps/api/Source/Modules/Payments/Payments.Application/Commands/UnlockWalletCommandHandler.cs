using GameGuild.CQRS;
using GameGuild.Modules.Payments.Payments.Application.Services;
using Microsoft.Extensions.Logging;

namespace GameGuild.Modules.Payments.Commands;

/// <summary>
///     Handler for UnlockWalletCommand
/// </summary>
public class UnlockWalletCommandHandler : ICommandHandler<UnlockWalletCommand, Unit>
{
    private readonly IWalletService _walletService;
    private readonly ILogger<UnlockWalletCommandHandler> _logger;

    public UnlockWalletCommandHandler(
        IWalletService walletService,
        ILogger<UnlockWalletCommandHandler> logger)
    {
        _walletService = walletService;
        _logger = logger;
    }

    public async Task<Unit> Handle(UnlockWalletCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Unlocking wallet for user {UserId}", request.UserId);

        await _walletService.UnlockWalletAsync(request.UserId, cancellationToken);

        _logger.LogInformation("Wallet unlocked for user {UserId}", request.UserId);

        return Unit.Value;
    }
}
