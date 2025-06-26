using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Text.Json;
using GameGuild.Common.Entities;
using GameGuild.Common.Enums;

namespace GameGuild.Modules.Product.Models;

[Table("Products")]
[Index(nameof(Name))]
[Index(nameof(Status))]
[Index(nameof(Visibility))]
[Index(nameof(CreatorId))]
public class Product : Content
{
    private string _name = string.Empty;

    private string? _shortDescription;

    private string? _imageUrl;

    private ProductType _type = ProductType.Program;

    private bool _isBundle = false;

    private Guid _creatorId;

    private User.Models.User _creator = null!;

    private string? _bundleItems;

    private decimal _referralCommissionPercentage = 30m;

    private decimal _maxAffiliateDiscount = 0m;

    private decimal _affiliateCommissionPercentage = 30m;

    private ICollection<ProductProgram> _productPrograms = new List<ProductProgram>();

    private ICollection<ProductPricing> _productPricings = new List<ProductPricing>();

    private ICollection<ProductSubscriptionPlan> _subscriptionPlans = new List<ProductSubscriptionPlan>();

    private ICollection<UserProduct> _userProducts = new List<UserProduct>();

    private ICollection<PromoCode> _promoCodes = new List<PromoCode>();

    [Required]
    [MaxLength(200)]
    public string Name
    {
        get => _name;
        set => _name = value;
    }

    [MaxLength(500)]
    public string? ShortDescription
    {
        get => _shortDescription;
        set => _shortDescription = value;
    }

    [MaxLength(500)]
    public string? ImageUrl
    {
        get => _imageUrl;
        set => _imageUrl = value;
    }

    public ProductType Type
    {
        get => _type;
        set => _type = value;
    }

    public bool IsBundle
    {
        get => _isBundle;
        set => _isBundle = value;
    }

    // Creator relationship
    public Guid CreatorId
    {
        get => _creatorId;
        set => _creatorId = value;
    }

    public virtual Modules.User.Models.User Creator
    {
        get => _creator;
        set => _creator = value;
    }

    /// <summary>
    /// JSON array of product IDs included in the bundle
    /// </summary>
    [Column(TypeName = "jsonb")]
    public string? BundleItems
    {
        get => _bundleItems;
        set => _bundleItems = value;
    }

    [Column(TypeName = "decimal(5,2)")]
    public decimal ReferralCommissionPercentage
    {
        get => _referralCommissionPercentage;
        set => _referralCommissionPercentage = value;
    }

    [Column(TypeName = "decimal(5,2)")]
    public decimal MaxAffiliateDiscount
    {
        get => _maxAffiliateDiscount;
        set => _maxAffiliateDiscount = value;
    }

    [Column(TypeName = "decimal(5,2)")]
    public decimal AffiliateCommissionPercentage
    {
        get => _affiliateCommissionPercentage;
        set => _affiliateCommissionPercentage = value;
    }

    // Navigation properties
    public virtual ICollection<ProductProgram> ProductPrograms
    {
        get => _productPrograms;
        set => _productPrograms = value;
    }

    public virtual ICollection<ProductPricing> ProductPricings
    {
        get => _productPricings;
        set => _productPricings = value;
    }

    public virtual ICollection<ProductSubscriptionPlan> SubscriptionPlans
    {
        get => _subscriptionPlans;
        set => _subscriptionPlans = value;
    }

    public virtual ICollection<UserProduct> UserProducts
    {
        get => _userProducts;
        set => _userProducts = value;
    }

    public virtual ICollection<PromoCode> PromoCodes
    {
        get => _promoCodes;
        set => _promoCodes = value;
    }

    // Helper methods for JSON metadata
    public T? GetBundleMetadata<T>(string key) where T : class
    {
        if (Metadata?.AdditionalData == null) return null;

        try
        {
            var metadataDict = JsonSerializer.Deserialize<Dictionary<string, object>>(Metadata.AdditionalData);
            if (metadataDict != null && metadataDict.TryGetValue(key, out object? value))
            {
                return JsonSerializer.Deserialize<T>(JsonSerializer.Serialize(value));
            }
        }
        catch
        {
            // Handle JSON parsing errors gracefully
        }

        return null;
    }

    public void SetBundleMetadata<T>(string key, T value)
    {
        if (Metadata == null)
        {
            Metadata = new ResourceMetadata
            {
                ResourceType = nameof(Product), AdditionalData = "{}"
            };
        }

        var metadataDict = string.IsNullOrEmpty(Metadata.AdditionalData)
            ? new Dictionary<string, object>()
            : JsonSerializer.Deserialize<Dictionary<string, object>>(Metadata.AdditionalData) ?? new Dictionary<string, object>();

        metadataDict[key] = value!;
        Metadata.AdditionalData = JsonSerializer.Serialize(metadataDict);
    }

    public List<Guid> GetBundleItemIds()
    {
        if (string.IsNullOrEmpty(BundleItems)) return new List<Guid>();

        try
        {
            return JsonSerializer.Deserialize<List<Guid>>(BundleItems) ?? new List<Guid>();
        }
        catch
        {
            return new List<Guid>();
        }
    }

    public void SetBundleItemIds(List<Guid> productIds)
    {
        BundleItems = JsonSerializer.Serialize(productIds);
    }
}

/// <summary>
/// Entity Framework configuration for Product entity
/// </summary>
public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        // Configure relationship with Creator (can't be done with annotations)
        builder.HasOne(p => p.Creator)
            .WithMany()
            .HasForeignKey(p => p.CreatorId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
