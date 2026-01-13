using GameGuild.CQRS;

namespace GameGuild.Commerce.Subscriptions;

/// <summary>
///     Query handler for getting paginated subscriptions
/// </summary>
public class GetPagedSubscriptionsQueryHandler(ISubscriptionRepository subscriptionRepository) : IQueryHandler<GetPagedSubscriptionsQuery, Models.PagedResult<Subscription>>
{
    public async Task<Models.PagedResult<Subscription>> Handle(GetPagedSubscriptionsQuery request, CancellationToken cancellationToken)
    {
        var pagedResult = await subscriptionRepository.GetPagedAsync(request.Page, request.PageSize, request.Status, request.TenantId, request.PlanId, cancellationToken).ConfigureAwait(false);

        return pagedResult;
    }
}
