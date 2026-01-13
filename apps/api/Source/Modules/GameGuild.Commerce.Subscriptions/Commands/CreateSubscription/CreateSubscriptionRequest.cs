using GameGuild.ValueObjects;

namespace GameGuild.Commerce.Subscriptions;

/// <summary>
///     Request model for creating a subscription
/// </summary>
public record CreateSubscriptionRequest(Guid TenantId, Guid PlanId, Guid CreatedByUserId, BillingCycle BillingCycle, decimal Amount, string Currency, DateTime? StartDate, int? TrialDays);
