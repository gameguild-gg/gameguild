using GameGuild.CQRS;

namespace GameGuild.Commerce.Subscriptions;

/// <summary>
///     Query handler for getting a subscription plan by slug
/// </summary>
public class GetSubscriptionPlanBySlugQueryHandler(ISubscriptionPlanRepository subscriptionPlanRepository) 
    : IQueryHandler<GetSubscriptionPlanBySlugQuery, SubscriptionPlan?>
{
    public async Task<SubscriptionPlan?> Handle(GetSubscriptionPlanBySlugQuery request, CancellationToken cancellationToken)
    {
        return await subscriptionPlanRepository.GetBySlugAsync(request.Slug, cancellationToken).ConfigureAwait(false);
    }
}
