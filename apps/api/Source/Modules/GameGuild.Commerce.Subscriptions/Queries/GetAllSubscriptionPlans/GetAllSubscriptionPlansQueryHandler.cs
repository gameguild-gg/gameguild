using GameGuild.CQRS;

namespace GameGuild.Commerce.Subscriptions;

/// <summary>
///     Query handler for getting every subscription plan (active and inactive).
///     Unlike <see cref="GetActiveSubscriptionPlansQuery"/>, this is the administrative
///     full-catalog read and is therefore not served from the active-plan list cache.
/// </summary>
public sealed class GetAllSubscriptionPlansQueryHandler(ISubscriptionPlanRepository subscriptionPlanRepository)
    : IQueryHandler<GetAllSubscriptionPlansQuery, IEnumerable<SubscriptionPlan>>
{
    public async Task<IEnumerable<SubscriptionPlan>> Handle(GetAllSubscriptionPlansQuery request, CancellationToken cancellationToken)
    {
        return await subscriptionPlanRepository.GetAllAsync(cancellationToken).ConfigureAwait(false);
    }
}
