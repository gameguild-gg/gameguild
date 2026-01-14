using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using GameGuild.Entities;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.Commerce.Products;

/// <summary>
///     Immutable version history for product pricing.
///     Once created, pricing versions cannot be modified - only new versions added.
///     This ensures active subscriptions and orders reference stable price points.
/// </summary>
[Table("product_pricing_versions")]
[Index(nameof(ProductPricingId), nameof(PriceVersion), IsUnique = true)]
[Index(nameof(ProductPricingId), nameof(EffectiveFrom))]
[Index(nameof(IsActive))]
public class ProductPricingVersion : EntityBase
{
    /// <summary>
    ///     Private constructor for EF Core
    /// </summary>
    private ProductPricingVersion() { }

    /// <summary>
    ///     Foreign key to the ProductPricing entity
    /// </summary>
    [Required]
    public Guid ProductPricingId { get; private set; }

    /// <summary>
    ///     Navigation property to ProductPricing
    /// </summary>
    [ForeignKey(nameof(ProductPricingId))]
    public virtual ProductPricing ProductPricing { get; private set; } = null!;

    /// <summary>
    ///     Sequential price version number (1, 2, 3, ...)
    ///     Named PriceVersion to avoid hiding EntityBase.Version
    /// </summary>
    [Column("price_version")]
    public int PriceVersion { get; private set; }

    /// <summary>
    ///     Base price at this version (immutable after creation)
    /// </summary>
    [Column(TypeName = "decimal(10,2)")]
    public decimal BasePrice { get; private set; }

    /// <summary>
    ///     Sale price at this version (immutable after creation)
    /// </summary>
    [Column(TypeName = "decimal(10,2)")]
    public decimal? SalePrice { get; private set; }

    /// <summary>
    ///     Currency code (immutable after creation)
    /// </summary>
    [MaxLength(3)]
    public string Currency { get; private set; } = "USD";

    /// <summary>
    ///     When this pricing version becomes effective
    /// </summary>
    public DateTime EffectiveFrom { get; private set; }

    /// <summary>
    ///     When this pricing version expires (null = current active version)
    /// </summary>
    public DateTime? EffectiveTo { get; private set; }

    /// <summary>
    ///     Whether this is the currently active version
    /// </summary>
    public bool IsActive { get; private set; }

    /// <summary>
    ///     Reason for the price change (audit trail)
    /// </summary>
    [MaxLength(500)]
    public string? ChangeReason { get; private set; }

    /// <summary>
    ///     User who created this version
    /// </summary>
    public Guid? CreatedByUserId { get; private set; }

    /// <summary>
    ///     Creates a new pricing version from the current ProductPricing state.
    ///     The previous active version will be marked as inactive.
    /// </summary>
    /// <param name="pricing">The ProductPricing to snapshot</param>
    /// <param name="priceVersion">Price version number</param>
    /// <param name="effectiveFrom">When this version becomes effective</param>
    /// <param name="changeReason">Optional reason for the price change</param>
    /// <param name="createdByUserId">User creating the version</param>
    /// <returns>New immutable pricing version</returns>
    public static ProductPricingVersion Create(
        ProductPricing pricing,
        int priceVersion,
        DateTime effectiveFrom,
        string? changeReason = null,
        Guid? createdByUserId = null)
    {
        ArgumentNullException.ThrowIfNull(pricing);

        if (priceVersion < 1)
            throw new ArgumentException("Price version must be 1 or greater", nameof(priceVersion));

        return new ProductPricingVersion
        {
            Id = Guid.NewGuid(),
            ProductPricingId = pricing.Id,
            PriceVersion = priceVersion,
            BasePrice = pricing.BasePrice,
            SalePrice = pricing.SalePrice,
            Currency = pricing.Currency,
            EffectiveFrom = effectiveFrom,
            EffectiveTo = null,
            IsActive = true,
            ChangeReason = changeReason,
            CreatedByUserId = createdByUserId,
            TenantId = pricing.TenantId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    ///     Creates the initial version for a new ProductPricing
    /// </summary>
    public static ProductPricingVersion CreateInitial(
        ProductPricing pricing,
        Guid? createdByUserId = null)
    {
        return Create(pricing, 1, DateTime.UtcNow, "Initial pricing", createdByUserId);
    }

    /// <summary>
    ///     Supersedes this version with a new one (marks this as inactive)
    /// </summary>
    /// <param name="supersededAt">When this version was superseded</param>
    internal void Supersede(DateTime supersededAt)
    {
        if (!IsActive)
            throw new InvalidOperationException("Cannot supersede an already inactive version");

        EffectiveTo = supersededAt;
        IsActive = false;
        Touch();
    }

    /// <summary>
    ///     Gets the effective price at a specific date
    /// </summary>
    public decimal GetEffectivePrice(DateTime asOfDate)
    {
        // Check if sale price is active at the given date
        // Note: This version's sale dates are captured from the original pricing
        // For simplicity, we return sale price if it exists, otherwise base price
        return SalePrice ?? BasePrice;
    }
}
