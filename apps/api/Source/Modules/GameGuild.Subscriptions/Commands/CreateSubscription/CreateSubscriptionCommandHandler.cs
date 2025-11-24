using GameGuild.CQRS;
using GameGuild.Subscriptions.Abstractions;
using GameGuild.Subscriptions.Entities;

namespace GameGuild.Subscriptions.Commands;

/// <summary>
///     Command handler for creating a new subscription
/// </summary>
public class CreateSubscriptionCommandHandler(ISubscriptionRepository subscriptionRepository) : ICommandHandler<CreateSubscriptionCommand, Guid>
{
    public async Task<Guid> Handle(CreateSubscriptionCommand request, CancellationToken cancellationToken)
    {
        var startDate = request.StartDate ?? DateTime.UtcNow;
        var trialEndDate = request.TrialDays.HasValue ? startDate.AddDays(request.TrialDays.Value) : (DateTime?) null;

        var subscription = new Subscription(request.TenantId, request.PlanId, request.CreatedByUserId, request.BillingCycle, new Money(request.Amount), startDate, trialEndDate);

        await subscriptionRepository.AddAsync(subscription, cancellationToken).ConfigureAwait(false);

        return subscription.Id;
    }
}
