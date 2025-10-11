using GameGuild.CQRS;


namespace GameGuild.Modules.Subscriptions.Features.SubscriptionPlans;

public record SetSubscriptionPlanExternalIdCommand(
    Guid Id,
    string ExternalId
) : ICommand;

