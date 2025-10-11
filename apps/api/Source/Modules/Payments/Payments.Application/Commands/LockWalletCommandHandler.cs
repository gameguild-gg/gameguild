using GameGuild.CQRS;
using GameGuild.Modules.Payments.Payments.Application.Services;
using Microsoft.Extensions.Logging;

namespace GameGuild.Modules.Payments.Commands;

/// <summary>
///     Handler for LockWalletCommand
/// </summary>
public class LockWalletCommandHandler : ICommandHandler<LockWalletCommand, Unit>
{
    private readonly IWalletService _walletService;
    private readonly ILogger<LockWalletCommandHandler> _logger;

    public LockWalletCommandHandler(
        IWalletService walletService,
        ILogger<LockWalletCommandHandler> logger)
    {
        _walletService = walletService;
        _logger = logger;
    }

    public async Task<Unit> Handle(LockWalletCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Locking wallet for user {UserId}. Reason: {Reason}",
            request.UserId, request.Reason);

        await _walletService.LockWalletAsync(request.UserId, request.Reason, cancellationToken);

        _logger.LogInformation("Wallet locked for user {UserId}", request.UserId);

        return Unit.Value;
    }
}
