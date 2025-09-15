using GameGuild.CQRS;

namespace GameGuild.Modules.Subscriptions.Commands.CreateSubscription;

/// <summary>
/// Command to create a new subscription
/// </summary>
public record CreateSubscriptionCommand(
    Guid UserId,
    Guid SubscriptionPlanId,
    BillingCycle BillingCycle,
    decimal Amount,
    string Currency = "USD",
    DateTime? StartDate = null,
    int? TrialDays = null
) : ICommand<Guid>;
