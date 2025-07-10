namespace GameGuild.Modules.Products.GraphQL;

public class CreatePromoCodeInput {
  public required Guid ProductId { get; set; }

  public required string Code { get; set; }

  public required decimal DiscountPercentage { get; set; }

  public DateTime? ExpiryDate { get; set; }

  public required Common.PromoCodeType DiscountType { get; set; }

  public DateTime? ValidFrom { get; set; }

  public DateTime? ValidUntil { get; set; }

  public int? MaxUses { get; set; }

  public required decimal DiscountValue { get; set; }
}
