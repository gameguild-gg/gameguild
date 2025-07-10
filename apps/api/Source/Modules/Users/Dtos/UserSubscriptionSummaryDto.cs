using GameGuild.Common;


namespace GameGuild.Modules.Users;

public class UserSubscriptionSummaryDto {
  public Guid Id { get; set; }
  
  public SubscriptionStatus Status { get; set; }
  
  public string PlanName { get; set; } = string.Empty;
  
  public DateTime CurrentPeriodStart { get; set; }
  
  public DateTime CurrentPeriodEnd { get; set; }
  
  public DateTime? TrialEndsAt { get; set; }
  
  public DateTime? NextBillingAt { get; set; }
  
  public bool IsTrialActive { get; set; }
  
  public bool IsActive { get; set; }
}