using GameGuild.CQRS;

namespace GameGuild.Subscriptions.Commands;

public record UpdateSubscriptionPlanPricingCommand(Guid Id, long MonthlyPriceInCents, long? AnnualPriceInCents = null) : ICommand;
