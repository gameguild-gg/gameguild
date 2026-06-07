using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.Commerce.Products;

/// <summary>
///     Entity representing pricing information for products.
///     Price changes are tracked via ProductPricingVersion for audit trail.
/// </summary>
[Table("product_pricing")]
[Index(nameof(ProductId))]
[Index(nameof(IsDefault))]
[Index(nameof(Currency))]
[Index(nameof(SaleStartDate))]
[Index(nameof(SaleEndDate))]
public class ProductPricing : EntityBase
{
    /// <summary>Default constructor for EF Core</summary>
    public ProductPricing() { }

    /// <summary>Constructor for partial initialization</summary>
    /// <param name="partial">Partial product pricing data</param>
    public ProductPricing(object partial) : base(partial) { }

    /// <summary>Foreign key to the Product entity</summary>
    [Required]
    public Guid ProductId { get; set; }

    /// <summary>Navigation property to the Product entity</summary>
    [ForeignKey(nameof(ProductId))]
    public virtual Product Product { get; set; } = null!;

    /// <summary>Name of this pricing option (e.g., "Standard", "Premium", "Early Bird")</summary>
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    /// <summary>Regular price for this product</summary>
    [Column(TypeName = "decimal(10,2)")]
    public decimal BasePrice { get; private set; }

    /// <summary>Sale price (if on sale)</summary>
    [Column(TypeName = "decimal(10,2)")]
    public decimal? SalePrice { get; private set; }

    /// <summary>Currency code for prices</summary>
    [MaxLength(3)]
    public string Currency { get; set; } = "USD";

    /// <summary>When the sale price becomes active</summary>
    public DateTime? SaleStartDate { get; set; }

    /// <summary>When the sale price expires</summary>
    public DateTime? SaleEndDate { get; set; }

    /// <summary>Whether this is the default pricing option for the product</summary>
    public bool IsDefault { get; set; } = false;

    /// <summary>Current version number for price tracking</summary>
    public int CurrentVersion { get; private set; } = 1;

    /// <summary>Navigation property to price version history</summary>
    public virtual ICollection<ProductPricingVersion> Versions { get; set; } = new List<ProductPricingVersion>();

    /// <summary>
    ///     Updates the base price and creates a new version for audit trail.
    ///     Returns the new version that should be persisted.
    /// </summary>
    /// <param name="newBasePrice">New base price</param>
    /// <param name="changeReason">Reason for the price change</param>
    /// <param name="changedByUserId">User making the change</param>
    /// <returns>The new pricing version created</returns>
    public ProductPricingVersion UpdateBasePrice(decimal newBasePrice, string? changeReason = null, Guid? changedByUserId = null)
    {
        if (newBasePrice < 0)
            throw new ArgumentException("Price cannot be negative", nameof(newBasePrice));

        // Create version before changing
        var previousVersion = GetCurrentActiveVersion();
        previousVersion?.Supersede(SystemClock.UtcNow);

        BasePrice = newBasePrice;
        CurrentVersion++;
        Touch();

        return ProductPricingVersion.Create(this, CurrentVersion, SystemClock.UtcNow, changeReason, changedByUserId);
    }

    /// <summary>
    ///     Updates the sale price and creates a new version for audit trail.
    /// </summary>
    public ProductPricingVersion UpdateSalePrice(decimal? newSalePrice, string? changeReason = null, Guid? changedByUserId = null)
    {
        if (newSalePrice.HasValue && newSalePrice.Value < 0)
            throw new ArgumentException("Sale price cannot be negative", nameof(newSalePrice));

        var previousVersion = GetCurrentActiveVersion();
        previousVersion?.Supersede(SystemClock.UtcNow);

        SalePrice = newSalePrice;
        CurrentVersion++;
        Touch();

        return ProductPricingVersion.Create(this, CurrentVersion, SystemClock.UtcNow, changeReason, changedByUserId);
    }

