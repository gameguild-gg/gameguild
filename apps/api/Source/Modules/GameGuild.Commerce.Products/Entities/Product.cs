using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;
using GameGuild.Entities;
using GameGuild.Identity.Users;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.Commerce.Products;

/// <summary>
///     Represents a product in the system.
///     Inherits from EntityBase to provide GUID IDs, version control, timestamps, and soft delete functionality.
/// </summary>
[Table("Products")]
[Index(nameof(Name))]
[Index(nameof(Type))]
[Index(nameof(CreatorId))]
public class Product : EntityBase
{
    /// <summary>
    ///     Default constructor
    /// </summary>
    public Product() { }

    /// <summary>
    ///     Constructor for partial initialization
    /// </summary>
    /// <param name="partial">Partial product data</param>
    public Product(object partial) : base(partial) { }

    /// <summary>
    ///     Product name
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    ///     Full product description
    /// </summary>
    [MaxLength(4000)]
    public string? Description { get; set; }

    /// <summary>
    ///     Short description for listings
    /// </summary>
    [MaxLength(500)]
    public string? ShortDescription { get; set; }

    /// <summary>
    ///     Product image URL
    /// </summary>
    [MaxLength(500)]
    public string? ImageUrl { get; set; }

    /// <summary>
    ///     Type of product
    /// </summary>
    public ProductType Type { get; set; } = ProductType.Program;

    /// <summary>
    ///     Whether this product is a bundle of other products
    /// </summary>
    public bool IsBundle { get; set; }

    /// <summary>
    ///     Creator user ID
    /// </summary>
    public Guid? CreatorId { get; set; }

    /// <summary>
    ///     Navigation property to the creator
    /// </summary>
    public virtual User? Creator { get; set; }

    /// <summary>
    ///     JSON array of product IDs included in the bundle.
    ///     DEPRECATED: Use BundleItems collection instead for type-safe bundle management.
    /// </summary>
    [Obsolete("Use BundleItems navigation property instead for type-safe bundle management")]
    [MaxLength(4000)]
    [Column(TypeName = "jsonb")]
    public string? BundleItemsJson { get; set; }

    /// <summary>
    ///     Referral commission percentage.
    ///     DEPRECATED: Use CommissionConfig instead.
    /// </summary>
    [Obsolete("Use CommissionConfig instead for commission management")]
    [Column(TypeName = "decimal(5,2)")]
    public decimal ReferralCommissionPercentage { get; set; } = 30m;

    /// <summary>
    ///     Maximum affiliate discount.
    ///     DEPRECATED: Use CommissionConfig instead.
    /// </summary>
    [Obsolete("Use CommissionConfig instead for commission management")]
    [Column(TypeName = "decimal(5,2)")]
    public decimal MaxAffiliateDiscount { get; set; }

    /// <summary>
    ///     Affiliate commission percentage.
    ///     DEPRECATED: Use CommissionConfig instead.
    /// </summary>
    [Obsolete("Use CommissionConfig instead for commission management")]
    [Column(TypeName = "decimal(5,2)")]
    public decimal AffiliateCommissionPercentage { get; set; } = 30m;

    // Navigation properties

    /// <summary>
    ///     Pricing options for this product
    /// </summary>
    public virtual ICollection<ProductPricing> Pricing { get; set; } = new List<ProductPricing>();

    /// <summary>
    ///     Subscription plans for this product
    /// </summary>
    public virtual ICollection<ProductSubscriptionPlan> SubscriptionPlans { get; set; } = new List<ProductSubscriptionPlan>();

    /// <summary>
    ///     User-product relationships (purchases/access)
    /// </summary>
    public virtual ICollection<UserProduct> UserProducts { get; set; } = new List<UserProduct>();

    /// <summary>
    ///     Promo codes for this product
    /// </summary>
    public virtual ICollection<PromoCode> PromoCodes { get; set; } = new List<PromoCode>();

    /// <summary>
    ///     Commission configuration for affiliate/referral programs.
    ///     Replaces the deprecated inline commission fields.
    /// </summary>
    public virtual ProductCommissionConfig? CommissionConfig { get; set; }

    /// <summary>
    ///     Type-safe collection of bundle items.
    ///     Replaces the deprecated BundleItemsJson field.
    /// </summary>
    public virtual ICollection<ProductBundleItem> BundleItems { get; set; } = new List<ProductBundleItem>();

