namespace GameGuild.Modules.Subscriptions.Services;

/// <summary>
/// DTO for creating a new subscription
/// </summary>
public class CreateSubscriptionDto {
  public Guid SubscriptionPlanId { get; set; }

  public Guid? PaymentMethodId { get; set; }

  public string? ExternalSubscriptionId { get; set; }

  public DateTime? TrialEndsAt { get; set; }
}
