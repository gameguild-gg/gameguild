using System.ComponentModel.DataAnnotations;
using GameGuild.Modules.Subscriptions.SubscriptionPlans.Models;

namespace GameGuild.Modules.Subscriptions.DTOs;

/// <summary>
///     Request model for creating subscription plans
/// </summary>
public class CreateSubscriptionPlanRequest
{
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(500)]
    public string Description { get; set; } = string.Empty;

    [Required]
    [Range(0, double.MaxValue)]
    public decimal Price { get; set; }

    [Required]
    [MaxLength(3)]
    public string Currency { get; set; } = "USD";

    [Required]
    public PlanInterval Interval { get; set; }

    [Range(1, int.MaxValue)]
    public int MaxUsers { get; set; } = 1;

    [Range(1, int.MaxValue)]
    public int MaxProjects { get; set; } = 1;

    [Range(1, long.MaxValue)]
    public long MaxStorage { get; set; } = 1073741824; // 1GB default

    [Range(1, int.MaxValue)]
    public int MaxApiCallsPerMonth { get; set; } = 1000;

    public bool HasAdvancedFeatures { get; set; }

    public bool HasPrioritySupport { get; set; }
}