    /// <summary>
    ///     Products that include this product in their bundles.
    /// </summary>
    public virtual ICollection<ProductBundleItem> IncludedInBundles { get; set; } = new List<ProductBundleItem>();

    /// <summary>
    ///     Factory method to create a new product
    /// </summary>
    public static Product Create(
        string name,
        ProductType type = ProductType.Program,
        string? description = null,
        string? shortDescription = null,
        string? imageUrl = null,
        Guid? creatorId = null,
        bool isBundle = false,
        Guid? tenantId = null)
    {
        return new Product
        {
            Id = Guid.NewGuid(),
            Name = name,
            Type = type,
            Description = description,
            ShortDescription = shortDescription,
            ImageUrl = imageUrl,
            CreatorId = creatorId,
            IsBundle = isBundle,
            TenantId = tenantId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    ///     Creates a product with commission configuration
    /// </summary>
    public static (Product Product, ProductCommissionConfig CommissionConfig) CreateWithCommission(
        string name,
        ProductType type = ProductType.Program,
        string? description = null,
        string? shortDescription = null,
        string? imageUrl = null,
        Guid? creatorId = null,
        bool isBundle = false,
        decimal referralCommissionPercentage = 30m,
        decimal affiliateCommissionPercentage = 30m,
        decimal maxAffiliateDiscount = 0m,
        Guid? tenantId = null)
    {
        var product = Create(name, type, description, shortDescription, imageUrl, creatorId, isBundle, tenantId);
        var commissionConfig = ProductCommissionConfig.Create(
            product.Id,
            referralCommissionPercentage,
            affiliateCommissionPercentage,
            maxAffiliateDiscount,
            tenantId);

        return (product, commissionConfig);
    }

    /// <summary>
    ///     Adds a product to this bundle
    /// </summary>
    /// <param name="includedProductId">Product ID to include</param>
    /// <param name="quantity">Quantity to include</param>
    /// <param name="displayOrder">Display order in bundle</param>
    /// <returns>The created bundle item</returns>
    public ProductBundleItem AddToBundleTypeSafe(Guid includedProductId, int quantity = 1, int displayOrder = 0)
    {
        if (!IsBundle)
            throw new InvalidOperationException("Cannot add items to a non-bundle product");

        if (BundleItems.Any(bi => bi.IncludedProductId == includedProductId))
            throw new InvalidOperationException("Product is already in this bundle");

        var bundleItem = ProductBundleItem.Create(Id, includedProductId, quantity, displayOrder, true, TenantId);
        BundleItems.Add(bundleItem);
        return bundleItem;
    }

    /// <summary>
    ///     Removes a product from this bundle
    /// </summary>
    public bool RemoveFromBundle(Guid includedProductId)
    {
        var item = BundleItems.FirstOrDefault(bi => bi.IncludedProductId == includedProductId);
        if (item == null)
            return false;

        BundleItems.Remove(item);
        return true;
    }

    /// <summary>
    ///     Gets bundle item product IDs (type-safe version)
    /// </summary>
    public IEnumerable<Guid> GetBundleProductIds()
    {
        return BundleItems.OrderBy(bi => bi.DisplayOrder).Select(bi => bi.IncludedProductId);
    }

    /// <summary>
    ///     Get bundle item IDs from JSON.
    ///     DEPRECATED: Use GetBundleProductIds() instead.
    /// </summary>
    [Obsolete("Use GetBundleProductIds() instead for type-safe bundle management")]
    public List<Guid> GetBundleItemIds()
    {
#pragma warning disable CS0618 // Type or member is obsolete
        if (string.IsNullOrEmpty(BundleItemsJson))
            return new List<Guid>();

        try
        {
            return JsonSerializer.Deserialize<List<Guid>>(BundleItemsJson) ?? new List<Guid>();
        }
        catch
        {
            return new List<Guid>();
        }
#pragma warning restore CS0618
    }

    /// <summary>
    ///     Set bundle item IDs to JSON.
    ///     DEPRECATED: Use AddToBundleTypeSafe() instead.
    /// </summary>
    [Obsolete("Use AddToBundleTypeSafe() instead for type-safe bundle management")]
    public void SetBundleItemIds(List<Guid> productIds)
    {
#pragma warning disable CS0618 // Type or member is obsolete
        BundleItemsJson = JsonSerializer.Serialize(productIds);
#pragma warning restore CS0618
    }
}
