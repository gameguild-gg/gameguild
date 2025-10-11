using GameGuild.Modules.Products.Domain.Enums;
using ProductEntity = GameGuild.Modules.Products.Models.Product;
namespace GameGuild.Modules.Products.GraphQL;

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
