using GameGuild.Common;
using GameGuild.Modules.Contents;
using PromoCodeTypeEnum = GameGuild.Common.PromoCodeType;


namespace GameGuild.Modules.Products.GraphQL;

public class CreateProductInput {
  public required string Name { get; set; }

  public string? ShortDescription { get; set; }

  public required Common.ProductType Type { get; set; }

  public bool IsBundle { get; set; } = false;

  public Guid? TenantId { get; set; }
}

public class UpdateProductInput {
  public required Guid Id { get; set; }

  public string? Name { get; set; }

  public string? ShortDescription { get; set; }

  public string? Description { get; set; }

  public Common.ProductType? Type { get; set; }

  public bool? IsBundle { get; set; }

  public ContentStatus? Status { get; set; }

  public AccessLevel? Visibility { get; set; }
}

public class BundleManagementInput {
  public Guid BundleId { get; set; }

  public Guid ProductId { get; set; }
}

public class SetProductPricingInput {
  public required Guid ProductId { get; set; }

  public required decimal BasePrice { get; set; }

  public required string Currency { get; set; } = "USD";
}

public class UpdateProductPricingInput {
  public required Guid PricingId { get; set; }

  public decimal? BasePrice { get; set; }

  public string? Currency { get; set; }
}

public class GrantProductAccessInput {
  public required Guid ProductId { get; set; }

  public required Guid UserId { get; set; }

  public required ProductAcquisitionType AcquisitionType { get; set; }

  public required decimal PurchasePrice { get; set; }

  public string? Currency { get; set; }

  public DateTime? ExpiresAt { get; set; }
}

public class CreatePromoCodeInput {
  public required Guid ProductId { get; set; }

  public required string Code { get; set; }

  public required decimal DiscountPercentage { get; set; }

  public DateTime? ExpiryDate { get; set; }

  public required PromoCodeTypeEnum DiscountType { get; set; }

  public DateTime? ValidFrom { get; set; }

  public DateTime? ValidUntil { get; set; }

  public int? MaxUses { get; set; }

  public required decimal DiscountValue { get; set; }
}

public class UpdatePromoCodeInput {
  public required Guid Id { get; set; }

  public string? Code { get; set; }

  public decimal? DiscountPercentage { get; set; }

  public DateTime? ExpiryDate { get; set; }

  public PromoCodeTypeEnum? DiscountType { get; set; }

  public DateTime? ValidFrom { get; set; }

  public DateTime? ValidUntil { get; set; }

  public int? MaxUses { get; set; }

  public decimal? DiscountValue { get; set; }
}
