using GameGuild.CQRS;
using GameGuild.Modules.Subscriptions.SubscriptionPlans.Entities;
using GameGuild.Modules.Subscriptions.SubscriptionPlans.Models;

namespace GameGuild.Modules.Subscriptions.Features.SubscriptionPlans;

public record GetPagedSubscriptionPlansQuery(
    int Page = 1,
    int PageSize = 10,
    bool? IsActive = null,
    bool? IsFeatured = null,
    string? SearchTerm = null
) : IQuery<IEnumerable<SubscriptionPlan>>;

