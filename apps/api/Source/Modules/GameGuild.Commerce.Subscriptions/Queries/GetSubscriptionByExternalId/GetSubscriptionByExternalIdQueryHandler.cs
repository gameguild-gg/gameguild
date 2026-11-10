using GameGuild.CQRS;

namespace GameGuild.Commerce.Subscriptions;

/// <summary>
///     Query handler for resolving a subscription by its provider external ID —
///     the lookup webhook consumers need when reconciling external state.
/// </summary>
public sealed class GetSubscriptionByExternalIdQueryHandler(ISubscriptionRepository subscriptionRepository)
    : IQueryHandler<GetSubscriptionByExternalIdQuery, Subscription?>
{
    public async Task<Subscription?> Handle(GetSubscriptionByExternalIdQuery request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.ExternalId))
        {
            return null;
        }

        return await subscriptionRepository.GetByExternalIdAsync(request.ExternalId, cancellationToken).ConfigureAwait(false);
    }
}
