using GameGuild.CQRS;

namespace GameGuild.Commerce.Subscriptions;

public record CreateSubscriptionPlanCommand(string Name, string Slug, long MonthlyPriceInCents, string Currency = "USD", string? Description = null) : ICommand<Guid>;
