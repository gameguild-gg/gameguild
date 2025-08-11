using GameGuild.Common;


namespace GameGuild.Modules.Products;

public class GrantAccessRequest {
  public ProductAcquisitionType AcquisitionType { get; set; }

  public decimal PurchasePrice { get; set; } = 0;

  public string Currency { get; set; } = "USD";

  public DateTime? ExpiresAt { get; set; }
}