    /// <summary>
    ///     Sets both base and sale price, creating a single version entry.
    /// </summary>
    public ProductPricingVersion UpdatePrices(decimal newBasePrice, decimal? newSalePrice, string? changeReason = null, Guid? changedByUserId = null)
    {
        if (newBasePrice < 0)
            throw new ArgumentException("Base price cannot be negative", nameof(newBasePrice));

        if (newSalePrice.HasValue && newSalePrice.Value < 0)
            throw new ArgumentException("Sale price cannot be negative", nameof(newSalePrice));

        if (newSalePrice.HasValue && newSalePrice.Value >= newBasePrice)
            throw new ArgumentException("Sale price must be less than base price", nameof(newSalePrice));

        var previousVersion = GetCurrentActiveVersion();
        previousVersion?.Supersede(SystemClock.UtcNow);

        BasePrice = newBasePrice;
        SalePrice = newSalePrice;
        CurrentVersion++;
        Touch();

        return ProductPricingVersion.Create(this, CurrentVersion, SystemClock.UtcNow, changeReason, changedByUserId);
    }

    /// <summary>
    ///     Creates the initial version for a new ProductPricing.
    ///     Call this after creating a new ProductPricing entity.
    /// </summary>
    public ProductPricingVersion CreateInitialVersion(Guid? createdByUserId = null)
    {
        return ProductPricingVersion.CreateInitial(this, createdByUserId);
    }

    /// <summary>
    ///     Gets the price version that was active at a specific date.
    ///     Useful for historical reporting and order reconciliation.
    /// </summary>
    public ProductPricingVersion? GetVersionAt(DateTime asOfDate)
    {
        return Versions
            .Where(v => v.EffectiveFrom <= asOfDate && (v.EffectiveTo == null || v.EffectiveTo > asOfDate))
            .OrderByDescending(v => v.PriceVersion)
            .FirstOrDefault();
    }

    /// <summary>
    ///     Gets the currently active version
    /// </summary>
    public ProductPricingVersion? GetCurrentActiveVersion()
    {
        return Versions.FirstOrDefault(v => v.IsActive);
    }

    /// <summary>Get the current effective price (sale price if active, otherwise base price)</summary>
    public decimal GetCurrentPrice()
    {
        if (SalePrice.HasValue && IsSaleActive()) return SalePrice.Value;
        return BasePrice;
    }

    /// <summary>Check if a sale is currently active</summary>
    public bool IsSaleActive()
    {
        if (!SalePrice.HasValue) return false;

        var now = SystemClock.UtcNow;
        if (SaleStartDate.HasValue && SaleStartDate.Value > now)
            return false;

        if (SaleEndDate.HasValue && SaleEndDate.Value <= now)
            return false;

        return true;
    }

    /// <summary>
    ///     Factory method to create a new ProductPricing with initial version (full parameters)
    /// </summary>
    public static (ProductPricing Pricing, ProductPricingVersion InitialVersion) CreateWithVersion(
        Guid productId,
        string name,
        decimal basePrice,
        string currency,
        decimal? salePrice,
        DateTime? saleStartDate,
        DateTime? saleEndDate,
        bool isDefault,
        Guid? createdByUserId = null,
        Guid? tenantId = null)
    {
        if (productId == Guid.Empty)
            throw new ArgumentException("Product ID is required", nameof(productId));

        if (basePrice < 0)
            throw new ArgumentException("Base price cannot be negative", nameof(basePrice));

        var pricing = new ProductPricing
        {
            Id = Guid.NewGuid(),
            ProductId = productId,
            Name = name,
            BasePrice = basePrice,
            SalePrice = salePrice,
            Currency = currency,
            SaleStartDate = saleStartDate,
            SaleEndDate = saleEndDate,
            IsDefault = isDefault,
            CurrentVersion = 1,
            TenantId = tenantId
        };

        var initialVersion = pricing.CreateInitialVersion(createdByUserId);

        return (pricing, initialVersion);
    }

    /// <summary>
    ///     Factory method to create a new ProductPricing with initial version (minimal parameters)
    /// </summary>
    public static (ProductPricing Pricing, ProductPricingVersion InitialVersion) CreateWithVersion(
        Guid productId,
        string name,
        decimal basePrice,
        decimal? salePrice = null,
        string currency = "USD",
        bool isDefault = false,
        Guid? tenantId = null,
        Guid? createdByUserId = null)
    {
        return CreateWithVersion(productId, name, basePrice, currency, salePrice, null, null, isDefault, createdByUserId, tenantId);
    }
}
