using GameGuild.CQRS;

namespace GameGuild.Commerce.Subscriptions;

public sealed record SetSubscriptionPlanExternalIdCommand(Guid Id, string ExternalId) : ICommand;
