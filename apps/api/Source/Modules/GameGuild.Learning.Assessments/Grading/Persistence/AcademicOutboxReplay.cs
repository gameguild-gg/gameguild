using GameGuild.CQRS;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.Learning.Assessments.Grading.Persistence;

/// <summary>
///     Requeues dead-lettered academic outbox deliveries (issue #1218 G5), mirroring the core
///     event transport's <c>IEventReplayService</c>/<c>ReplayOutboxEventCommand</c> pair: an
///     operator fixes the frozen consumer (deploys it, corrects the poison handler), then
///     replays the message — the affected deliveries return to Pending with a fresh attempt
///     budget and the message is live again.
/// </summary>
public interface IAcademicOutboxReplayService
{
    /// <summary>
    ///     Requeues the dead-lettered deliveries of one outbox message. When
    ///     <paramref name="consumerKey"/> is null every dead-lettered delivery of the message is
    ///     replayed; otherwise only that consumer's. Returns false when the message does not
    ///     exist or has no matching dead-lettered delivery.
    /// </summary>
    Task<bool> ReplayAsync(
        Guid messageId,
        string? consumerKey = null,
        CancellationToken cancellationToken = default);
}

public sealed class AcademicOutboxReplayService(IApplicationDbContext context) : IAcademicOutboxReplayService
{
    public async Task<bool> ReplayAsync(
        Guid messageId,
        string? consumerKey = null,
        CancellationToken cancellationToken = default)
    {
        var message = await context.Set<AcademicOutboxMessage>()
            .FirstOrDefaultAsync(value => value.Id == messageId, cancellationToken)
            .ConfigureAwait(false);
        if (message is null)
        {
            return false;
        }

        var deliveries = await context.Set<AcademicOutboxDelivery>()
            .Where(value => value.OutboxMessageId == messageId
                && value.Status == AcademicOutboxDeliveryStatus.DeadLettered
                && (consumerKey == null || value.ConsumerKey == consumerKey))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        if (deliveries.Count == 0)
        {
            return false;
        }

        var now = SystemClock.UtcNow;
        foreach (var delivery in deliveries)
        {
            delivery.TryRequeue(now);
        }

        message.MarkRequeued();
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return true;
    }
}

/// <summary>
///     Replays dead-lettered academic outbox deliveries (issue #1218 G5): mirrors
///     <c>ReplayOutboxEventCommand</c> from the core event transport.
/// </summary>
public sealed record ReplayAcademicOutboxEventCommand(Guid MessageId, string? ConsumerKey)
    : ICommand<bool>;

public sealed class ReplayAcademicOutboxEventCommandHandler(IAcademicOutboxReplayService replayService)
    : ICommandHandler<ReplayAcademicOutboxEventCommand, bool>
{
    public Task<bool> Handle(ReplayAcademicOutboxEventCommand request, CancellationToken cancellationToken) =>
        replayService.ReplayAsync(request.MessageId, request.ConsumerKey, cancellationToken);
}
