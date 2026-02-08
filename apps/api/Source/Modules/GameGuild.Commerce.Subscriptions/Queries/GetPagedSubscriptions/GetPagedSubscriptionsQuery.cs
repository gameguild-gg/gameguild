using GameGuild.CQRS;

namespace GameGuild.Commerce.Subscriptions;

public record GetPagedSubscriptionsQuery(int Page = 1, int PageSize = 10, SubscriptionStatus? Status = null, Guid? TenantId = null, Guid? PlanId = null) : IQuery<PagedResult<Subscription>>;
