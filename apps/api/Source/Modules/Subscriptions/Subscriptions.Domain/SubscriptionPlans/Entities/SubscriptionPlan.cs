using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using GameGuild.Database;
using GameGuild.Modules.Subscriptions.Entities;
using GameGuild.Modules.Subscriptions.SubscriptionPlans.Abstractions;
using GameGuild.Modules.Subscriptions.SubscriptionPlans.Events;

namespace GameGuild.Modules.Subscriptions.SubscriptionPlans.Entities;

/// <summary>
///     Represents a subscription plan that defines features, limits, and pricing for tenants
/// </summary>
[Table("SubscriptionPlans")]
[Index(nameof(Name), IsUnique = true)]
[Index(nameof(ExternalId), IsUnique = true)]
public class SubscriptionPlan : EntityBase, ISubscriptionPlan
{
    /// <summary>
    ///     Default constructor for EF
    /// </summary>
    private SubscriptionPlan() { }

    /// <summary>
    ///     Creates a new subscription plan
    /// </summary>
    public SubscriptionPlan(
        string name,
        string slug,
        long monthlyPriceInCents,
        string currency = "USD",
        string? description = null)
    {
        Name = name;
        Slug = slug;
        MonthlyPriceInCents = monthlyPriceInCents;
        Currency = currency;
        Description = description;

        Raise(new SubscriptionPlanCreatedEvent(Id, name, monthlyPriceInCents));
    }

    /// <summary>
    ///     External ID for integration with payment providers (Stripe, etc.)
    /// </summary>
    [MaxLength(100)]
    public string? ExternalId { get; set; }

    /// <summary>
    ///     Whether this plan is featured/highlighted
    /// </summary>
    public bool IsFeatured { get; set; }

    /// <summary>
    ///     Display order for sorting plans
    /// </summary>
    public int SortOrder { get; set; }

    /// <summary>
    ///     Whether this plan includes priority support
    /// </summary>
    public bool HasPrioritySupport { get; set; }

    /// <summary>
    ///     Whether this plan includes advanced analytics
    /// </summary>
    public bool HasAdvancedAnalytics { get; set; }

    /// <summary>
    ///     Whether this plan includes custom branding
    /// </summary>
    public bool HasCustomBranding { get; set; }

    /// <summary>
    ///     Features included in this plan (JSON serialized list of feature codes)
    /// </summary>
    [MaxLength(2000)]
    public string? Features { get; set; }

    /// <summary>
    ///     Additional metadata for the plan (JSON serialized)
    /// </summary>
    [MaxLength(4000)]
    public string? Metadata { get; set; }

    /// <summary>
    ///     Trial period in days (0 = no trial)
    /// </summary>
    public int TrialPeriodDays { get; set; } = 0;

    // Navigation properties
    public virtual ICollection<Subscription> Subscriptions { get; set; } = new List<Subscription>();

    /// <summary>
    ///     Display name of the plan
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    ///     URL-friendly slug for the plan
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string Slug { get; set; } = string.Empty;

    /// <summary>
    ///     Detailed description of the plan
    /// </summary>
    [MaxLength(1000)]
    public string? Description { get; set; }

    /// <summary>
    ///     Monthly price in the smallest currency unit (cents for USD, pence for GBP, etc.)
    /// </summary>
    public long MonthlyPriceInCents { get; set; }

    /// <summary>
    ///     Annual price in the smallest currency unit (with potential discount)
    /// </summary>
    public long? AnnualPriceInCents { get; set; }

    /// <summary>
    ///     Currency code (ISO 4217)
    /// </summary>
    [Required]
    [MaxLength(3)]
    public string Currency { get; set; } = "USD";

    /// <summary>
    ///     Whether this plan is currently available for new subscriptions
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    ///     Maximum number of users allowed in this plan (null = unlimited)
    /// </summary>
    public int? MaxUsers { get; set; }

    /// <summary>
    ///     Maximum storage in MB allowed in this plan (null = unlimited)
    /// </summary>
    public long? MaxStorageMb { get; set; }

    /// <summary>
    ///     Maximum number of API calls per month (null = unlimited)
    /// </summary>
    public long? MaxApiCallsPerMonth { get; set; }

