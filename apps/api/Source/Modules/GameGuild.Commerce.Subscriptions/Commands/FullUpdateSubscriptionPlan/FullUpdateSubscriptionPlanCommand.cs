using GameGuild.CQRS;

namespace GameGuild.Commerce.Subscriptions;

/// <summary>
///     Command to perform a full update on a subscription plan
/// </summary>
public sealed record FullUpdateSubscriptionPlanCommand(
    Guid PlanId,
    string Name,
    string Slug,
    string? Description,
    long MonthlyPriceInCents,
    long? AnnualPriceInCents,
    int? MaxUsers,
    long? MaxStorageMb,
    long? MaxApiCallsPerMonth,
    bool? HasPrioritySupport,
    bool? HasAdvancedAnalytics,
    bool? HasCustomBranding,
    string? Features,
    int? SortOrder) : ICommand;
