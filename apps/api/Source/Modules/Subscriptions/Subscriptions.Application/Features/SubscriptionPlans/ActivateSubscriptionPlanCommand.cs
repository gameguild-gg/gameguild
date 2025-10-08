using MediatR;
using GameGuild.Modules.Subscriptions.SubscriptionPlans.Entities;

namespace GameGuild.Modules.Subscriptions.Features.SubscriptionPlans;

public record ActivateSubscriptionPlanCommand(Guid Id) : ICommand;

