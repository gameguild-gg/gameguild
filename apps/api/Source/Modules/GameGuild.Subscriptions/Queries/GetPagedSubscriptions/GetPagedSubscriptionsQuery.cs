using GameGuild.CQRS;
using GameGuild.Subscriptions.Entities;
using GameGuild.Subscriptions.Models;

namespace GameGuild.Subscriptions.Queries;

public record GetPagedSubscriptionsQuery(int Page = 1, int PageSize = 10, SubscriptionStatus? Status = null, Guid? TenantId = null, Guid? PlanId = null) : IQuery<PagedResult<Subscription>>;