    /// <summary>
    ///     Gets the monthly price as Money value object
    /// </summary>
    public Money GetMonthlyPrice() { return new Money(MonthlyPriceInCents / 100m, Currency); }

    /// <summary>
    ///     Gets the annual price as Money value object (if available)
    /// </summary>
    public Money? GetAnnualPrice()
    {
        return AnnualPriceInCents.HasValue
            ? new Money(AnnualPriceInCents.Value / 100m, Currency)
            : null;
    }

    /// <summary>
    ///     Checks if the plan allows a specific number of users
    /// </summary>
    public bool AllowsUserCount(int userCount) { return MaxUsers == null || userCount <= MaxUsers; }

    /// <summary>
    ///     Checks if the plan allows a specific storage amount
    /// </summary>
    public bool AllowsStorage(long storageMb) { return MaxStorageMb == null || storageMb <= MaxStorageMb; }

    /// <summary>
    ///     Checks if the plan allows a specific number of API calls
    /// </summary>
    public bool AllowsApiCalls(long apiCalls) { return MaxApiCallsPerMonth == null || apiCalls <= MaxApiCallsPerMonth; }

    /// <summary>
    ///     Updates the plan details
    /// </summary>
    public void UpdateDetails(string name, string? description = null, int? sortOrder = null)
    {
        string oldName = Name;
        Name = name;
        Description = description ?? Description;
        if (sortOrder.HasValue) SortOrder = sortOrder.Value;

        Raise(new SubscriptionPlanChangedEvent(Id, oldName, name));
    }

    /// <summary>
    ///     Updates pricing information
    /// </summary>
    public void UpdatePricing(long monthlyPriceInCents, long? annualPriceInCents = null)
    {
        MonthlyPriceInCents = monthlyPriceInCents;
        AnnualPriceInCents = annualPriceInCents;
    }

    /// <summary>
    ///     Updates plan limits
    /// </summary>
    public void UpdateLimits(
        int? maxUsers = null,
        long? maxStorageMb = null,
        long? maxApiCallsPerMonth = null)
    {
        MaxUsers = maxUsers;
        MaxStorageMb = maxStorageMb;
        MaxApiCallsPerMonth = maxApiCallsPerMonth;
    }

    /// <summary>
    ///     Updates plan features
    /// </summary>
    public void UpdateFeatures(
        bool? hasPrioritySupport = null,
        bool? hasAdvancedAnalytics = null,
        bool? hasCustomBranding = null,
        string? features = null)
    {
        if (hasPrioritySupport.HasValue) HasPrioritySupport = hasPrioritySupport.Value;
        if (hasAdvancedAnalytics.HasValue) HasAdvancedAnalytics = hasAdvancedAnalytics.Value;
        if (hasCustomBranding.HasValue) HasCustomBranding = hasCustomBranding.Value;
        if (features != null) Features = features;
    }

    /// <summary>
    ///     Activates the plan for new subscriptions
    /// </summary>
    public void Activate()
    {
        if (IsActive) return;
        IsActive = true;
    }

    /// <summary>
    ///     Deactivates the plan (existing subscriptions remain active)
    /// </summary>
    public void Deactivate()
    {
        if (!IsActive) return;
        IsActive = false;

        Raise(new SubscriptionPlanDiscontinuedEvent(Id, Name));
    }

    /// <summary>
    ///     Sets the plan as featured
    /// </summary>
    public void SetFeatured(bool featured = true) { IsFeatured = featured; }

    /// <summary>
    ///     Sets external ID for payment provider integration
    /// </summary>
    public void SetExternalId(string externalId) { ExternalId = externalId; }

    /// <summary>
    ///     Calculates annual savings compared to monthly pricing
    /// </summary>
    public decimal CalculateAnnualSavingsPercentage()
    {
        if (!AnnualPriceInCents.HasValue) return 0;

        long monthlyTotal = MonthlyPriceInCents * 12;
        long savings = monthlyTotal - AnnualPriceInCents.Value;

        return monthlyTotal > 0 ? (decimal)savings / monthlyTotal * 100 : 0;
    }
}

