using GameGuild.CQRS;

namespace GameGuild.Commerce.Subscriptions;

public sealed record UpdateSubscriptionPlanPricingCommand(Guid Id, long MonthlyPriceInCents, long? AnnualPriceInCents = null) : ICommand;
