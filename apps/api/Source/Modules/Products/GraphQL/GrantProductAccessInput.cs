using GameGuild.Common;


namespace GameGuild.Modules.Products.GraphQL;

public class GrantProductAccessInput {
  public required Guid ProductId { get; set; }

  public required Guid UserId { get; set; }

  public required ProductAcquisitionType AcquisitionType { get; set; }

  public required decimal PurchasePrice { get; set; }

  public string? Currency { get; set; }

  public DateTime? ExpiresAt { get; set; }
}
