using GameGuild.CQRS;

namespace GameGuild.Commerce.Subscriptions;

/// <summary>
///     Query handler for getting paginated subscriptions
/// </summary>
public sealed class GetPagedSubscriptionsQueryHandler(ISubscriptionRepository subscriptionRepository) : IQueryHandler<GetPagedSubscriptionsQuery, PagedResult<Subscription>>
{
    public async Task<PagedResult<Subscription>> Handle(GetPagedSubscriptionsQuery request, CancellationToken cancellationToken)
    {
        var pagedResult = await subscriptionRepository.GetPagedAsync(request.Page, request.PageSize, request.Status, request.TenantId, request.PlanId, cancellationToken).ConfigureAwait(false);

        return pagedResult;
    }
}
