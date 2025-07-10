namespace GameGuild.Modules.Users;

public class UserResponseDto
{
  public Guid Id { get; set; }
  public int Version { get; set; }
  public string Name { get; set; } = string.Empty;
  public string Email { get; set; } = string.Empty;
  public bool IsActive { get; set; }
  public decimal Balance { get; set; }
  public decimal AvailableBalance { get; set; }
  public DateTime CreatedAt { get; set; }
  public DateTime UpdatedAt { get; set; }
  public DateTime? DeletedAt { get; set; }
  public bool IsDeleted { get; set; }

  // Subscription information
  public UserSubscriptionSummaryDto? ActiveSubscription { get; set; }
  public string Role { get; set; } = "Game Developer";
  public string SubscriptionType { get; set; } = "Free Trial";
}