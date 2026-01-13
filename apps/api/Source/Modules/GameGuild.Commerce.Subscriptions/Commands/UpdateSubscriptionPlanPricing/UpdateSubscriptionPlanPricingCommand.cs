using GameGuild.CQRS;

namespace GameGuild.Commerce.Subscriptions;

public record UpdateSubscriptionPlanPricingCommand(Guid Id, long MonthlyPriceInCents, long? AnnualPriceInCents = null) : ICommand;
