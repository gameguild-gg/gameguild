using GameGuild.CQRS;
using GameGuild.Identity.Context.Actors;

namespace GameGuild.Commerce.Payments.Commands.FreezeWallet;

/// <summary>
///     Handler for FreezeWalletCommand
/// </summary>
public sealed class FreezeWalletHandler(
    IWalletService walletService,
    IRevenueAuditService revenueAuditService,
    IActorContextAccessor actorContextAccessor) : ICommandHandler<FreezeWalletCommand>
{
    public async Task<Unit> Handle(FreezeWalletCommand request, CancellationToken cancellationToken)
    {
        await walletService.FreezeWalletAsync(request.WalletId, request.Reason, cancellationToken).ConfigureAwait(false);
        await revenueAuditService.RecordAuditTrailAsync(
            "UserWallet",
            request.WalletId,
            "StatusChanged",
            RequireActorId(),
            oldValue: "{\"isFrozen\":false}",
            newValue: "{\"isFrozen\":true}",
            reason: request.Reason,
            cancellationToken: cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }

    private Guid RequireActorId() => actorContextAccessor.ActorContext.SubjectIdAsGuid
        ?? throw new UnauthorizedAccessException("An authenticated administrator is required.");
}
