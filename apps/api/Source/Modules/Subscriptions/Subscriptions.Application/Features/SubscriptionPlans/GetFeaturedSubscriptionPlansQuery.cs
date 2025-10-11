using GameGuild.CQRS;
using GameGuild.Modules.Subscriptions.SubscriptionPlans.Entities;


namespace GameGuild.Modules.Subscriptions.Features.SubscriptionPlans;

public record GetFeaturedSubscriptionPlansQuery() : IQuery<IEnumerable<SubscriptionPlan>>;

