using System.ComponentModel.DataAnnotations;
using GameGuild.Common;


namespace GameGuild.Modules.Users;

public class CreateUserDto
{
    [Required]
    [StringLength(100, MinimumLength = 1)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [StringLength(255)]
    public string Email { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    [Range(0, double.MaxValue)]
    public decimal InitialBalance { get; set; } = 0;
}

public class UpdateUserDto
{
    [StringLength(100, MinimumLength = 1)]
    public string? Name { get; set; }

    [EmailAddress]
    [StringLength(255)]
    public string? Email { get; set; }

    public bool? IsActive { get; set; }
    
    /// <summary>
    /// Expected version for optimistic concurrency control
    /// </summary>
    public int? ExpectedVersion { get; set; }
}

/// <summary>
/// DTO for updating user balance
/// </summary>
public class UpdateUserBalanceDto
{
    [Range(0, double.MaxValue)]
    public decimal Balance { get; set; }

    [Range(0, double.MaxValue)]
    public decimal AvailableBalance { get; set; }

    public string? Reason { get; set; }
    
    /// <summary>
    /// Expected version for optimistic concurrency control
    /// </summary>
    public int? ExpectedVersion { get; set; }
}

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
