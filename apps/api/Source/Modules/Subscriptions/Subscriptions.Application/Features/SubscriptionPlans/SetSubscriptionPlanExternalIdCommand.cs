using MediatR;
using GameGuild.Modules.Subscriptions.SubscriptionPlans.Entities;

namespace GameGuild.Modules.Subscriptions.Features.SubscriptionPlans;

public record SetSubscriptionPlanExternalIdCommand(
    Guid Id,
    string ExternalId
) : ICommand;

