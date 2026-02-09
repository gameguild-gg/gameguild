using GameGuild.CQRS;

namespace GameGuild.Commerce.Subscriptions;

public sealed record UpdateSubscriptionPlanFeaturesCommand(Guid Id, bool? HasPrioritySupport, bool? HasAdvancedAnalytics, bool? HasCustomBranding, string? Features) : ICommand;
