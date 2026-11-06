using GameGuild.CQRS;
using GameGuild.Identity.Context.Actors;

namespace GameGuild.Identity.Authentication;

/// <summary>Owner-only cancellation is guarded at the command boundary as well as the HTTP boundary.</summary>
public sealed class CancelMfaRecoveryCommandHandler(IActorContextAccessor actors, IMfaRecoveryRequestStore recoveryStore,
    IAuthenticationAuditEventSink auditSink) : ICommandHandler<CancelMfaRecoveryCommand, MfaRecoveryCancelResult>
{
    public async Task<MfaRecoveryCancelResult> Handle(CancelMfaRecoveryCommand request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var actor = actors.ActorContext;
        if (!actor.IsAuthenticated || actor.ActorKind != ActorKind.User || actor.SubjectIdAsGuid is not { } userId || userId == Guid.Empty)
        {
            throw new UnauthorizedAccessException("An authenticated user is required to cancel recovery.");
        }
        var cancelled = await recoveryStore.CancelLiveByUserAsync(userId.ToString(), cancellationToken).ConfigureAwait(false);
        if (cancelled is null)
        {
            return new MfaRecoveryCancelResult { Cancelled = false, Message = "No active multi-factor recovery request." };
        }
        await MfaRecoveryAudit.RecordAsync(auditSink, cancelled, MfaRecoveryAudit.Cancelled, "OwnerCancellation", cancellationToken).ConfigureAwait(false);
        return new MfaRecoveryCancelResult { Cancelled = true, Message = "Multi-factor recovery request cancelled." };
    }
}
