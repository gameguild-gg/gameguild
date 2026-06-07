using GameGuild.CQRS;

namespace GameGuild.Commerce.Subscriptions;

/// <summary>
///     Query handler for getting a subscription plan by ID.
/// </summary>
public sealed class GetSubscriptionPlanByIdQueryHandler(ISubscriptionPlanRepository subscriptionPlanRepository)
    : IQueryHandler<GetSubscriptionPlanByIdQuery, SubscriptionPlan?>
{
    public async Task<SubscriptionPlan?> Handle(GetSubscriptionPlanByIdQuery request, CancellationToken cancellationToken)
    {
        return await subscriptionPlanRepository.GetByIdAsync(request.Id, cancellationToken).ConfigureAwait(false);
    }
}