using GameGuild.CQRS;

namespace GameGuild.Subscriptions.Commands;

public record SetSubscriptionPlanExternalIdCommand(Guid Id, string ExternalId) : ICommand;
