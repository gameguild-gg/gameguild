namespace GameGuild.Modules.Products;

public class UpdateProductPricingInput {
  public required Guid PricingId { get; set; }

  public decimal? BasePrice { get; set; }

  public string? Currency { get; set; }
}
