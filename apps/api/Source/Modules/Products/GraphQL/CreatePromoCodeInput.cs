using GameGuild.Modules.Products.Domain.Enums;
using ProductEntity = GameGuild.Modules.Products.Models.Product;
namespace GameGuild.Modules.Products.GraphQL;

public class CreatePromoCodeInput {
  public required Guid ProductId { get; set; }

  public required string Code { get; set; }

  public required decimal DiscountPercentage { get; set; }

  public DateTime? ExpiryDate { get; set; }

  public required PromoCodeType DiscountType { get; set; }

  public DateTime? ValidFrom { get; set; }

  public DateTime? ValidUntil { get; set; }

  public int? MaxUses { get; set; }

  public required decimal DiscountValue { get; set; }
}
