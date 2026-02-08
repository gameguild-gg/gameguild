using GameGuild.CQRS;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.Commerce.Subscriptions;

/// <summary>
///     Handler for PatchSubscriptionCommand.
///     Uses entity methods for modifications since property setters are private.
/// </summary>
public sealed class PatchSubscriptionHandler(IApplicationDbContext context)
    : ICommandHandler<PatchSubscriptionCommand>
{
    public async Task<Unit> Handle(PatchSubscriptionCommand request, CancellationToken cancellationToken)
    {
        var subscription = await context.Set<Subscription>()
            .FirstOrDefaultAsync(s => s.Id == request.SubscriptionId, cancellationToken)
            ?? throw new InvalidOperationException($"Subscription {request.SubscriptionId} not found");

        // Apply only provided fields using entity methods
        
        // Update billing cycle if provided (requires amount from current subscription)
        if (request.BillingCycle.HasValue && request.BillingCycle.Value != subscription.BillingCycle)
        {
            subscription.ChangeBillingCycle(request.BillingCycle.Value, subscription.Amount);
        }

        // Update auto-renew if provided
        if (request.AutoRenew.HasValue)
        {
            subscription.SetAutoRenew(request.AutoRenew.Value);
        }

        // Update external IDs if provided (SetExternalIds accepts nulls)
        if (request.ExternalSubscriptionId != null || request.ExternalCustomerId != null)
        {
            subscription.SetExternalIds(
                request.ExternalSubscriptionId ?? subscription.ExternalId,
                request.ExternalCustomerId ?? subscription.ExternalCustomerId);
        }

        // Update metadata if provided
        if (request.Metadata != null)
        {
            subscription.UpdateMetadata(request.Metadata);
        }

        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}
