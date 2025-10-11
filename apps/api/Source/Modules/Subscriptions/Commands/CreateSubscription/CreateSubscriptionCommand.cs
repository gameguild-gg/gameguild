using GameGuild.CQRS;


namespace GameGuild.Modules.Subscriptions.Commands.CreateSubscription;

/// <summary>
/// Command to create a new user subscription with billing configuration
/// </summary>
/// <param name="UserId">The unique identifier of the user subscribing</param>
/// <param name="SubscriptionPlanId">The unique identifier of the subscription plan</param>
/// <param name="BillingCycle">The billing cycle frequency (monthly, quarterly, etc.)</param>
/// <param name="Amount">The subscription amount in the specified currency</param>
/// <param name="Currency">The currency code (default: USD)</param>
/// <param name="StartDate">Optional start date (default: current UTC time)</param>
/// <param name="TrialDays">Optional trial period in days</param>
/// <returns>The unique identifier of the created subscription</returns>
public record CreateSubscriptionCommand(
  Guid UserId,
  Guid SubscriptionPlanId,
  BillingCycle BillingCycle,
  decimal Amount,
  string Currency = "USD",
  DateTime? StartDate = null,
  int? TrialDays = null
) : ICommand<Guid>;
