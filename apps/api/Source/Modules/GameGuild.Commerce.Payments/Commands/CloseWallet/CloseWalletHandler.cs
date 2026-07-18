using GameGuild.CQRS;
using GameGuild.Identity.Context.Actors;

namespace GameGuild.Commerce.Payments.Commands.CloseWallet;

/// <summary>
///     Handler for CloseWalletCommand
/// </summary>
public sealed class CloseWalletHandler(
    IWalletService walletService,
    IRevenueAuditService revenueAuditService,
    IActorContextAccessor actorContextAccessor) : ICommandHandler<CloseWalletCommand>
{
    public async Task<Unit> Handle(CloseWalletCommand request, CancellationToken cancellationToken)
    {
        await walletService.CloseWalletAsync(request.WalletId, cancellationToken).ConfigureAwait(false);
        await revenueAuditService.RecordAuditTrailAsync(
            "UserWallet",
            request.WalletId,
            "Deleted",
            RequireActorId(),
            oldValue: "{\"isActive\":true}",
            newValue: "{\"isActive\":false}",
            reason: "Administrative wallet closure",
            cancellationToken: cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }

    private Guid RequireActorId() => actorContextAccessor.ActorContext.SubjectIdAsGuid
        ?? throw new UnauthorizedAccessException("An authenticated administrator is required.");
}
