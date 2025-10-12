using GameGuild.CQRS;
using GameGuild.Modules.Subscriptions.Entities;


namespace GameGuild.Modules.Subscriptions.Features.GetSubscription;

public record GetPagedSubscriptionsQuery(
    int Page = 1,
    int PageSize = 10,
    SubscriptionStatus? Status = null,
    Guid? TenantId = null,
    Guid? PlanId = null
) : IQuery<IEnumerable<Subscription>>;

