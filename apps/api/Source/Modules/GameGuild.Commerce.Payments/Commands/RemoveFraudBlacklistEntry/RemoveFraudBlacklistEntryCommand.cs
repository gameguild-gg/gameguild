using GameGuild.CQRS;
using GameGuild.Identity.Authorization;
using GameGuild.Identity.Context.Actors;

namespace GameGuild.Commerce.Payments;

/// <summary>
///     Removes (soft-deletes) a fraud blacklist entry owned by the acting administrator's
///     tenant (issue #409). The tenant comes from the actor context, never from the route.
///     Requires the <c>payments:admin</c> permission.
/// </summary>
/// <param name="EntryId">Entry to remove.</param>
[AuthorizeRequest(PaymentsPermission.Keys.Admin)]
public sealed record RemoveFraudBlacklistEntryCommand(Guid EntryId) : ICommand<RemoveFraudBlacklistEntryResult>;

/// <summary>Result of a blacklist-entry removal.</summary>
public sealed record RemoveFraudBlacklistEntryResult
{
    /// <summary>Whether an entry was removed.</summary>
    public required bool Success { get; init; }

    /// <summary>Entry that was removed.</summary>
    public required Guid EntryId { get; init; }

    /// <summary>Failure reason when <see cref="Success"/> is false.</summary>
    public string? FailureReason { get; init; }
}

/// <summary>Handler for <see cref="RemoveFraudBlacklistEntryCommand"/>.</summary>
public sealed class RemoveFraudBlacklistEntryCommandHandler(
    IFraudBlacklistService blacklistService,
    IActorContextAccessor actorContextAccessor) : ICommandHandler<RemoveFraudBlacklistEntryCommand, RemoveFraudBlacklistEntryResult>
{
    /// <inheritdoc />
    public async Task<RemoveFraudBlacklistEntryResult> Handle(RemoveFraudBlacklistEntryCommand request, CancellationToken cancellationToken)
    {
        var actor = actorContextAccessor.ActorContext;
        var tenantId = actor.TenantId
            ?? throw new UnauthorizedAccessException("A tenant context is required to manage the fraud blacklist.");

        var removed = await blacklistService.RemoveAsync(tenantId, request.EntryId, cancellationToken).ConfigureAwait(false);

        return new RemoveFraudBlacklistEntryResult
        {
            Success = removed,
            EntryId = request.EntryId,
            FailureReason = removed ? null : $"No live fraud blacklist entry {request.EntryId} for the caller's tenant."
        };
    }
}
