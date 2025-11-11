using GameGuild.CQRS;

namespace GameGuild.Subscriptions.Commands;

public record CreateSubscriptionPlanCommand(string Name, string Slug, long MonthlyPriceInCents, string Currency = "USD", string? Description = null) : ICommand<Guid>;
