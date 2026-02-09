using GameGuild.CQRS;

namespace GameGuild.Commerce.Subscriptions;

/// <summary>
///     Query handler for getting subscription plans within a price range
/// </summary>
public sealed class GetSubscriptionPlansByPriceRangeQueryHandler(ISubscriptionPlanRepository subscriptionPlanRepository) 
    : IQueryHandler<GetSubscriptionPlansByPriceRangeQuery, IEnumerable<SubscriptionPlan>>
{
    public async Task<IEnumerable<SubscriptionPlan>> Handle(GetSubscriptionPlansByPriceRangeQuery request, CancellationToken cancellationToken)
    {
        // Convert decimal dollars to cents for repository call
        var minPriceInCents = (long)(request.MinPrice * 100);
        var maxPriceInCents = (long)(request.MaxPrice * 100);
        
        return await subscriptionPlanRepository.GetByPriceRangeAsync(minPriceInCents, maxPriceInCents, cancellationToken).ConfigureAwait(false);
    }
}
