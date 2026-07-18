using GameGuild.CQRS;
using GameGuild.Identity.Context.Actors;

namespace GameGuild.Commerce.Payments.Commands.UnfreezeWallet;

/// <summary>
///     Handler for UnfreezeWalletCommand
/// </summary>
public sealed class UnfreezeWalletHandler(
    IWalletService walletService,
    IRevenueAuditService revenueAuditService,
    IActorContextAccessor actorContextAccessor) : ICommandHandler<UnfreezeWalletCommand>
{
    public async Task<Unit> Handle(UnfreezeWalletCommand request, CancellationToken cancellationToken)
    {
        await walletService.UnfreezeWalletAsync(request.WalletId, cancellationToken).ConfigureAwait(false);
        await revenueAuditService.RecordAuditTrailAsync(
            "UserWallet",
            request.WalletId,
            "StatusChanged",
            RequireActorId(),
            oldValue: "{\"isFrozen\":true}",
            newValue: "{\"isFrozen\":false}",
            reason: "Administrative unfreeze",
            cancellationToken: cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }

    private Guid RequireActorId() => actorContextAccessor.ActorContext.SubjectIdAsGuid
        ?? throw new UnauthorizedAccessException("An authenticated administrator is required.");
}
