using GameGuild.CQRS;

namespace GameGuild.Commerce.Subscriptions;

/// <summary>
///     Query handler for getting active subscription plans.
/// </summary>
public sealed class GetActiveSubscriptionPlansQueryHandler(ISubscriptionPlanRepository subscriptionPlanRepository)
    : IRequestHandler<GetActiveSubscriptionPlansQuery, IEnumerable<SubscriptionPlan>>
{
    public async Task<IEnumerable<SubscriptionPlan>> Handle(GetActiveSubscriptionPlansQuery request, CancellationToken cancellationToken)
    {
        return await subscriptionPlanRepository.GetActiveAsync(cancellationToken).ConfigureAwait(false);
    }
}
