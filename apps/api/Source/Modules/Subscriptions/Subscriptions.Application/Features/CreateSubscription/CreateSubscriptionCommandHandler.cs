using GameGuild.CQRS;
using GameGuild.Modules.Subscriptions.Abstractions;
using GameGuild.Modules.Subscriptions.Entities;

namespace GameGuild.Modules.Subscriptions.Features.CreateSubscription;

/// <summary>
///     Command handler for creating a new subscription
/// </summary>
public class CreateSubscriptionCommandHandler : ICommandHandler<CreateSubscriptionCommand, Guid>
{
    private readonly ISubscriptionRepository _subscriptionRepository;

    public CreateSubscriptionCommandHandler(ISubscriptionRepository subscriptionRepository) { _subscriptionRepository = subscriptionRepository; }

    public async Task<Guid> Handle(CreateSubscriptionCommand request, CancellationToken cancellationToken)
    {
        DateTime startDate = request.StartDate ?? DateTime.UtcNow;
        var trialEndDate = request.TrialDays.HasValue
            ? startDate.AddDays(request.TrialDays.Value)
            : (DateTime?)null;

        var subscription = new Subscription(
            request.TenantId,
            request.PlanId,
            request.CreatedByUserId,
            request.BillingCycle,
            new Money(request.Amount),
            startDate,
            trialEndDate
        );

        await _subscriptionRepository.AddAsync(subscription, cancellationToken).ConfigureAwait(false);

        return subscription.Id;
    }
}

