using GameGuild.CQRS;
using GameGuild.Resources;

namespace GameGuild.Commerce.Subscriptions;

/// <summary>
///     Command to create a new subscription plan
/// </summary>
[RequiresQuota(ResourceUsageType.SubscriptionPlans, Source = "CreateSubscriptionPlan")]
public record CreateSubscriptionPlanCommand(string Name, string Slug, long MonthlyPriceInCents, string Currency = "USD", string? Description = null) : ICommand<Guid>;
