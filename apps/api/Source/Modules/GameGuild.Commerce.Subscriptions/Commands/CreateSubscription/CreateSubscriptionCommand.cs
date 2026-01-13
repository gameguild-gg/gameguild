using GameGuild.CQRS;
using GameGuild.ValueObjects;

namespace GameGuild.Commerce.Subscriptions;

/// <summary>
///     Command to create a new subscription
/// </summary>
public record CreateSubscriptionCommand(Guid TenantId, Guid PlanId, Guid CreatedByUserId, BillingCycle BillingCycle, decimal Amount, DateTime? StartDate = null, int? TrialDays = null) : ICommand<Guid>;
