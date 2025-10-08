using MediatR;
using GameGuild.Modules.Subscriptions.Entities;
using GameGuild.Modules.Subscriptions.SubscriptionPlans.Entities;
using GameGuild.Modules.Subscriptions.Models;

namespace GameGuild.Modules.Subscriptions.Features.GetSubscription;

public record GetPagedSubscriptionsQuery(
    int Page = 1,
    int PageSize = 10,
    SubscriptionStatus? Status = null,
    Guid? TenantId = null,
    Guid? PlanId = null
) : IQuery<IEnumerable<Subscription>>;

