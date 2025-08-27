using System.Text.Json;
using GameGuild.Modules.Contents;
using GameGuild.Modules.Resources;
using GameGuild.Modules.Users;


namespace GameGuild.Modules.Products;

[Table("Products")]
[Index(nameof(Name))]
[Index(nameof(Status))]
[Index(nameof(Visibility))]
[Index(nameof(CreatorId))]
public class Product : Content {
  [Required][MaxLength(200)] public string Name { get; set; } = string.Empty;

  [MaxLength(500)] public string? ShortDescription { get; set; }

  [MaxLength(500)] public string? ImageUrl { get; set; }

  public Common.ProductType Type { get; set; } = Common.ProductType.Program;

  public bool IsBundle { get; set; }

  // Creator relationship
  public Guid? CreatorId { get; set; }

  public virtual User? Creator { get; set; }

  /// <summary>
  /// JSON array of product IDs included in the bundle
  /// </summary>
  [Column(TypeName = "jsonb")]
  public string? BundleItems { get; set; }

  [Column(TypeName = "decimal(5,2)")] public decimal ReferralCommissionPercentage { get; set; } = 30m;

  [Column(TypeName = "decimal(5,2)")] public decimal MaxAffiliateDiscount { get; set; }

  [Column(TypeName = "decimal(5,2)")] public decimal AffiliateCommissionPercentage { get; set; } = 30m;

  // Navigation properties
  public virtual ICollection<ProductProgram> ProductPrograms { get; set; } = new List<ProductProgram>();

  public virtual ICollection<ProductPricing> ProductPricings { get; set; } = new List<ProductPricing>();

  public virtual ICollection<ProductSubscriptionPlan> SubscriptionPlans { get; set; } = new List<ProductSubscriptionPlan>();

  public virtual ICollection<UserProduct> UserProducts { get; set; } = new List<UserProduct>();

  public virtual ICollection<PromoCode> PromoCodes { get; set; } = new List<PromoCode>();

  // Helper methods for JSON metadata
  public T? GetBundleMetadata<T>(string key) where T : class {
    if (Metadata?.AdditionalData == null) return null;

    try {
      var metadataDict = JsonSerializer.Deserialize<Dictionary<string, object>>(Metadata.AdditionalData);

      if (metadataDict != null && metadataDict.TryGetValue(key, out var value)) return JsonSerializer.Deserialize<T>(JsonSerializer.Serialize(value));
    }
    catch {
      // Handle JSON parsing errors gracefully
    }

    return null;
  }

  public void SetBundleMetadata<T>(string key, T value) {
    if (Metadata == null) Metadata = new ResourceMetadata { ResourceType = nameof(Product), AdditionalData = "{}" };

    var metadataDict = string.IsNullOrEmpty(Metadata.AdditionalData)
                         ? new Dictionary<string, object>()
                         : JsonSerializer.Deserialize<Dictionary<string, object>>(Metadata.AdditionalData) ??
                           new Dictionary<string, object>();

    metadataDict[key] = value!;
    Metadata.AdditionalData = JsonSerializer.Serialize(metadataDict);
  }

  public List<Guid> GetBundleItemIds() {
    if (string.IsNullOrEmpty(BundleItems)) return new List<Guid>();

    try { return JsonSerializer.Deserialize<List<Guid>>(BundleItems) ?? new List<Guid>(); }
    catch { return new List<Guid>(); }
  }

  public void SetBundleItemIds(List<Guid> productIds) { BundleItems = JsonSerializer.Serialize(productIds); }
}
