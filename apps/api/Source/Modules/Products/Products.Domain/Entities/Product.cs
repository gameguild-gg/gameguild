using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;
using GameGuild;
using GameGuild.Modules.Products.Domain.Enums;
using GameGuild.Modules.Users;
using Microsoft.EntityFrameworkCore;

using ProductEntity = GameGuild.Modules.Products.Models.Product;
namespace GameGuild.Modules.Products.Domain.Entities;

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

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? ShortDescription { get; set; }

    [MaxLength(500)]
    public string? ImageUrl { get; set; }

    public ProductType Type { get; set; } = ProductType.Program;

    public bool IsBundle { get; set; }

    // Creator relationship
    public Guid? CreatorId { get; set; }

    public virtual User? Creator { get; set; }

    /// <summary>JSON array of product IDs included in the bundle</summary>
    [Column(TypeName = "jsonb")]
    public string? BundleItems { get; set; }

    [Column(TypeName = "decimal(5,2)")]
    public decimal ReferralCommissionPercentage { get; set; } = 30m;

    [Column(TypeName = "decimal(5,2)")]
    public decimal MaxAffiliateDiscount { get; set; }

    [Column(TypeName = "decimal(5,2)")]
    public decimal AffiliateCommissionPercentage { get; set; } = 30m;

    // Navigation properties
    public virtual ICollection<ProductPricing> ProductPricings { get; set; } = new List<ProductPricing>();
    public virtual ICollection<ProductSubscriptionPlan> SubscriptionPlans { get; set; } = new List<ProductSubscriptionPlan>();
    public virtual ICollection<UserProduct> UserProducts { get; set; } = new List<UserProduct>();
    public virtual ICollection<PromoCode> PromoCodes { get; set; } = new List<PromoCode>();

    // Helper methods for bundle management
    public List<Guid> GetBundleItemIds()
    {
        if (string.IsNullOrEmpty(BundleItems)) return new List<Guid>();

        try { return JsonSerializer.Deserialize<List<Guid>>(BundleItems) ?? new List<Guid>(); }
        catch { return new List<Guid>(); }
    }

    public void SetBundleItemIds(List<Guid> productIds)
    {
        BundleItems = JsonSerializer.Serialize(productIds);
    }
}