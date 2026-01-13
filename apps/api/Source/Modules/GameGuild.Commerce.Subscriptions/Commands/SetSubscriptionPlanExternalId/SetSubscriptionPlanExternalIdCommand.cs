using GameGuild.CQRS;

namespace GameGuild.Commerce.Subscriptions;

public record SetSubscriptionPlanExternalIdCommand(Guid Id, string ExternalId) : ICommand;
