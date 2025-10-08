using MediatR;
using GameGuild.Modules.Subscriptions.SubscriptionPlans.Entities;
using GameGuild.Modules.Subscriptions.SubscriptionPlans.Models;

namespace GameGuild.Modules.Subscriptions.Features.SubscriptionPlans;

public record GetSubscriptionPlanByIdQuery(Guid Id) : IQuery<SubscriptionPlan?>;

