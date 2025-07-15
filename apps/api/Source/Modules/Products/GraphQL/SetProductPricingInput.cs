namespace GameGuild.Modules.Products;

public class SetProductPricingInput {
  public required Guid ProductId { get; set; }

  public required decimal BasePrice { get; set; }

  public required string Currency { get; set; } = "USD";
}
