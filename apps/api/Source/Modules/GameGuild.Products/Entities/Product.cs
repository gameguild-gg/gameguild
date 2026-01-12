using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;
using GameGuild.Entities;
using GameGuild.Identity.Users;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.Products;

/// <summary>
/// Represents a product in the system
/// Inherits from EntityBase to provide GUID IDs, version control, timestamps, and soft delete functionality
/// </summary>
[Table("Products")]
[Index(nameof(Name))]
[Index(nameof(Type))]
[Index(nameof(CreatorId))]
public class Product : EntityBase
{
    /// <summary>
    /// Default constructor
    /// </summary>
    public Product() { }

    /// <summary>
    /// Constructor for partial initialization
    /// </summary>
    /// <param name="partial">Partial product data</param>
    public Product(object partial) : base(partial) { }

    /// <summary>
    /// Product name
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Full product description
    /// </summary>
    [MaxLength(4000)]
    public string? Description { get; set; }

    /// <summary>
    /// Short description for listings
    /// </summary>
    [MaxLength(500)]
    public string? ShortDescription { get; set; }

    /// <summary>
    /// Product image URL
    /// </summary>
    [MaxLength(500)]
    public string? ImageUrl { get; set; }

    /// <summary>
    /// Type of product
    /// </summary>
    public ProductType Type { get; set; } = ProductType.Program;

    /// <summary>
    /// Whether this product is a bundle of other products
    /// </summary>
    public bool IsBundle { get; set; }

    /// <summary>
    /// Creator user ID
    /// </summary>
    public Guid? CreatorId { get; set; }

    /// <summary>
    /// Navigation property to the creator
    /// </summary>
    public virtual User? Creator { get; set; }

    /// <summary>
    /// JSON array of product IDs included in the bundle
    /// </summary>
    [MaxLength(4000)]
    [Column(TypeName = "jsonb")]
    public string? BundleItems { get; set; }

    /// <summary>
    /// Referral commission percentage
    /// </summary>
    [Column(TypeName = "decimal(5,2)")]
    public decimal ReferralCommissionPercentage { get; set; } = 30m;

    /// <summary>
    /// Maximum affiliate discount
    /// </summary>
    [Column(TypeName = "decimal(5,2)")]
    public decimal MaxAffiliateDiscount { get; set; }

    /// <summary>
    /// Affiliate commission percentage
    /// </summary>
    [Column(TypeName = "decimal(5,2)")]
    public decimal AffiliateCommissionPercentage { get; set; } = 30m;

    // Navigation properties
    /// <summary>
    /// Pricing options for this product
    /// </summary>
    public virtual ICollection<ProductPricing> Pricing { get; set; } = new List<ProductPricing>();

    /// <summary>
    /// Subscription plans for this product
    /// </summary>
    public virtual ICollection<ProductSubscriptionPlan> SubscriptionPlans { get; set; } = new List<ProductSubscriptionPlan>();

    /// <summary>
    /// User-product relationships (purchases/access)
    /// </summary>
    public virtual ICollection<UserProduct> UserProducts { get; set; } = new List<UserProduct>();

    /// <summary>
    /// Promo codes for this product
    /// </summary>
    public virtual ICollection<PromoCode> PromoCodes { get; set; } = new List<PromoCode>();

    /// <summary>
    /// Factory method to create a new product
    /// </summary>
    public static Product Create(
        string name,
        ProductType type = ProductType.Program,
        string? description = null,
        string? shortDescription = null,
        string? imageUrl = null,
        Guid? creatorId = null,
        bool isBundle = false,
        decimal referralCommissionPercentage = 30m,
        decimal maxAffiliateDiscount = 0m,
        decimal affiliateCommissionPercentage = 30m,
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
            ReferralCommissionPercentage = referralCommissionPercentage,
            MaxAffiliateDiscount = maxAffiliateDiscount,
            AffiliateCommissionPercentage = affiliateCommissionPercentage,
            TenantId = tenantId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Get bundle item IDs from JSON
    /// </summary>
    public List<Guid> GetBundleItemIds()
    {
        if (string.IsNullOrEmpty(BundleItems))
            return new List<Guid>();

        try
        {
            return JsonSerializer.Deserialize<List<Guid>>(BundleItems) ?? new List<Guid>();
        }
        catch
        {
            return new List<Guid>();
        }
    }

    /// <summary>
    /// Set bundle item IDs to JSON
    /// </summary>
    public void SetBundleItemIds(List<Guid> productIds)
    {
        BundleItems = JsonSerializer.Serialize(productIds);
    }
}
