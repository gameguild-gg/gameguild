using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using GameGuild.Entities;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.Commerce.Products;

/// <summary>
///     Represents affiliate and referral commission configuration for a product.
///     Separated from Product entity to follow Single Responsibility Principle.
///     This allows commission logic to be managed independently and extended.
/// </summary>
[Table("product_commission_configs")]
[Index(nameof(ProductId), IsUnique = true)]
[Index(nameof(IsActive))]
public class ProductCommissionConfig : EntityBase
{
    /// <summary>
    ///     Private constructor for EF Core
    /// </summary>
    private ProductCommissionConfig() { }

    /// <summary>
    ///     Foreign key to the Product entity
    /// </summary>
    [Required]
    public Guid ProductId { get; private set; }

    /// <summary>
    ///     Navigation property to Product
    /// </summary>
    [ForeignKey(nameof(ProductId))]
    public virtual Product Product { get; private set; } = null!;

    /// <summary>
    ///     Whether affiliate/referral commissions are enabled for this product
    /// </summary>
    public bool IsActive { get; private set; } = true;

    /// <summary>
    ///     Referral commission percentage (0-100).
    ///     Paid to users who refer new customers directly.
    /// </summary>
    [Column(TypeName = "decimal(5,2)")]
    [Range(0, 100)]
    public decimal ReferralCommissionPercentage { get; private set; } = 30m;

    /// <summary>
    ///     Affiliate commission percentage (0-100).
    ///     Paid to registered affiliates for purchases through their links.
    /// </summary>
    [Column(TypeName = "decimal(5,2)")]
    [Range(0, 100)]
    public decimal AffiliateCommissionPercentage { get; private set; } = 30m;

    /// <summary>
    ///     Maximum discount an affiliate can offer (0-100).
    /// </summary>
    [Column(TypeName = "decimal(5,2)")]
    [Range(0, 100)]
    public decimal MaxAffiliateDiscount { get; private set; }

    /// <summary>
    ///     Minimum order value for commission to apply
    /// </summary>
    [Column(TypeName = "decimal(10,2)")]
    public decimal MinimumOrderValue { get; private set; }

    /// <summary>
    ///     Cookie duration in days for tracking referrals
    /// </summary>
    public int CookieDurationDays { get; private set; } = 30;

    /// <summary>
    ///     Whether commissions apply to recurring subscription payments
    /// </summary>
    public bool CommissionOnRecurring { get; private set; } = true;

    /// <summary>
    ///     Maximum number of recurring payments to pay commission on (null = unlimited)
    /// </summary>
    public int? MaxRecurringPayments { get; private set; }

    /// <summary>
    ///     Creates a new commission configuration for a product
    /// </summary>
    public static ProductCommissionConfig Create(
        Guid productId,
        decimal referralCommissionPercentage = 30m,
        decimal affiliateCommissionPercentage = 30m,
        decimal maxAffiliateDiscount = 0m,
        Guid? tenantId = null)
    {
        if (productId == Guid.Empty)
            throw new ArgumentException("Product ID is required", nameof(productId));

        ValidatePercentage(referralCommissionPercentage, nameof(referralCommissionPercentage));
        ValidatePercentage(affiliateCommissionPercentage, nameof(affiliateCommissionPercentage));
        ValidatePercentage(maxAffiliateDiscount, nameof(maxAffiliateDiscount));

        return new ProductCommissionConfig
        {
            Id = Guid.NewGuid(),
            ProductId = productId,
            IsActive = true,
            ReferralCommissionPercentage = referralCommissionPercentage,
            AffiliateCommissionPercentage = affiliateCommissionPercentage,
            MaxAffiliateDiscount = maxAffiliateDiscount,
            TenantId = tenantId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    ///     Creates default commission config with standard rates
    /// </summary>
    public static ProductCommissionConfig CreateDefault(Guid productId, Guid? tenantId = null)
    {
        return Create(productId, 30m, 30m, 0m, tenantId);
    }

    /// <summary>
    ///     Updates referral commission rate
    /// </summary>
    public void SetReferralCommission(decimal percentage)
    {
        ValidatePercentage(percentage, nameof(percentage));
        ReferralCommissionPercentage = percentage;
        Touch();
    }

    /// <summary>
    ///     Updates affiliate commission rate
    /// </summary>
    public void SetAffiliateCommission(decimal percentage)
    {
        ValidatePercentage(percentage, nameof(percentage));
        AffiliateCommissionPercentage = percentage;
        Touch();
    }

    /// <summary>
    ///     Updates maximum affiliate discount
    /// </summary>
    public void SetMaxAffiliateDiscount(decimal percentage)
    {
        ValidatePercentage(percentage, nameof(percentage));
        MaxAffiliateDiscount = percentage;
        Touch();
    }

    /// <summary>
    ///     Configures recurring commission settings
    /// </summary>
    public void ConfigureRecurringCommissions(bool enabled, int? maxPayments = null)
    {
        if (maxPayments.HasValue && maxPayments.Value < 1)
            throw new ArgumentException("Max payments must be at least 1 if specified", nameof(maxPayments));

        CommissionOnRecurring = enabled;
        MaxRecurringPayments = enabled ? maxPayments : null;
        Touch();
    }

    /// <summary>
    ///     Activates or deactivates commission tracking
    /// </summary>
    public void SetActive(bool isActive)
    {
        IsActive = isActive;
        Touch();
    }

    /// <summary>
    ///     Calculates referral commission for an amount
    /// </summary>
    public decimal CalculateReferralCommission(decimal amount)
    {
        if (!IsActive || amount < MinimumOrderValue)
            return 0;

        return Math.Round(amount * (ReferralCommissionPercentage / 100m), 2);
    }

    /// <summary>
    ///     Calculates affiliate commission for an amount
    /// </summary>
    public decimal CalculateAffiliateCommission(decimal amount)
    {
        if (!IsActive || amount < MinimumOrderValue)
            return 0;

        return Math.Round(amount * (AffiliateCommissionPercentage / 100m), 2);
    }

    private static void ValidatePercentage(decimal value, string paramName)
    {
        if (value < 0 || value > 100)
            throw new ArgumentOutOfRangeException(paramName, "Percentage must be between 0 and 100");
    }
}
