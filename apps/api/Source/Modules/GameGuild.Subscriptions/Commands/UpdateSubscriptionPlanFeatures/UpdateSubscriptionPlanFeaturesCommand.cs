using GameGuild.CQRS;

namespace GameGuild.Subscriptions.Commands;

public record UpdateSubscriptionPlanFeaturesCommand(Guid Id, bool? HasPrioritySupport, bool? HasAdvancedAnalytics, bool? HasCustomBranding, string? Features) : ICommand;
