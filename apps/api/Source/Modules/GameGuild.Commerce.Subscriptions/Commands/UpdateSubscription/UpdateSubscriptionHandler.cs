using GameGuild.CQRS;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.Commerce.Subscriptions;

/// <summary>
///     Handler for UpdateSubscriptionCommand.
///     Uses entity methods for modifications since property setters are private.
/// </summary>
public sealed class UpdateSubscriptionHandler(IApplicationDbContext context)
    : ICommandHandler<UpdateSubscriptionCommand>
{
    public async Task<Unit> Handle(UpdateSubscriptionCommand request, CancellationToken cancellationToken)
    {
        var subscription = await context.Set<Subscription>()
            .FirstOrDefaultAsync(s => s.Id == request.SubscriptionId, cancellationToken)
            ?? throw new InvalidOperationException($"Subscription {request.SubscriptionId} not found");

        // Use entity methods for updates (they have proper validation and raise events)
        // Create Money value object with the request amount and subscription's currency
        var newAmount = new Money(request.Amount, subscription.Amount.Currency);
        
        // Update plan if changed
        if (request.PlanId != subscription.PlanId)
        {
            subscription.ChangePlan(request.PlanId, newAmount);
        }
        
        // Update billing cycle if changed
        if (request.BillingCycle != subscription.BillingCycle)
        {
            subscription.ChangeBillingCycle(request.BillingCycle, newAmount);
        }
        
        // Update auto-renew
        subscription.SetAutoRenew(request.AutoRenew);
        
        // Update external IDs
        subscription.SetExternalIds(request.ExternalSubscriptionId, request.ExternalCustomerId);

        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}
