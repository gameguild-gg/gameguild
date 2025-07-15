namespace GameGuild.Modules.Products;

public class UpdatePromoCodeInput {
  public required Guid Id { get; set; }

  public string? Code { get; set; }

  public decimal? DiscountPercentage { get; set; }

  public DateTime? ExpiryDate { get; set; }

  public Common.PromoCodeType? DiscountType { get; set; }

  public DateTime? ValidFrom { get; set; }

  public DateTime? ValidUntil { get; set; }

  public int? MaxUses { get; set; }

  public decimal? DiscountValue { get; set; }
}
