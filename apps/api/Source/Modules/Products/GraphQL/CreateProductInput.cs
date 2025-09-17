namespace GameGuild.Modules.Products;

public class CreateProductInput {
  public required string Name { get; set; }

  public string? ShortDescription { get; set; }

  public required ProductType Type { get; set; }

  public bool IsBundle { get; set; } = false;

  public Guid? TenantId { get; set; }
}
