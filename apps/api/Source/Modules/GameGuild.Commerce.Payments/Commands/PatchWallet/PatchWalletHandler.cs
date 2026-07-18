using GameGuild.CQRS;
using GameGuild.Identity.Context.Actors;
using System.Text.Json;

namespace GameGuild.Commerce.Payments.Commands.PatchWallet;

/// <summary>
///     Handler for PatchWalletCommand
/// </summary>
public sealed class PatchWalletHandler(
    IWalletService walletService,
    IRevenueAuditService revenueAuditService,
    IActorContextAccessor actorContextAccessor) : ICommandHandler<PatchWalletCommand>
{
    public async Task<Unit> Handle(PatchWalletCommand request, CancellationToken cancellationToken)
    {
        await walletService.UpdateWalletSettingsAsync(
            request.WalletId,
            request.Currency,
            request.DailyLimit,
            request.MonthlyLimit,
            cancellationToken).ConfigureAwait(false);
        await revenueAuditService.RecordAuditTrailAsync(
            "UserWallet",
            request.WalletId,
            "ConfigurationChanged",
            RequireActorId(),
            newValue: JsonSerializer.Serialize(new
            {
                request.Currency,
                request.DailyLimit,
                request.MonthlyLimit
            }),
            reason: "Administrative wallet settings update",
            cancellationToken: cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }

    private Guid RequireActorId() => actorContextAccessor.ActorContext.SubjectIdAsGuid
        ?? throw new UnauthorizedAccessException("An authenticated administrator is required.");
}
