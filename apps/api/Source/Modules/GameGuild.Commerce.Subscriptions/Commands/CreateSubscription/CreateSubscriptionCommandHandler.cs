using GameGuild.CQRS;
using GameGuild.ValueObjects;
using Microsoft.Extensions.Logging;

namespace GameGuild.Commerce.Subscriptions;

/// <summary>
///     Command handler for creating a new subscription
/// </summary>
public class CreateSubscriptionCommandHandler(
    ISubscriptionRepository subscriptionRepository,
    ILogger<CreateSubscriptionCommandHandler> logger) : ICommandHandler<CreateSubscriptionCommand, Guid>
{
    public async Task<Guid> Handle(CreateSubscriptionCommand request, CancellationToken cancellationToken)
    {
        var startDate = request.StartDate ?? DateTime.UtcNow;
        var trialEndDate = request.TrialDays.HasValue ? startDate.AddDays(request.TrialDays.Value) : (DateTime?) null;

        var subscription = new Subscription(
            request.TenantId, 
            request.PlanId, 
            request.CreatedByUserId, 
            request.BillingCycle, 
            new Money(request.Amount), 
            startDate, 
            trialEndDate);

        // Associate with fulfilled order for economic audit trail (if provided)
        if (request.FulfilledOrderId.HasValue)
        {
            subscription.SetFulfilledOrderId(request.FulfilledOrderId.Value);
            logger.LogInformation(
                "Subscription {SubscriptionId} linked to fulfilled order {OrderId}",
                subscription.Id, request.FulfilledOrderId.Value);
        }
        else
        {
            // Log warning for subscriptions created without order linkage (legacy/migration scenarios)
            logger.LogWarning(
                "Subscription {SubscriptionId} created without FulfilledOrderId linkage. " +
                "This should only occur for legacy data migration or admin corrections.",
                subscription.Id);
        }

        await subscriptionRepository.AddAsync(subscription, cancellationToken).ConfigureAwait(false);

        return subscription.Id;
    }
}
