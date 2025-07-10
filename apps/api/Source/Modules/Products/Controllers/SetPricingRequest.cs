namespace GameGuild.Modules.Products.Controllers;

public class SetPricingRequest {
  public decimal BasePrice { get; set; }

  public string Currency { get; set; } = "USD";
}
