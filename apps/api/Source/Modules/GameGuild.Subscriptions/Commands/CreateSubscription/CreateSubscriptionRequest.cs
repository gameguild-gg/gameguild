namespace GameGuild.Subscriptions.Commands;

/// <summary>
///     Request model for creating a subscription
/// </summary>
public record CreateSubscriptionRequest(Guid TenantId, Guid PlanId, Guid CreatedByUserId, BillingCycle BillingCycle, decimal Amount, string Currency, DateTime? StartDate, int? TrialDays);
